using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    // --------------------------------------------------------------
    //  MOVEMENT SETTINGS
    // --------------------------------------------------------------
    [Header("Movement Settings")]
    public float moveForce = 40f;
    public float maxSpeed = 14f;
    public float jumpForce = 7f;
    public Transform cameraTransform;

    [Header("Arcade Controls")]
    public float lateralDampingForce = 1.2f;   // Controls sliding/drift
    public float counterForce = 0.3f;          // Gentle direction change assistance

    [Header("Visual Rolling")]
    public Transform visualModel;
    public float rollSpeed = 50f;

    [Header("Air Control")]
    public float airControlMultiplier = 0.5f;

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool jumpRequested;

    // --------------------------------------------------------------
    //  CURSOR
    // --------------------------------------------------------------
    [Header("Cursor Settings")]
    public bool lockCursorOnStart = true;
    private bool isCursorLocked = false;

    // --------------------------------------------------------------
    //  GROUND CHECK
    // --------------------------------------------------------------
    [Header("Ground Check")]
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer;

    private bool isGrounded;
    private int groundedFrames = 0;
    private int ungroundedFrames = 0;

    // --------------------------------------------------------------
    //  JUMP TIMING
    // --------------------------------------------------------------
    [Header("Jump Timing")]
    public float jumpCooldownSeconds = 1f;
    private float jumpCooldownTimer = 0f;
    private float timeSinceJump = Mathf.Infinity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.None;
    }

    private void Start()
    {
        if (lockCursorOnStart)
            LockCursor(true);
    }

    private void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
        isCursorLocked = locked;
    }

    // --------------------------------------------------------------
    //  GROUND CHECK WITH SMOOTHING
    // --------------------------------------------------------------
    private bool CheckRawGrounded()
    {
        // Ignore grounding right after a jump
        if (timeSinceJump < 0.15f)
            return false;

        // Ignore if moving up too fast
        if (rb.linearVelocity.y > 0.5f)
            return false;

        float radius = 0.45f;
        float distance = groundCheckDistance + 0.05f;

        bool hit = Physics.SphereCast(
            transform.position,
            radius,
            Vector3.down,
            out _,
            distance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        return hit;
    }

    private void UpdateGroundState()
    {
        bool rawGrounded = CheckRawGrounded();

        // Smooth grounding — prevents micro-bounce issues
        if (rawGrounded)
        {
            groundedFrames++;
            ungroundedFrames = 0;
        }
        else
        {
            groundedFrames = 0;
            ungroundedFrames++;
        }

        // Require 2 consecutive grounded frames
        isGrounded = groundedFrames >= 2;
    }

    // --------------------------------------------------------------
    //  FIXED UPDATE — MAIN PHYSICS LOOP
    // --------------------------------------------------------------
    private void FixedUpdate()
    {
        timeSinceJump += Time.fixedDeltaTime;

        // Update ground state
        UpdateGroundState();

        if (jumpCooldownTimer > 0f)
            jumpCooldownTimer -= Time.fixedDeltaTime;

        // --------------------------------------------------
        // MOVEMENT
        // --------------------------------------------------
        if (cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;

            if (moveDirection.sqrMagnitude > 0.01f)
            {
                Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

                if (isGrounded)
                {
                    float alignment = horizontalVel.magnitude > 0.1f
                        ? Vector3.Dot(horizontalVel.normalized, moveDirection)
                        : 0f;

                    // 🔥 SMOOTH ARCADE MOVEMENT: Natural turning without robotic snap
                    // Only apply gentle counter-force when changing direction
                    if (alignment < 0.7f && horizontalVel.magnitude > 0.5f)
                    {
                        rb.AddForce(-horizontalVel * counterForce, ForceMode.Force);
                    }

                    // Lateral damping for controlled sliding
                    Vector3 rightDir = Vector3.Cross(Vector3.up, moveDirection);
                    float lateralSpeed = Vector3.Dot(rb.linearVelocity, rightDir);
                    rb.AddForce(-rightDir * lateralSpeed * lateralDampingForce, ForceMode.Force);

                    // Main movement push (handles all acceleration naturally)
                    if (rb.linearVelocity.magnitude < maxSpeed)
                        rb.AddForce(moveDirection * moveForce, ForceMode.Force);

                    // Rolling torque
                    Vector3 torqueDir = new Vector3(moveDirection.z, 0, -moveDirection.x);
                    rb.AddTorque(torqueDir * moveForce * 0.5f, ForceMode.Force);
                }
                else
                {
                    // Air control
                    if (horizontalVel.magnitude < maxSpeed)
                        rb.AddForce(moveDirection * moveForce * airControlMultiplier, ForceMode.Force);
                }

                // --------------------------------------------------
                // VISUAL ROLLING BASED ON REAL PHYSICS SPEED
                // --------------------------------------------------
                if (visualModel != null && horizontalVel.magnitude > 0.1f)
                {
                    float radius = 0.5f; // adjust if needed
                    Vector3 rollAxis = Vector3.Cross(Vector3.up, horizontalVel.normalized);

                    float angularRate = horizontalVel.magnitude / radius;
                    float rotationAmount = angularRate * Mathf.Rad2Deg * Time.fixedDeltaTime;

                    visualModel.Rotate(rollAxis, rotationAmount, Space.World);
                }
            }
        }

        // --------------------------------------------------
        // JUMP
        // --------------------------------------------------
        if (jumpRequested && isGrounded && jumpCooldownTimer <= 0f)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpCooldownTimer = jumpCooldownSeconds;
            timeSinceJump = 0f;
            groundedFrames = 0; // prevent double jump
            isGrounded = false;
        }

        jumpRequested = false;
    }

    // --------------------------------------------------------------
    //  INPUT SYSTEM
    // --------------------------------------------------------------
    public void OnMove(InputAction.CallbackContext ctx) =>
        moveInput = ctx.ReadValue<Vector2>();

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            jumpRequested = true;
    }

#if !UNITY_EDITOR
    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            LockCursor(!isCursorLocked);
    }
#endif
}