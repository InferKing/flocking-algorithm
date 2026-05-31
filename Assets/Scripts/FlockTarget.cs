using UnityEngine;
using Random = System.Random;

public class FlockTarget : MonoBehaviour
{
    [SerializeField]
    private bool _makeActiveOnEnable = true;

    [SerializeField, Min(0f)]
    private float _spreadRadius = 1.2f;

    [SerializeField, Min(0f)]
    private float _verticalSpread = 0.25f;

    [SerializeField, Min(0f)]
    private float _arrivalRadius = 0.75f;

    [SerializeField]
    private bool _drawGizmos = true;

    public float SpreadRadius => _spreadRadius;
    public float ArrivalRadius => _arrivalRadius;
    public bool IsActive => FlockTargetRegistry.Active == this;

    private System.Random _random;
    
    private void OnEnable()
    {
        FlockTargetRegistry.Register(this, _makeActiveOnEnable);
    }

    private void OnDisable()
    {
        FlockTargetRegistry.Unregister(this);
    }

    public void MakeActive()
    {
        FlockTargetRegistry.MakeActive(this);
    }

    public Vector3 GetAssignedPosition(int seed)
    {
        return transform.position + transform.rotation * GetStableOffset(seed);
    }

    private void InitializeSeed(int seed)
    {
        if (_random != null)
            return;

        _random = new Random(seed & int.MaxValue);
    }
    
    private Vector3 GetStableOffset(int seed)
    {
        InitializeSeed(seed);
        
        if (_spreadRadius <= 0f && _verticalSpread <= 0f)
        {
            return Vector3.zero;
        }

        var angle = (float)_random.NextDouble() * Mathf.PI * 2f;
        var radius = Mathf.Sqrt((float)_random.NextDouble()) * _spreadRadius;
        var height = ((float)_random.NextDouble() * 2f - 1f) * _verticalSpread;

        return new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
    }

    private void OnDrawGizmosSelected()
    {
        if (!_drawGizmos)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _spreadRadius);

        if (_arrivalRadius > 0f)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, _arrivalRadius);
        }
    }
}
