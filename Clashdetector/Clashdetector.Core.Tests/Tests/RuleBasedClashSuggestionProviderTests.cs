using Clashdetector.Core.Services;

namespace Clashdetector.Core.Tests.Tests;

public sealed class RuleBasedClashSuggestionProviderTests
{
    [Fact]
    public void GetSuggestion_ReturnsMappedSuggestionRegardlessOfPairOrder()
    {
        var provider = new RuleBasedClashSuggestionProvider();

        var one = provider.GetSuggestion("Ducts", "Pipes");
        var two = provider.GetSuggestion("Pipes", "Ducts");

        Assert.Equal(one, two);
        Assert.Contains("offset", one, StringComparison.OrdinalIgnoreCase);
    }
}
