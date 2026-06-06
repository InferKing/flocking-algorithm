using UnityEngine;

public class FlockSteeringManager : MonoBehaviour
{
    [SerializeField, Min(1)]
    private int _steeringFrameSpread = 5;

    private void LateUpdate()
    {
        var frameSpread = Mathf.Max(1, _steeringFrameSpread);
        var frameBucket = Time.frameCount % frameSpread;
        var agents = FlockAgentRegistry.Agents;

        for (var i = 0; i < agents.Count; i++)
        {
            var agent = agents[i];
            if (ReferenceEquals(agent, null))
            {
                continue;
            }

            if (agent.SteeringIndex % frameSpread != frameBucket)
            {
                continue;
            }

            agent.Steer();
        }
    }
}
