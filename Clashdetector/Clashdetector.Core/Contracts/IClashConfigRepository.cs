using Clashdetector.Core.Models;

namespace Clashdetector.Core.Contracts;

public interface IClashConfigRepository
{
    ClashSettingsState Load();

    void Save(ClashSettingsState state);
}
