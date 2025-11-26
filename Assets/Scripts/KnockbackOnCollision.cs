using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KnockbackOnCollision : MonoBehaviour
{
    [Header("Knockback Settings")]
    [SerializeField] private float maxKnockbackStrength = 20f;
    [SerializeField] private float dropThreshold = 7.5f;   // Momentum needed to drop flag
    [SerializeField] private bool ignoreVertical = true;   // Horizontal-only collisions feel better

    private Rigidbody rb;
    private FlagPickup flag;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        flag = FindAnyObjectByType<FlagPickup>();
    }

    //-------------------------
    //   MOMENTUM CALCULATION
    //-------------------------
    private float GetMomentum()
    {
        Vector3 hVel = rb.linearVelocity;
        if (ignoreVertical) hVel.y = 0f;

        return rb.mass * hVel.magnitude;
    }

    private float GetMomentum(Rigidbody body)
    {
        Vector3 hVel = body.linearVelocity;
        if (ignoreVertical) hVel.y = 0f;

        return body.mass * hVel.magnitude;
    }

    //-------------------------
    //   COLLISION
    //-------------------------
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.rigidbody)
            return;

        Rigidbody otherRb = collision.rigidbody;

        // Calculate momentum difference
        float myMomentum = GetMomentum(rb);
        float theirMomentum = GetMomentum(otherRb);
        float diff = myMomentum - theirMomentum;

        // Close to equal? → small bump, skip launch
        if (Mathf.Abs(diff) < 0.15f)
            return;

        // Determine knockback direction
        Vector3 dirToOther = (collision.transform.position - transform.position).normalized;
        Vector3 dirToSelf = -dirToOther;

        // Calculate force magnitude (clamped)
        float rawForce = Mathf.Abs(diff);
        float finalForce = Mathf.Clamp(rawForce, 0f, maxKnockbackStrength);

        // Apply: who gets launched?
        if (diff > 0)
        {
            // I had more momentum → THEY get launched
            ApplyKnockback(otherRb, dirToOther * finalForce);
        }
        else
        {
            // They had more → I get launched
            ApplyKnockback(rb, dirToSelf * finalForce);
        }

        //-----------------------------------
        // FLAG DROP LOGIC (Momentum-based)
        //-----------------------------------
        if (flag != null && flag.IsHeld)
        {
            Transform holder = flag.CurrentHolder;

            // Only drop if the COLLISION involved the holder
            if (holder == transform || holder == collision.transform)
            {
                if (rawForce >= dropThreshold)
                {
                    Vector3 dropPoint = collision.contacts[0].point;
                    flag.DropToWorld(FlagPickup.FlagDropCause.Unknown, holder, dropPoint);
                }
            }
        }
    }

    private void ApplyKnockback(Rigidbody body, Vector3 force)
    {
        if (ignoreVertical)
            force.y = 0f;

        body.AddForce(force, ForceMode.VelocityChange);
    }
}
