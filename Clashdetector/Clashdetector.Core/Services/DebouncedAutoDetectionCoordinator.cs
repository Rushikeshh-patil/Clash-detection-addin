using Clashdetector.Core.Contracts;
using Clashdetector.Core.Models;

namespace Clashdetector.Core.Services;

public sealed class DebouncedAutoDetectionCoordinator : IAutoDetectionCoordinator
{
    private readonly object _sync = new();
    private readonly Dictionary<string, HashSet<int>> _pending = new(StringComparer.OrdinalIgnoreCase);
    private DateTimeOffset? _nextRunUtc;

    public bool IsAutoModeEnabled { get; private set; }

    public int DebounceMilliseconds { get; set; } = 1500;

    public void SetAutoMode(bool enabled)
    {
        lock (_sync)
        {
            IsAutoModeEnabled = enabled;
            if (!enabled)
            {
                _pending.Clear();
                _nextRunUtc = null;
            }
        }
    }

    public void RegisterChange(string modelStableKey, IEnumerable<int> changedElementIds)
    {
        if (string.IsNullOrWhiteSpace(modelStableKey))
        {
            return;
        }

        lock (_sync)
        {
            if (!IsAutoModeEnabled)
            {
                return;
            }

            if (!_pending.TryGetValue(modelStableKey, out var set))
            {
                set = new HashSet<int>();
                _pending[modelStableKey] = set;
            }

            foreach (var id in changedElementIds)
            {
                if (id > 0)
                {
                    set.Add(id);
                }
            }

            var debounce = DebounceMilliseconds < 250 ? 250 : DebounceMilliseconds;
            _nextRunUtc = DateTimeOffset.UtcNow.AddMilliseconds(debounce);
        }
    }

    public bool TryDequeueTrigger(out AutoDetectionTrigger trigger)
    {
        lock (_sync)
        {
            trigger = new AutoDetectionTrigger();

            if (!IsAutoModeEnabled || _nextRunUtc is null || DateTimeOffset.UtcNow < _nextRunUtc.Value)
            {
                return false;
            }

            if (_pending.Count == 0)
            {
                _nextRunUtc = null;
                return false;
            }

            var snapshot = new Dictionary<string, IReadOnlyCollection<int>>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in _pending)
            {
                snapshot[pair.Key] = pair.Value.ToArray();
            }

            _pending.Clear();
            _nextRunUtc = null;
            trigger = new AutoDetectionTrigger
            {
                TriggeredAtUtc = DateTimeOffset.UtcNow,
                ChangedElementIdsByModel = snapshot,
            };
            return true;
        }
    }

    public void ClearPending()
    {
        lock (_sync)
        {
            _pending.Clear();
            _nextRunUtc = null;
        }
    }
}
