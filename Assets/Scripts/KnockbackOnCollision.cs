using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KnockbackOnCollision : MonoBehaviour
{
    [Header("Knockback Settings")]
    [SerializeField] private float baseKnockback = 14f;
    [SerializeField] private float momentumMultiplier = 1.25f;
    [SerializeField] private float winnerRecoilPercent = 0.25f;
    [SerializeField] private float minSpeedDifference = 0.15f;
    [SerializeField] private float maxKnockback = 22f;
    [SerializeField] private bool ignoreVertical = true;

    [Header("Flag Drop")]
    [SerializeField] private float dropThreshold = 8f;

    private Rigidbody rb;
    private FlagPickup flag;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private FlagPickup Flag
    {
        get
        {
            if (flag == null)
                flag = FindFirstObjectByType<FlagPickup>();
            return flag;
        }
    }

    private float GetHorizontalSpeed(Rigidbody b)
    {
        Vector3 v = b.linearVelocity;
        if (ignoreVertical) v.y = 0;
        return v.magnitude;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.rigidbody) return;

        Rigidbody other = collision.rigidbody;

        // ---- SINGLE AUTHORITY: prevents double knockback ----
        if (GetInstanceID() > other.GetInstanceID()) return;

        float mySpeed = GetHorizontalSpeed(rb);
        float theirSpeed = GetHorizontalSpeed(other);
        float speedDiff = mySpeed - theirSpeed;

        // Too small of an impact
        if (Mathf.Abs(speedDiff) < minSpeedDifference)
            return;

        // ---- RELATIVE VELOCITY for true impact direction ----
        Vector3 relVel = rb.linearVelocity - other.linearVelocity;
        if (ignoreVertical) relVel.y = 0;

        Vector3 dir = relVel.normalized;
        float relativeSpeed = relVel.magnitude;

        // ---- MOMENTUM-BASED FORCE (your version) ----
        float force = relativeSpeed * momentumMultiplier * baseKnockback;
        force = Mathf.Clamp(force, 0f, maxKnockback);

        // ---- APPLY HYBRID KNOCKBACK ----
        if (speedDiff > 0)
        {
            // YOU ARE FASTER → launch opponent
            other.AddForce(dir * force, ForceMode.Impulse);

            // winner gets light bounce for fun feel
            rb.AddForce(-dir * (force * winnerRecoilPercent), ForceMode.Impulse);
        }
        else
        {
            // YOU ARE SLOWER → opponent launches YOU
            rb.AddForce(-dir * force, ForceMode.Impulse);

            // faster opponent gets light bounce
            other.AddForce(dir * (force * winnerRecoilPercent), ForceMode.Impulse);
        }

        // ---- FLAG DROP LOGIC ----
        TryDropFlag(collision, relativeSpeed, dir);
    }

    private void TryDropFlag(Collision collision, float impactForce, Vector3 dir)
    {
        if (Flag == null || !Flag.IsHeld) return;

        Transform holder = Flag.CurrentHolder;
        if (holder == null) return;

        // Must involve the holder
        if (holder != transform && holder != collision.transform)
            return;

        // Not enough force to drop
        if (impactForce < dropThreshold)
            return;

        Vector3 dropPoint = collision.contacts.Length > 0 ?
            collision.contacts[0].point :
            holder.position;

        Flag.DropToWorld(FlagPickup.FlagDropCause.Unknown, holder, dropPoint);
    }
}
