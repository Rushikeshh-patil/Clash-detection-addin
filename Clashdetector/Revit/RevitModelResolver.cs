using Autodesk.Revit.DB;
using Clashdetector.Core.Contracts;
using Clashdetector.Core.Models;

namespace Clashdetector.Revit;

public sealed class RevitModelResolver : IModelResolver
{
    private readonly Func<Document?> _hostDocumentProvider;

    public RevitModelResolver(Func<Document?> hostDocumentProvider)
    {
        _hostDocumentProvider = hostDocumentProvider;
    }

    public IReadOnlyList<ModelRef> GetAvailableModels()
    {
        var hostDoc = _hostDocumentProvider();
        if (hostDoc is null)
        {
            return new[] { ModelRef.Host() };
        }

        var result = new List<ModelRef>
        {
            ModelRef.Host($"Host: {hostDoc.Title}"),
        };

        var links = new FilteredElementCollector(hostDoc)
            .OfClass(typeof(RevitLinkInstance))
            .Cast<RevitLinkInstance>()
            .ToList();

        foreach (var link in links)
        {
            var linkDoc = link.GetLinkDocument();
            if (linkDoc is null)
            {
                continue;
            }

            result.Add(ModelRef.Link(
                link.Id.IntegerValue,
                $"Link: {linkDoc.Title} ({link.Name})"));
        }

        return result;
    }

    public string ResolveDisplayName(ModelRef modelRef)
    {
        var hostDoc = _hostDocumentProvider();
        if (hostDoc is null)
        {
            return modelRef.DisplayName;
        }

        if (modelRef.Kind == ModelKind.Host)
        {
            return $"Host: {hostDoc.Title}";
        }

        if (modelRef.LinkInstanceId is null)
        {
            return modelRef.DisplayName;
        }

        var link = hostDoc.GetElement(new ElementId(modelRef.LinkInstanceId.Value)) as RevitLinkInstance;
        var linkDoc = link?.GetLinkDocument();
        return linkDoc is null
            ? modelRef.DisplayName
            : $"Link: {linkDoc.Title} ({link!.Name})";
    }

    public bool TryResolveModel(ModelRef modelRef, out ResolvedModel? resolved)
    {
        resolved = null;
        var hostDoc = _hostDocumentProvider();
        if (hostDoc is null)
        {
            return false;
        }

        if (modelRef.Kind == ModelKind.Host)
        {
            resolved = new ResolvedModel
            {
                Document = hostDoc,
                TransformToHost = Transform.Identity,
                StableKey = "host",
                DisplayName = $"Host: {hostDoc.Title}",
                ModelRef = ModelRef.Host($"Host: {hostDoc.Title}"),
            };
            return true;
        }

        if (modelRef.LinkInstanceId is null)
        {
            return false;
        }

        var linkId = new ElementId(modelRef.LinkInstanceId.Value);
        var linkInstance = hostDoc.GetElement(linkId) as RevitLinkInstance;
        if (linkInstance is null)
        {
            return false;
        }

        var linkDoc = linkInstance.GetLinkDocument();
        if (linkDoc is null)
        {
            return false;
        }

        resolved = new ResolvedModel
        {
            Document = linkDoc,
            TransformToHost = linkInstance.GetTotalTransform(),
            StableKey = $"link:{linkId.IntegerValue}",
            DisplayName = $"Link: {linkDoc.Title} ({linkInstance.Name})",
            ModelRef = ModelRef.Link(linkId.IntegerValue, $"Link: {linkDoc.Title} ({linkInstance.Name})"),
            LinkInstanceId = linkId,
        };
        return true;
    }

    public IReadOnlyList<string> GetAvailableModelCategoryNames()
    {
        var hostDoc = _hostDocumentProvider();
        if (hostDoc is null)
        {
            return Array.Empty<string>();
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddDocumentCategoryNames(hostDoc, names);

        var links = new FilteredElementCollector(hostDoc)
            .OfClass(typeof(RevitLinkInstance))
            .Cast<RevitLinkInstance>();
        foreach (var link in links)
        {
            var linkDoc = link.GetLinkDocument();
            if (linkDoc is not null)
            {
                AddDocumentCategoryNames(linkDoc, names);
            }
        }

        return names.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static void AddDocumentCategoryNames(Document doc, HashSet<string> destination)
    {
        var collector = new FilteredElementCollector(doc)
            .WhereElementIsNotElementType();
        foreach (var element in collector)
        {
            var category = element.Category;
            if (category is null || category.CategoryType != CategoryType.Model)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(category.Name))
            {
                destination.Add(category.Name);
            }
        }
    }
}
