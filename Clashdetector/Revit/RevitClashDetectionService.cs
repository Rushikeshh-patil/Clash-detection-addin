using System.Diagnostics;
using Autodesk.Revit.DB;
using Clashdetector.Core.Contracts;
using Clashdetector.Core.Models;
using Clashdetector.Core.Services;
using Clashdetector.Core.Utilities;

namespace Clashdetector.Revit;

public sealed class RevitClashDetectionService : IClashDetectionService
{
    private const double IntersectionTolerance = 1e-9;
    private readonly RevitModelResolver _modelResolver;
    private readonly IClashSuggestionProvider _suggestionProvider;
    private readonly DiagnosticLogger _logger;
    private readonly Func<Document?> _hostDocumentProvider;

    public RevitClashDetectionService(
        RevitModelResolver modelResolver,
        IClashSuggestionProvider suggestionProvider,
        DiagnosticLogger logger,
        Func<Document?> hostDocumentProvider)
    {
        _modelResolver = modelResolver;
        _suggestionProvider = suggestionProvider;
        _logger = logger;
        _hostDocumentProvider = hostDocumentProvider;
    }

    public DetectionRunSummary Run(ClashDetectionRequest request)
    {
        var summary = DetectionRunSummary.Empty();
        if (request.ActiveConfigs.Count == 0)
        {
            return summary;
        }

        var hostDoc = _hostDocumentProvider();
        if (hostDoc is null)
        {
            _logger.Warn("Skipping clash run because no active host document was available.");
            return summary;
        }

        var totalStopwatch = Stopwatch.StartNew();
        var totalElements = 0;
        var totalCandidates = 0;
        var totalConfirmed = 0;

        foreach (var config in request.ActiveConfigs)
        {
            if (!config.IsActive)
            {
                continue;
            }

            if (request.IsIncremental
                && !IncrementalDetectionScope.ShouldProcessConfig(config, request.ChangedElementIdsByModel))
            {
                continue;
            }

            var metrics = new DetectionMetrics
            {
                TimestampUtc = DateTimeOffset.UtcNow,
            };

            var configStopwatch = Stopwatch.StartNew();
            try
            {
                var results = DetectForConfig(config, request.IsIncremental, request.ChangedElementIdsByModel, out var elementsScanned, out var candidatePairs);
                summary.Results.AddRange(results);
                metrics.ElementsScanned = elementsScanned;
                metrics.CandidatePairs = candidatePairs;
                metrics.ConfirmedClashes = results.Count;
                totalElements += elementsScanned;
                totalCandidates += candidatePairs;
                totalConfirmed += results.Count;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error while processing config '{config.Name}'.", ex);
            }
            finally
            {
                configStopwatch.Stop();
                metrics.DurationMs = configStopwatch.ElapsedMilliseconds;
                summary.MetricsByConfig[config.Id] = metrics;
            }
        }

        totalStopwatch.Stop();
        summary.TotalMetrics = new DetectionMetrics
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            DurationMs = totalStopwatch.ElapsedMilliseconds,
            ElementsScanned = totalElements,
            CandidatePairs = totalCandidates,
            ConfirmedClashes = totalConfirmed,
        };
        return summary;
    }

    private List<ClashResult> DetectForConfig(
        ClashConfig config,
        bool isIncremental,
        IReadOnlyDictionary<string, IReadOnlyCollection<int>> changedByModel,
        out int elementsScanned,
        out int candidatePairs)
    {
        elementsScanned = 0;
        candidatePairs = 0;

        if (!_modelResolver.TryResolveModel(config.ModelA, out var resolvedA)
            || !_modelResolver.TryResolveModel(config.ModelB, out var resolvedB))
        {
            return new List<ClashResult>();
        }

        var enabledRules = config.CategoryPairs
            .Where(x => x.Enabled && !string.IsNullOrWhiteSpace(x.CategoryA) && !string.IsNullOrWhiteSpace(x.CategoryB))
            .ToList();
        if (enabledRules.Count == 0)
        {
            return new List<ClashResult>();
        }

        var categoriesA = enabledRules.Select(x => x.CategoryA.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var categoriesB = enabledRules.Select(x => x.CategoryB.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var snapshotsA = CollectElementSnapshots(resolvedA!, categoriesA);
        var snapshotsB = CollectElementSnapshots(resolvedB!, categoriesB);
        elementsScanned = snapshotsA.Count + snapshotsB.Count;

        var byCategoryA = snapshotsA
            .GroupBy(x => x.CategoryName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);
        var byCategoryB = snapshotsB
            .GroupBy(x => x.CategoryName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);

        var results = new List<ClashResult>();
        var dedup = new HashSet<string>(StringComparer.Ordinal);
        var solidCache = new Dictionary<string, IReadOnlyList<Solid>>(StringComparer.OrdinalIgnoreCase);
        var runTimestamp = DateTimeOffset.UtcNow;

        foreach (var rule in enabledRules)
        {
            if (!byCategoryA.TryGetValue(rule.CategoryA.Trim(), out var elementsA)
                || !byCategoryB.TryGetValue(rule.CategoryB.Trim(), out var elementsB))
            {
                continue;
            }

            foreach (var elementA in elementsA)
            {
                foreach (var elementB in elementsB)
                {
                    if (elementA.ModelStableKey == elementB.ModelStableKey && elementA.ElementId == elementB.ElementId)
                    {
                        continue;
                    }

                    if (elementA.ModelStableKey == elementB.ModelStableKey && elementA.ElementId > elementB.ElementId)
                    {
                        continue;
                    }

                    if (isIncremental
                        && !IncrementalDetectionScope.ShouldEvaluatePair(config, elementA.ElementId, elementB.ElementId, changedByModel))
                    {
                        continue;
                    }

                    if (!BoxesOverlap(elementA.Min, elementA.Max, elementB.Min, elementB.Max))
                    {
                        continue;
                    }

                    candidatePairs++;

                    if (!TryFindIntersection(
                        elementA,
                        elementB,
                        solidCache,
                        out var penetrationDepth,
                        out var clashCenter))
                    {
                        continue;
                    }

                    var dedupKey = ClashDeduplicationKeyFactory.Create(
                        config.Id,
                        elementA.ModelStableKey,
                        elementA.ElementId,
                        elementB.ModelStableKey,
                        elementB.ElementId);
                    if (!dedup.Add(dedupKey))
                    {
                        continue;
                    }

                    var severity = SeverityClassifier.Classify(penetrationDepth, config.SeverityThresholds);
                    var suggestion = _suggestionProvider.GetSuggestion(rule.CategoryA, rule.CategoryB);

                    results.Add(new ClashResult
                    {
                        ConfigId = config.Id,
                        ConfigName = config.Name,
                        ModelA = elementA.ModelRef,
                        ModelB = elementB.ModelRef,
                        ElementAId = elementA.ElementId,
                        ElementBId = elementB.ElementId,
                        CategoryA = elementA.CategoryName,
                        CategoryB = elementB.CategoryName,
                        Severity = severity,
                        PenetrationDepth = penetrationDepth,
                        Suggestion = suggestion,
                        RunTimestampUtc = runTimestamp,
                        DedupKey = dedupKey,
                        Location = new ClashLocation
                        {
                            X = clashCenter.X,
                            Y = clashCenter.Y,
                            Z = clashCenter.Z,
                        },
                    });
                }
            }
        }

        return results;
    }

    private static List<ElementSnapshot> CollectElementSnapshots(ResolvedModel model, HashSet<string> categoryNames)
    {
        var list = new List<ElementSnapshot>();
        var collector = new FilteredElementCollector(model.Document)
            .WhereElementIsNotElementType();

        foreach (var element in collector)
        {
            var category = element.Category;
            if (category is null || !categoryNames.Contains(category.Name))
            {
                continue;
            }

            var bbox = element.get_BoundingBox(null);
            if (bbox is null)
            {
                continue;
            }

            if (!TryTransformBoundingBox(bbox, model.TransformToHost, out var min, out var max))
            {
                continue;
            }

            list.Add(new ElementSnapshot
            {
                Element = element,
                ElementId = element.Id.IntegerValue,
                CategoryName = category.Name,
                Min = min,
                Max = max,
                ModelStableKey = model.StableKey,
                ModelRef = model.ModelRef,
                TransformToHost = model.TransformToHost,
            });
        }

        return list;
    }

    private static bool TryTransformBoundingBox(BoundingBoxXYZ bbox, Transform transform, out XYZ min, out XYZ max)
    {
        var points = new[]
        {
            new XYZ(bbox.Min.X, bbox.Min.Y, bbox.Min.Z),
            new XYZ(bbox.Max.X, bbox.Min.Y, bbox.Min.Z),
            new XYZ(bbox.Min.X, bbox.Max.Y, bbox.Min.Z),
            new XYZ(bbox.Max.X, bbox.Max.Y, bbox.Min.Z),
            new XYZ(bbox.Min.X, bbox.Min.Y, bbox.Max.Z),
            new XYZ(bbox.Max.X, bbox.Min.Y, bbox.Max.Z),
            new XYZ(bbox.Min.X, bbox.Max.Y, bbox.Max.Z),
            new XYZ(bbox.Max.X, bbox.Max.Y, bbox.Max.Z),
        };

        var transformed = points.Select(transform.OfPoint).ToArray();
        min = new XYZ(
            transformed.Min(x => x.X),
            transformed.Min(x => x.Y),
            transformed.Min(x => x.Z));
        max = new XYZ(
            transformed.Max(x => x.X),
            transformed.Max(x => x.Y),
            transformed.Max(x => x.Z));
        return true;
    }

    private static bool BoxesOverlap(XYZ minA, XYZ maxA, XYZ minB, XYZ maxB)
    {
        return minA.X <= maxB.X
            && maxA.X >= minB.X
            && minA.Y <= maxB.Y
            && maxA.Y >= minB.Y
            && minA.Z <= maxB.Z
            && maxA.Z >= minB.Z;
    }

    private static bool TryFindIntersection(
        ElementSnapshot elementA,
        ElementSnapshot elementB,
        Dictionary<string, IReadOnlyList<Solid>> solidCache,
        out double penetrationDepth,
        out XYZ center)
    {
        penetrationDepth = 0.0;
        center = new XYZ(
            (elementA.Min.X + elementA.Max.X + elementB.Min.X + elementB.Max.X) / 4.0,
            (elementA.Min.Y + elementA.Max.Y + elementB.Min.Y + elementB.Max.Y) / 4.0,
            (elementA.Min.Z + elementA.Max.Z + elementB.Min.Z + elementB.Max.Z) / 4.0);

        var solidsA = GetElementSolids(elementA, solidCache);
        var solidsB = GetElementSolids(elementB, solidCache);
        if (solidsA.Count == 0 || solidsB.Count == 0)
        {
            return false;
        }

        var maxDepth = 0.0;
        var found = false;
        var bestCenter = center;

        foreach (var solidA in solidsA)
        {
            foreach (var solidB in solidsB)
            {
                Solid? intersection;
                try
                {
                    intersection = BooleanOperationsUtils.ExecuteBooleanOperation(
                        solidA,
                        solidB,
                        BooleanOperationsType.Intersect);
                }
                catch
                {
                    continue;
                }

                if (intersection is null || intersection.Volume <= IntersectionTolerance)
                {
                    continue;
                }

                var currentDepth = Math.Cbrt(intersection.Volume);
                if (currentDepth > maxDepth)
                {
                    maxDepth = currentDepth;
                    var bb = intersection.GetBoundingBox();
                    if (bb is not null)
                    {
                        bestCenter = new XYZ(
                            (bb.Min.X + bb.Max.X) * 0.5,
                            (bb.Min.Y + bb.Max.Y) * 0.5,
                            (bb.Min.Z + bb.Max.Z) * 0.5);
                    }
                    else
                    {
                        bestCenter = intersection.ComputeCentroid();
                    }
                }

                found = true;
            }
        }

        penetrationDepth = maxDepth;
        center = bestCenter;
        return found;
    }

    private static IReadOnlyList<Solid> GetElementSolids(
        ElementSnapshot snapshot,
        Dictionary<string, IReadOnlyList<Solid>> solidCache)
    {
        var key = $"{snapshot.ModelStableKey}:{snapshot.ElementId}";
        if (solidCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var solids = new List<Solid>();
        var options = new Options
        {
            IncludeNonVisibleObjects = false,
            DetailLevel = ViewDetailLevel.Fine,
            ComputeReferences = false,
        };

        var geometry = snapshot.Element.get_Geometry(options);
        if (geometry is not null)
        {
            foreach (var solid in ExtractSolids(geometry))
            {
                if (solid.Volume <= IntersectionTolerance)
                {
                    continue;
                }

                try
                {
                    solids.Add(SolidUtils.CreateTransformed(solid, snapshot.TransformToHost));
                }
                catch
                {
                    // Skip malformed solids.
                }
            }
        }

        solidCache[key] = solids;
        return solids;
    }

    private static IEnumerable<Solid> ExtractSolids(GeometryElement geometry)
    {
        foreach (var obj in geometry)
        {
            if (obj is Solid solid && solid.Volume > IntersectionTolerance)
            {
                yield return solid;
                continue;
            }

            if (obj is GeometryInstance instance)
            {
                var instanceGeometry = instance.GetInstanceGeometry();
                foreach (var nestedSolid in ExtractSolids(instanceGeometry))
                {
                    yield return nestedSolid;
                }
            }
        }
    }

    private sealed class ElementSnapshot
    {
        public required Element Element { get; init; }

        public required int ElementId { get; init; }

        public required string CategoryName { get; init; }

        public required XYZ Min { get; init; }

        public required XYZ Max { get; init; }

        public required string ModelStableKey { get; init; }

        public required ModelRef ModelRef { get; init; }

        public required Transform TransformToHost { get; init; }
    }
}
