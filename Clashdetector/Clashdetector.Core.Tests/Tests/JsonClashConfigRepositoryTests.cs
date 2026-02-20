using Clashdetector.Core.Models;
using Clashdetector.Core.Services;

namespace Clashdetector.Core.Tests.Tests;

public sealed class JsonClashConfigRepositoryTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsConfiguration()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "ClashdetectorTests", Guid.NewGuid().ToString("N"));
        var settingsFile = Path.Combine(tempRoot, "settings.json");
        var logDir = Path.Combine(tempRoot, "logs");
        var logger = new DiagnosticLogger(logDir);
        var repository = new JsonClashConfigRepository(settingsFile, logger);

        var state = new ClashSettingsState
        {
            AutoModeEnabled = true,
            AutoDebounceMilliseconds = 2200,
            Configs =
            {
                new ClashConfig
                {
                    Name = "MEP vs Struct",
                    IsActive = true,
                    ModelA = ModelRef.Host("Main Host"),
                    ModelB = ModelRef.Link(42, "Coord Link"),
                    CategoryPairs =
                    {
                        new CategoryPairRule
                        {
                            CategoryA = "Ducts",
                            CategoryB = "Structural Framing",
                            Enabled = true,
                        },
                    },
                    SeverityThresholds = new SeverityThresholds
                    {
                        MediumMin = 0.2,
                        HighMin = 0.8,
                    },
                },
            },
        };
        state.LastSelectedConfigId = state.Configs[0].Id;

        repository.Save(state);
        var loaded = repository.Load();

        Assert.True(loaded.AutoModeEnabled);
        Assert.Equal(2200, loaded.AutoDebounceMilliseconds);
        Assert.Single(loaded.Configs);
        Assert.Equal("MEP vs Struct", loaded.Configs[0].Name);
        Assert.Equal(ModelKind.Link, loaded.Configs[0].ModelB.Kind);
        Assert.Equal(42, loaded.Configs[0].ModelB.LinkInstanceId);
        Assert.Single(loaded.Configs[0].CategoryPairs);
        Assert.Equal("Structural Framing", loaded.Configs[0].CategoryPairs[0].CategoryB);
    }
}
