using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(Rigidbody))]
public class PlayerDash : MonoBehaviour
{
    [Header("Dash Settings")]
    [Tooltip("Horizontal Dash Speed.")]
    public float dashSpeed = 24f;

    [Tooltip("How long the dash lasts(seconds).")]
    public float dashDuration = 0.18f;

    [Tooltip("Cooldown between dashes(seconds).")]
    public float dashCooldown = 0.6f;

    [Tooltip("Minimum horizontal speed before we consider using current velocity as dash direction")]
    public float minVelocityForDirection = 0.2f;

    [Header("References")]
    [Tooltip("Optional: PlayerController reference. If null, will auto-find on this GameObject.")]
    public PlayerController playerController;

    private Rigidbody rb;
    private KnockbackHandler knockback;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float cooldownTimer = 0f;
    private Vector3 dashDirection = Vector3.zero;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        knockback = GetComponent<KnockbackHandler>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if(cooldownTimer > 0f)
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
            //Maintain a strong horizontal dash velocity; keep current vertival component
            Vector3 currentVel = rb.linearVelocity;
            Vector3 horiz = dashDirection * dashSpeed;
        rb.linearVelocity = new Vector3(horiz.x, currentVel.y, horiz.z);
       
    }

    // INPUT (hool this to an Input Action bound to Left Click)
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

        //Decide dash direction:
        // 1) If we're already moving, dash along current horizontal velocity
        Vector3 currentVel = rb.linearVelocity;
        Vector3 horizontalVel = new Vector3(currentVel.x, 0f, currentVel.z);

        if (horizontalVel.magnitude >= minVelocityForDirection)
        {
            dashDirection = horizontalVel.normalized;
        }
        else
        {
            Vector3 forward = Vector3.forward;

            if (playerController != null && playerController.cameraTransform != null)
            {
                forward = playerController.cameraTransform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.0001f)
                    forward = Vector3.forward;
                forward.Normalize();
            }
            else
            {
                forward = transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.0001f)
                    forward = Vector3.forward;
                forward.Normalize();
            }

            dashDirection = forward;
        }

        isDashing = true;
        dashTimer = dashDuration;
        cooldownTimer = dashCooldown;

        //Lock normal movement while dashing
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
