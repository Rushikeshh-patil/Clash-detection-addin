using Clashdetector.Core.Models;

namespace Clashdetector.Core.Contracts;

public interface IModelResolver
{
    IReadOnlyList<ModelRef> GetAvailableModels();

    string ResolveDisplayName(ModelRef modelRef);
}
