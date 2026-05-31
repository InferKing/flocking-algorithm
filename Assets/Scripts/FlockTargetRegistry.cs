using System.Collections.Generic;

public static class FlockTargetRegistry
{
    private static readonly List<FlockTarget> _targets = new();

    public static FlockTarget Active { get; private set; }

    public static void Register(FlockTarget target, bool makeActive)
    {
        if (target == null)
        {
            return;
        }

        if (!_targets.Contains(target))
        {
            _targets.Add(target);
        }

        if (makeActive || Active == null)
        {
            Active = target;
        }
    }

    public static void Unregister(FlockTarget target)
    {
        _targets.Remove(target);

        if (Active == target)
        {
            Active = _targets.Count > 0 ? _targets[0] : null;
        }
    }

    public static void MakeActive(FlockTarget target)
    {
        if (target == null)
        {
            return;
        }

        if (!_targets.Contains(target))
        {
            _targets.Add(target);
        }

        Active = target;
    }
}
