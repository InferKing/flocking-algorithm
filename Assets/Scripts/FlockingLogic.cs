using System.Collections.Generic;
using FishAlive;
using UnityEngine;

[RequireComponent(typeof(FishMotion))]
public class FlockingLogic : MonoBehaviour
{
    private static readonly List<FlockingLogic> Agents = new();

    [Header("Target")]
    [SerializeField] private FlockTarget target;
    [SerializeField] private bool useActiveTarget = true;
    [SerializeField, Min(0f)] private float targetWeight = 1.2f;

    [Header("Flock")]
    [SerializeField, Min(0.01f)] private float neighborRadius = 1.5f;
    [SerializeField, Min(0.01f)] private float separationRadius = 0.45f;
    [SerializeField, Min(0f)] private float separationWeight = 1.7f;
    [SerializeField, Min(0f)] private float alignmentWeight = 0.8f;
    [SerializeField, Min(0f)] private float cohesionWeight = 0.6f;
    [SerializeField, Min(0f)] private float forwardWeight = 0.2f;

    [Header("Motion")]
    [SerializeField, Min(0f)] private float cruiseMotionForce = 1.0f;
    [SerializeField, Min(0f)] private float farTargetMotionForce = 1.6f;
    [SerializeField, Min(0f)] private float nearTargetMotionForce = 0.45f;
    [SerializeField, Min(0.02f)] private float steeringInterval = 0.12f;
    [SerializeField, Min(0.1f)] private float turnVelocityMultiplier = 1.0f;
    [SerializeField] private bool abortActiveTurns = false;
    [SerializeField] private bool enableFishAvoidance = true;

    private FishMotion fishMotion;
    private float nextSteeringTime;
    private int stableSeed;
    private bool fishMotionConfigured;

    private void Awake()
    {
        fishMotion = GetComponent<FishMotion>();
        stableSeed = GetInstanceID();
    }

    private void OnEnable()
    {
        if (!Agents.Contains(this))
        {
            Agents.Add(this);
        }

        fishMotionConfigured = false;
    }

    private void OnDisable()
    {
        Agents.Remove(this);
    }

    private void Start()
    {
        nextSteeringTime = Time.time + Random.value * steeringInterval;
    }

    private void LateUpdate()
    {
        if (fishMotionConfigured)
        {
            return;
        }

        ConfigureFishMotion();
        fishMotionConfigured = true;
    }

    private void ConfigureFishMotion()
    {
        fishMotion.SetReachMode(ReachMode.Wander);
        fishMotion.SetAutoMotion(false);
        fishMotion.SetAvoidanceEnabled(enableFishAvoidance);
        fishMotion.SetMotionForce(cruiseMotionForce);
    }

    private void Update()
    {
        if (!fishMotionConfigured)
        {
            return;
        }

        if (Time.time < nextSteeringTime)
        {
            return;
        }

        Steer();
        nextSteeringTime = Time.time + steeringInterval;
    }

    public void SetTarget(FlockTarget newTarget)
    {
        target = newTarget;
    }

    private void Steer()
    {
        Vector3 finalDirection = CalculateFlockDirection();
        float motionForce = CalculateMotionForce();

        if (finalDirection.sqrMagnitude < 0.0001f)
        {
            finalDirection = transform.forward;
        }

        fishMotion.SetMotionForce(motionForce);
        fishMotion.StartTurnTowardsDirection(finalDirection.normalized, abortActiveTurns, turnVelocityMultiplier);
    }

    private Vector3 CalculateFlockDirection()
    {
        Vector3 position = transform.position;
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesionCenter = Vector3.zero;

        int neighborCount = 0;
        int separationCount = 0;
        float neighborRadiusSqr = neighborRadius * neighborRadius;
        float separationRadiusSqr = separationRadius * separationRadius;

        for (int i = 0; i < Agents.Count; i++)
        {
            FlockingLogic other = Agents[i];
            if (other == this || !other.isActiveAndEnabled)
            {
                continue;
            }

            Vector3 toOther = other.transform.position - position;
            float sqrDistance = toOther.sqrMagnitude;
            if (sqrDistance <= 0.0001f || sqrDistance > neighborRadiusSqr)
            {
                continue;
            }

            neighborCount++;
            alignment += other.transform.forward;
            cohesionCenter += other.transform.position;

            if (sqrDistance < separationRadiusSqr)
            {
                float distance = Mathf.Sqrt(sqrDistance);
                separation -= toOther / Mathf.Max(distance * distance, 0.0001f);
                separationCount++;
            }
        }

        Vector3 direction = transform.forward * forwardWeight;

        if (separationCount > 0)
        {
            direction += separation.normalized * separationWeight;
        }

        if (neighborCount > 0)
        {
            direction += (alignment / neighborCount).normalized * alignmentWeight;

            Vector3 center = cohesionCenter / neighborCount;
            Vector3 toCenter = center - position;
            if (toCenter.sqrMagnitude > 0.0001f)
            {
                direction += toCenter.normalized * cohesionWeight;
            }
        }

        FlockTarget resolvedTarget = ResolveTarget();
        if (resolvedTarget != null)
        {
            Vector3 targetPoint = resolvedTarget.GetAssignedPosition(stableSeed);
            Vector3 toTarget = targetPoint - position;
            float distanceToTarget = toTarget.magnitude;

            if (distanceToTarget > 0.0001f)
            {
                float arrivalFactor = resolvedTarget.ArrivalRadius > 0f
                    ? Mathf.Clamp01(distanceToTarget / resolvedTarget.ArrivalRadius)
                    : 1f;

                direction += toTarget.normalized * targetWeight * arrivalFactor;
            }
        }

        return direction;
    }

    private float CalculateMotionForce()
    {
        FlockTarget resolvedTarget = ResolveTarget();
        if (resolvedTarget == null)
        {
            return cruiseMotionForce;
        }

        float distanceToTarget = Vector3.Distance(transform.position, resolvedTarget.GetAssignedPosition(stableSeed));
        if (distanceToTarget <= resolvedTarget.ArrivalRadius)
        {
            return nearTargetMotionForce;
        }

        if (distanceToTarget >= resolvedTarget.ArrivalRadius + resolvedTarget.SpreadRadius + neighborRadius)
        {
            return farTargetMotionForce;
        }

        return cruiseMotionForce;
    }

    private FlockTarget ResolveTarget()
    {
        if (target != null)
        {
            return target;
        }

        return useActiveTarget ? FlockTarget.Active : null;
    }
}
