namespace Clashdetector.Core.Contracts;

public interface IClashSuggestionProvider
{
    string GetSuggestion(string categoryA, string categoryB);
}
