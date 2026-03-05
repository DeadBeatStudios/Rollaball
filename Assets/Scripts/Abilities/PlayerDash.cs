using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerDash : MonoBehaviour
{
    [Header("Dash Settings")]
    [Tooltip("Horizontal Dash Speed.")]
    public float dashSpeed = 24f;

    [Tooltip("How long the dash lasts (seconds).")]
    public float dashDuration = 0.18f;

    [Tooltip("Cooldown between dashes (seconds).")]
    public float dashCooldown = 0.6f;

    [Header("Direction Settings")]
    [Tooltip("If move input magnitude is below this, we treat it as 'no input' and dash toward camera forward.")]
    public float inputDeadzone = 0.15f;

    [Header("References")]
    [Tooltip("Optional: PlayerController reference. If null, will auto-find on this GameObject.")]
    public PlayerController playerController;

    private Rigidbody rb;
    private KnockbackHandler knockback;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float cooldownTimer = 0f;
    private Vector3 dashDirection = Vector3.zero;

    public bool IsDashing => isDashing;
    public float CooldownRemaining => cooldownTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        knockback = GetComponent<KnockbackHandler>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();
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

    // Hook to Input Action
    public void OnDash(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        TryStartDash();
    }

    private void TryStartDash()
    {
        if (isDashing)
            return;

        if (cooldownTimer > 0f)
            return;

        if (knockback != null && knockback.IsStaggered)
            return;

        // ---------------------------------------------------------
        // DASH DIRECTION:
        // Priority:
        // 1) Camera-relative MOVE INPUT (if any)
        // 2) Camera forward (if no input)
        // 3) Transform forward (fallback)
        // ---------------------------------------------------------

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        if (playerController != null && playerController.cameraTransform != null)
        {
            forward = playerController.cameraTransform.forward;
            right = playerController.cameraTransform.right;
        }

        // Flatten to horizontal plane
        forward.y = 0f;
        right.y = 0f;

        // Robust fallbacks
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = transform.forward;
            forward.y = 0f;
        }

        if (right.sqrMagnitude < 0.0001f)
        {
            // Ensure right is perpendicular to forward
            right = Vector3.Cross(Vector3.up, forward);
        }

        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        if (right.sqrMagnitude < 0.0001f) right = Vector3.right;

        forward.Normalize();
        right.Normalize();

        // Read move input from controller (clean)
        Vector2 moveInput = playerController != null ? playerController.MoveInput : Vector2.zero;

        Vector3 inputDir = (right * moveInput.x) + (forward * moveInput.y);

        if (moveInput.magnitude >= inputDeadzone && inputDir.sqrMagnitude > 0.0001f)
            dashDirection = inputDir.normalized;
        else
            dashDirection = forward; // No input: dash where camera faces

        isDashing = true;
        dashTimer = dashDuration;
        cooldownTimer = dashCooldown;

        // Lock normal movement while dashing
        if (playerController != null)
            playerController.SetExternalMovementLock(true);
    }

    private void EndDash()
    {
        isDashing = false;

        if (playerController != null)
            playerController.SetExternalMovementLock(false);
    }
}
 