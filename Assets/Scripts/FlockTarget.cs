using System.Collections.Generic;
using UnityEngine;

public class FlockTarget : MonoBehaviour
{
    private static readonly List<FlockTarget> Targets = new();

    [SerializeField] private bool makeActiveOnEnable = true;
    [SerializeField, Min(0f)] private float spreadRadius = 1.2f;
    [SerializeField, Min(0f)] private float verticalSpread = 0.25f;
    [SerializeField, Min(0f)] private float arrivalRadius = 0.75f;
    [SerializeField] private bool drawGizmos = true;

    public static FlockTarget Active { get; private set; }
    public float SpreadRadius => spreadRadius;
    public float ArrivalRadius => arrivalRadius;

    private void OnEnable()
    {
        if (!Targets.Contains(this))
        {
            Targets.Add(this);
        }

        if (makeActiveOnEnable || Active == null)
        {
            Active = this;
        }
    }

    private void OnDisable()
    {
        Targets.Remove(this);

        if (Active == this)
        {
            Active = Targets.Count > 0 ? Targets[0] : null;
        }
    }

    public void MakeActive()
    {
        Active = this;
    }

    public Vector3 GetAssignedPosition(int seed)
    {
        return transform.position + transform.rotation * GetStableOffset(seed);
    }

    private Vector3 GetStableOffset(int seed)
    {
        if (spreadRadius <= 0f && verticalSpread <= 0f)
        {
            return Vector3.zero;
        }

        float angle = Hash01(seed, 17) * Mathf.PI * 2f;
        float radius = Mathf.Sqrt(Hash01(seed, 31)) * spreadRadius;
        float height = (Hash01(seed, 47) * 2f - 1f) * verticalSpread;

        return new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
    }

    private static float Hash01(int seed, int salt)
    {
        unchecked
        {
            uint value = (uint)seed;
            value ^= (uint)salt * 0x9E3779B9u;
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return (value & 0x00FFFFFFu) / 16777215f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spreadRadius);

        if (arrivalRadius > 0f)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, arrivalRadius);
        }
    }
}
