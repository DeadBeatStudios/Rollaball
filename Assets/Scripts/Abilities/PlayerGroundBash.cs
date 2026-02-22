using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerGroundBash : MonoBehaviour
{
    [Header("Bash Settings")]
    public float bashCooldown = 1.5f;

    [Tooltip("If grounded, we pop up slightly before slamming.")]
    public float upwardLiftForce = 8f;

    [Tooltip("Guaranteed downward slam SPEED (not force). This is the key fix.")]
    public float slamDownSpeed = 20f;

    public float shockwaveForce = 25f;
    public float shockwaveRadius = 8f;
    public float liftDelay = 0.1f;

    [Header("Layers")]
    public LayerMask groundLayer;
    public LayerMask bashAffectsLayers;

    [Header("Optional Integration")]
    [Tooltip("If assigned, locks normal movement during bash.")]
    [SerializeField] private PlayerController playerController;

    [Tooltip("If assigned, prevents bash while staggered.")]
    [SerializeField] private KnockbackHandler knockback;

    private Rigidbody rb;
    private float cooldownTimer = 0f;
    private bool isBashing = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (knockback == null)
            knockback = GetComponent<KnockbackHandler>();
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    public void OnBash(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            TryStartBash();
    }

    private void TryStartBash()
    {
        if (isBashing)
            return;

        if (cooldownTimer > 0f)
            return;

        if (knockback != null && knockback.IsStaggered)
            return;

        StartCoroutine(BashSequence());
    }

    private IEnumerator BashSequence()
    {
        isBashing = true;
        cooldownTimer = bashCooldown; // start cooldown immediately so you can't stack bashes

        if (playerController != null)
            playerController.SetExternalMovementLock(true);

        bool groundedAtStart = IsGrounded();

        // If grounded, do a small pop-up first (feel/telegraph)
        if (groundedAtStart)
        {
            // Clear vertical velocity so the lift is consistent
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(v.x, 0f, v.z);

            rb.AddForce(Vector3.up * upwardLiftForce, ForceMode.VelocityChange);
            yield return new WaitForSeconds(liftDelay);
        }

        // COMMIT SLAM:
        // Force a guaranteed downward speed regardless of current vertical velocity.
        {
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(v.x, -Mathf.Abs(slamDownSpeed), v.z);
        }

        // Wait until we actually hit ground
        while (!IsGrounded())
            yield return null;

        DoShockwave();

        if (playerController != null)
            playerController.SetExternalMovementLock(false);

        isBashing = false;
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