using System.Collections.Generic;

public static class FlockAgentRegistry
{
    private static readonly List<FlockingLogic> _agents = new();

    public static IReadOnlyList<FlockingLogic> Agents => _agents;

    public static void Register(FlockingLogic agent)
    {
        if (agent == null || _agents.Contains(agent))
        {
            return;
        }

        _agents.Add(agent);
    }

    public static void Unregister(FlockingLogic agent)
    {
        _agents.Remove(agent);
    }
}
