using System.Collections.Generic;
using UnityEngine;

public readonly struct FlockAgentSnapshot
{
    public FlockAgentSnapshot(FlockingLogic agent, Vector3 position, Vector3 forward)
    {
        Agent = agent;
        Position = position;
        Forward = forward;
    }

    public FlockingLogic Agent { get; }
    public Vector3 Position { get; }
    public Vector3 Forward { get; }
}

public static class FlockAgentRegistry
{
    private static readonly List<FlockingLogic> _agents = new();
    private static readonly Dictionary<Vector3Int, List<FlockAgentSnapshot>> _grid = new();
    private static readonly Stack<List<FlockAgentSnapshot>> _cellPool = new();

    private static bool _gridDirty = true;
    private static int _gridFrame = -1;
    private static int _nextSteeringIndex;
    private static float _cellSize = 1f;

    public static IReadOnlyList<FlockingLogic> Agents => _agents;

    public static void Register(FlockingLogic agent)
    {
        if (agent == null || _agents.Contains(agent))
        {
            return;
        }

        _agents.Add(agent);
        agent.SetSteeringIndex(_nextSteeringIndex);
        _nextSteeringIndex++;
        _gridDirty = true;
    }

    public static void Unregister(FlockingLogic agent)
    {
        if (_agents.Remove(agent))
        {
            _gridDirty = true;
        }
    }

    public static void GetNearby(Vector3 position, float radius, List<FlockAgentSnapshot> results)
    {
        results.Clear();

        if (radius <= 0f)
        {
            return;
        }

        EnsureGrid(radius);

        var centerCell = GetCell(position);
        var cellRange = Mathf.CeilToInt(radius / _cellSize);

        for (var x = -cellRange; x <= cellRange; x++)
        {
            for (var y = -cellRange; y <= cellRange; y++)
            {
                for (var z = -cellRange; z <= cellRange; z++)
                {
                    var cell = new Vector3Int(centerCell.x + x, centerCell.y + y, centerCell.z + z);
                    if (!_grid.TryGetValue(cell, out var agents))
                    {
                        continue;
                    }

                    results.AddRange(agents);
                }
            }
        }
    }

    private static void EnsureGrid(float queryRadius)
    {
        var requestedCellSize = Mathf.Max(queryRadius, 0.01f);
        if (requestedCellSize > _cellSize)
        {
            _cellSize = requestedCellSize;
            _gridDirty = true;
        }

        if (!_gridDirty && _gridFrame == Time.frameCount)
        {
            return;
        }

        RebuildGrid();
    }

    private static void RebuildGrid()
    {
        RecycleGrid();

        foreach (var agent in _agents)
        {
            if (agent == null || !agent.isActiveAndEnabled)
            {
                continue;
            }

            var agentTransform = agent.CachedTransform;
            if (ReferenceEquals(agentTransform, null))
            {
                continue;
            }

            var snapshot = new FlockAgentSnapshot(agent, agentTransform.position, agentTransform.forward);
            var cell = GetCell(snapshot.Position);
            if (!_grid.TryGetValue(cell, out var agents))
            {
                agents = GetCellAgentsList();
                _grid[cell] = agents;
            }

            agents.Add(snapshot);
        }

        _gridDirty = false;
        _gridFrame = Time.frameCount;
    }

    private static void RecycleGrid()
    {
        foreach (var cellAgents in _grid.Values)
        {
            cellAgents.Clear();
            _cellPool.Push(cellAgents);
        }

        _grid.Clear();
    }

    private static List<FlockAgentSnapshot> GetCellAgentsList()
    {
        return _cellPool.Count > 0 ? _cellPool.Pop() : new List<FlockAgentSnapshot>();
    }

    private static Vector3Int GetCell(Vector3 position)
    {
        return new Vector3Int(
            Mathf.FloorToInt(position.x / _cellSize),
            Mathf.FloorToInt(position.y / _cellSize),
            Mathf.FloorToInt(position.z / _cellSize));
    }
}
