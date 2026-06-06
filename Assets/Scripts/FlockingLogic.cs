using System.Collections.Generic;
using FishAlive;
using UnityEngine;

[RequireComponent(typeof(FishMotion))]
public class FlockingLogic : MonoBehaviour
{
    [Header("Target")]
    [SerializeField]
    private FlockTarget _target;

    [SerializeField]
    private bool _useActiveTarget = true;

    [SerializeField, Min(0f)]
    private float _targetWeight = 1.2f;

    [Header("Flock")]
    [SerializeField, Min(0.01f)]
    private float _neighborRadius = 1.5f;

    [SerializeField, Min(0.01f)]
    private float _separationRadius = 0.45f;

    [SerializeField, Min(0f)]
    private float _separationWeight = 1.7f;

    [SerializeField, Min(0f)]
    private float _alignmentWeight = 0.8f;

    [SerializeField, Min(0f)]
    private float _cohesionWeight = 0.6f;

    [SerializeField, Min(0f)]
    private float _forwardWeight = 0.2f;

    [Header("Motion")]
    [SerializeField, Min(0f)]
    private float _cruiseMotionForce = 1.0f;

    [SerializeField, Min(0f)]
    private float _farTargetMotionForce = 1.6f;

    [SerializeField, Min(0f)]
    private float _nearTargetMotionForce = 0.45f;

    [SerializeField, Min(0.1f)]
    private float _turnVelocityMultiplier = 1.0f;

    [SerializeField]
    private bool _abortActiveTurns = false;

    [SerializeField]
    private bool _enableFishAvoidance = true;

    private FishMotion _fishMotion;
    private Transform _transform;
    private readonly List<FlockAgentSnapshot> _nearbyAgents = new();
    private int _stableSeed;
    private int _steeringIndex;

    internal Transform CachedTransform => _transform;
    internal int SteeringIndex => _steeringIndex;

    private void Awake()
    {
        _transform = transform;
        _fishMotion = GetComponent<FishMotion>();
        _stableSeed = GetInstanceID();
    }

    private void OnEnable()
    {
        FlockAgentRegistry.Register(this);
    }

    private void OnDisable()
    {
        FlockAgentRegistry.Unregister(this);
    }

    private void Start()
    {
        _fishMotion.SetReachMode(ReachMode.Wander);
        _fishMotion.SetAutoMotion(false);
        _fishMotion.SetAvoidanceEnabled(_enableFishAvoidance);
        _fishMotion.SetMotionForce(_cruiseMotionForce);
    }

    /// <summary>
    /// If need to set target manually
    /// </summary>
    /// <param name="newTarget">new Target, man</param>
    public void SetTarget(FlockTarget newTarget)
    {
        _target = newTarget;
        _useActiveTarget = newTarget == null;
    }

    internal void SetSteeringIndex(int steeringIndex)
    {
        _steeringIndex = steeringIndex;
    }

    internal void Steer()
    {
        var finalDirection = CalculateFlockDirection();
        var motionForce = CalculateMotionForce();

        if (finalDirection.sqrMagnitude < 0.0001f)
        {
            finalDirection = _transform.forward;
        }

        _fishMotion.SetMotionForce(motionForce);
        _fishMotion.StartTurnTowardsDirection(finalDirection.normalized, _abortActiveTurns, _turnVelocityMultiplier);
    }

    private Vector3 CalculateFlockDirection()
    {
        var position = _transform.position;
        var forward = _transform.forward;
        var separation = Vector3.zero;
        var alignment = Vector3.zero;
        var cohesionCenter = Vector3.zero;

        var neighborCount = 0;
        var separationCount = 0;
        var neighborRadiusSqr = _neighborRadius * _neighborRadius;
        var separationRadiusSqr = _separationRadius * _separationRadius;

        FlockAgentRegistry.GetNearby(position, _neighborRadius, _nearbyAgents);

        for (var i = 0; i < _nearbyAgents.Count; i++)
        {
            var other = _nearbyAgents[i];
            if (ReferenceEquals(other.Agent, this))
            {
                continue;
            }

            var otherPosition = other.Position;
            var toOther = otherPosition - position;
            var sqrDistance = toOther.sqrMagnitude;
            if (sqrDistance <= 0.0001f || sqrDistance > neighborRadiusSqr)
            {
                continue;
            }

            neighborCount++;
            alignment += other.Forward;
            cohesionCenter += otherPosition;

            if (sqrDistance < separationRadiusSqr)
            {
                var distance = Mathf.Sqrt(sqrDistance);
                separation -= toOther / Mathf.Max(distance * distance, 0.0001f);
                separationCount++;
            }
        }

        var direction = forward * _forwardWeight;

        if (separationCount > 0)
        {
            direction += separation.normalized * _separationWeight;
        }

        if (neighborCount > 0)
        {
            direction += (alignment / neighborCount).normalized * _alignmentWeight;

            var center = cohesionCenter / neighborCount;
            var toCenter = center - position;
            if (toCenter.sqrMagnitude > 0.0001f)
            {
                direction += toCenter.normalized * _cohesionWeight;
            }
        }

        var resolvedTarget = ResolveTarget();
        
        if (!ReferenceEquals(resolvedTarget, null))
        {
            var targetPoint = resolvedTarget.GetAssignedPosition(_stableSeed);
            var toTarget = targetPoint - position;
            var distanceToTarget = toTarget.magnitude;

            if (distanceToTarget > 0.0001f)
            {
                var arrivalFactor = resolvedTarget.ArrivalRadius > 0f
                    ? Mathf.Clamp01(distanceToTarget / resolvedTarget.ArrivalRadius)
                    : 1f;

                direction += toTarget.normalized * (_targetWeight * arrivalFactor);
            }
        }

        return direction;
    }

    private float CalculateMotionForce()
    {
        var resolvedTarget = ResolveTarget();
        
        if (ReferenceEquals(resolvedTarget, null))
        {
            return _cruiseMotionForce;
        }

        var distanceToTarget = Vector3.Distance(_transform.position, resolvedTarget.GetAssignedPosition(_stableSeed));
        if (distanceToTarget <= resolvedTarget.ArrivalRadius)
        {
            return _nearTargetMotionForce;
        }

        if (distanceToTarget >= resolvedTarget.ArrivalRadius + resolvedTarget.SpreadRadius + _neighborRadius)
        {
            return _farTargetMotionForce;
        }

        return _cruiseMotionForce;
    }

    private FlockTarget ResolveTarget()
    {
        if (!ReferenceEquals(_target, null))
        {
            return _target;
        }

        return _useActiveTarget ? FlockTargetRegistry.Active : null;
    }
}
