using UnityEngine;

/// <summary>
/// Universal knockback system for Player and Enemy.
/// Attach to BOTH.
/// Automatically detects momentum difference and applies force.
/// No tags, no layers required.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class KnockbackOnCollision : MonoBehaviour
{
    [Header("Knockback Settings")]
    [Tooltip("Scales total knockback force")]
    public float knockbackMultiplier = 0.12f;

    [Tooltip("Minimum knockback force to avoid dead collisions")]
    public float minKnockback = 2f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private float GetHorizontalSpeed()
    {
        Vector3 vel = rb.linearVelocity;
        vel.y = 0;
        return vel.magnitude;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Only react if other object also uses this script
        if (!collision.gameObject.TryGetComponent(out KnockbackOnCollision other))
            return;

        ResolveKnockback(other, collision.transform);
    }

    private void ResolveKnockback(KnockbackOnCollision other, Transform otherTransform)
    {
        float mySpeed = GetHorizontalSpeed();
        float theirSpeed = other.GetHorizontalSpeed();

        // Momentum difference
        float momentumDiff = (mySpeed * mySpeed) - (theirSpeed * theirSpeed);

        if (Mathf.Abs(momentumDiff) < 0.01f)
            return; // tie → ignore small bumps

        Vector3 dir = (otherTransform.position - transform.position).normalized;

        // Convert momentum difference into force
        float force = Mathf.Abs(momentumDiff) * knockbackMultiplier;
        force = Mathf.Max(force, minKnockback);

        if (momentumDiff > 0)
        {
            // YOU hit harder → knock back opponent
            other.ApplyKnockback(dir, force);
        }
        else
        {
            // They hit harder → you get knocked back
            ApplyKnockback(-dir, force);
        }
    }

    public void ApplyKnockback(Vector3 direction, float force)
    {
        rb.AddForce(direction.normalized * force, ForceMode.Impulse);
    }
}
