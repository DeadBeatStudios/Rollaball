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

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private float GetHorizSpeed(Rigidbody b)
    {
        Vector3 v = b.linearVelocity;
        if (ignoreVertical) v.y = 0;
        return v.magnitude;
    }

    private void OnCollisionEnter(Collision c)
    {
        if (!c.rigidbody) return;

        Rigidbody other = c.rigidbody;

        if (GetInstanceID() > other.GetInstanceID())
            return;

        float mySpeed = GetHorizSpeed(rb);
        float theirSpeed = GetHorizSpeed(other);
        float speedDiff = mySpeed - theirSpeed;

        if (Mathf.Abs(speedDiff) < minSpeedDifference)
            return;

        Vector3 relVel = rb.linearVelocity - other.linearVelocity;
        if (ignoreVertical) relVel.y = 0;

        Vector3 dir = relVel.normalized;
        float relativeSpeed = relVel.magnitude;

        float force = relativeSpeed * momentumMultiplier * baseKnockback;
        force = Mathf.Clamp(force, 0f, maxKnockback);

        // Target receives full knockback
        IKnockbackReceiver recvTarget = other.GetComponentInParent<IKnockbackReceiver>();
        if (recvTarget != null)
        {
            recvTarget.ApplyKnockback(dir * force, 0.25f);
        }

        // Self receives recoil
        IKnockbackReceiver recvSelf = GetComponentInParent<IKnockbackReceiver>();
        if (recvSelf != null)
        {
            recvSelf.ApplyKnockback(-dir * (force * winnerRecoilPercent), 0.2f);
        }
    }
}
