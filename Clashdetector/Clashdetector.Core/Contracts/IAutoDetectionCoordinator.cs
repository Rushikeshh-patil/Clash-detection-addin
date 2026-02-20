using Clashdetector.Core.Models;

namespace Clashdetector.Core.Contracts;

public interface IAutoDetectionCoordinator
{
    bool IsAutoModeEnabled { get; }

    int DebounceMilliseconds { get; set; }

    void SetAutoMode(bool enabled);

    void RegisterChange(string modelStableKey, IEnumerable<int> changedElementIds);

    bool TryDequeueTrigger(out AutoDetectionTrigger trigger);

    void ClearPending();
}
