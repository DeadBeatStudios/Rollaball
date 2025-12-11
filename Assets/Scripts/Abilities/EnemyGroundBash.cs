using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class EnemyGroundBash : MonoBehaviour
{
    [Header("Bash Settings")]
    public float bashCooldown = 1.5f;
    public float upwardLiftForce = 8f;
    public float slamDownForce = 20f;
    public float shockwaveForce = 25f;
    public float shockwaveRadius = 8f;
    public float liftDelay = 0.1f;

    [Header("Layers")]
    public LayerMask groundLayer;
    public LayerMask bashAffectsLayers;

    private Rigidbody rb;
    private float cooldownTimer = 0f;
    private bool isBashing = false;
    private Coroutine bashRoutine;

    // Public state access
    public bool IsBashing => isBashing;
    public float CooldownRemaining => cooldownTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    /// <summary>
    /// Attempts to start the bash sequence.
    /// Returns true if started, false if on cooldown or already bashing.
    /// </summary>
    public bool TryStartGroundBash()
    {
        if (isBashing)
            return false;

        if (cooldownTimer > 0f)
            return false;

        bashRoutine = StartCoroutine(BashSequence());
        return true;
    }

    private IEnumerator BashSequence()
    {
        isBashing = true;

        bool grounded = IsGrounded();

        // Optional lift if starting from ground
        if (grounded)
        {
            rb.AddForce(Vector3.up * upwardLiftForce, ForceMode.VelocityChange);
            yield return new WaitForSeconds(liftDelay);
        }

        // Slam down
        rb.AddForce(Vector3.down * slamDownForce, ForceMode.VelocityChange);

        // Wait until we hit the ground again
        while (!IsGrounded())
            yield return null;

        DoShockwave();

        cooldownTimer = bashCooldown;
        isBashing = false;
        bashRoutine = null;
    }

    private void DoShockwave()
    {
        Vector3 origin = transform.position;
        Collider[] hits = Physics.OverlapSphere(origin, shockwaveRadius, bashAffectsLayers);

        foreach (var hit in hits)
        {
            if (hit.transform == transform)
                continue;

            IKnockbackReceiver recv = hit.GetComponentInParent<IKnockbackReceiver>();
            if (recv != null)
            {
                Vector3 dir = (hit.transform.position - origin).normalized;
                dir.y = 0.25f;

                recv.ApplyKnockback(dir * shockwaveForce, 0.25f);
            }
        }
    }

    private bool IsGrounded()
    {
        return Physics.SphereCast(
            transform.position,
            0.45f,
            Vector3.down,
            out _,
            0.2f,
            groundLayer,
            QueryTriggerInteraction.Ignore);
    }
}
