using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyDash : MonoBehaviour
{
    [Header("Dash Settings")]
    [Tooltip("Horizontal Dash Speed.")]
    public float dashSpeed = 24f;

    [Tooltip("How long the dash lasts (seconds).")]
    public float dashDuration = 0.18f;

    [Tooltip("Cooldown between dashes (seconds).")]
    public float dashCooldown = 0.6f;

    [Tooltip("Minimum horizontal speed before we consider using current velocity as dash direction when no explicit direction is given.")]
    public float minVelocityForDirection = 0.2f;

    [Header("References")]
    [Tooltip("Optional: Knockback handler to prevent dashing while staggered.")]
    public KnockbackHandler knockback;

    private Rigidbody rb;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float cooldownTimer = 0f;
    private Vector3 dashDirection = Vector3.zero;

    // Public state access for other systems (AI, animations, etc.)
    public bool IsDashing => isDashing;
    public float CooldownRemaining => cooldownTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (knockback == null)
            knockback = GetComponent<KnockbackHandler>();
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (!isDashing)
            return;

        dashTimer -= Time.fixedDeltaTime;

        if (dashTimer <= 0f)
        {
            EndDash();
            return;
        }

        // Maintain strong horizontal dash velocity; keep current vertical component
        Vector3 currentVel = rb.linearVelocity;
        Vector3 horiz = dashDirection * dashSpeed;
        rb.linearVelocity = new Vector3(horiz.x, currentVel.y, horiz.z);
    }

    /// <summary>
    /// Tries to start a dash in the given direction.
    /// If the direction is near zero, it will fall back to velocity or forward direction.
    /// Returns true if a dash was started, false if blocked by cooldown, stagger, or already dashing.
    /// </summary>
    public bool TryStartDash(Vector3 desiredDirection)
    {
        if (isDashing)
            return false;

        if (cooldownTimer > 0f)
            return false;

        if (knockback != null && knockback.IsStaggered)
            return false;

        // Decide dash direction:

        // 1) If the caller supplied a usable direction, use that
        Vector3 dir = desiredDirection;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.0001f)
        {
            dir.Normalize();
        }
        else
        {
            // 2) Otherwise, if we have enough horizontal velocity, dash along that
            Vector3 currentVel = rb.linearVelocity;
            Vector3 horizontalVel = new Vector3(currentVel.x, 0f, currentVel.z);

            if (horizontalVel.magnitude >= minVelocityForDirection)
            {
                dir = horizontalVel.normalized;
            }
            else
            {
                // 3) Fallback: use the enemy's forward direction
                dir = transform.forward;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f)
                    dir = Vector3.forward;
                dir.Normalize();
            }
        }

        dashDirection = dir;
        isDashing = true;
        dashTimer = dashDuration;
        cooldownTimer = dashCooldown;

        return true;
    }

    private void EndDash()
    {
        isDashing = false;
    }
}
