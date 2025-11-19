using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveForce = 20f;
    public float maxSpeed = 10f;
    public float jumpForce = 5f;
    public Transform cameraTransform; // assign Main Camera in Inspector

    [Header("Visual Rolling")]
    public Transform visualModel; // 💡 NEW: Assign the VisualModel child here
    public float rollSpeed = 50f; // 💡 NEW: Controls visual rolling speed

    [Header("Air Control")]
    public float airControlMultiplier = 0.5f; // 💡 NEW: How much control in air

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool jumpRequested;

    [Header("Cursor Settings")]
    public bool lockCursorOnStart = true;
    private bool isCursorLocked = false;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Jump Timing")]
    public float jumpCooldownSeconds = 1f;
    private float jumpCooldownTimer = 0f;
    private float timeSinceJump = Mathf.Infinity; // 🔥 NEW: Tracks time since last jump
    private int consecutiveGroundedFrames = 0; // 🔥 NEW: Must be grounded for multiple frames
    public int requiredGroundedFrames = 2; // 🔥 NEW: How many frames before allowing jump

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

    private bool CheckGrounded()
    {
        // 🔥 FIX: Don't consider grounded immediately after jumping
        if (timeSinceJump < 0.15f)
        {
            return false;
        }

        // 🔥 FIX: Don't check ground while moving upward (prevents double jump)
        if (rb.linearVelocity.y > 0.5f)
        {
            return false;
        }

        float sphereRadius = 0.45f;
        float castDistance = groundCheckDistance + 0.05f;
        Vector3 origin = transform.position;

        bool hit = Physics.SphereCast(
            origin,
            sphereRadius,
            Vector3.down,
            out RaycastHit hitInfo,
            castDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        Debug.DrawRay(origin, Vector3.down * castDistance, hit ? Color.green : Color.red);
        return hit;
    }

    private void FixedUpdate()
    {
        // 🔥 Update jump timer
        timeSinceJump += Time.fixedDeltaTime;

        // 🔥 Ground check - ONLY ONCE per FixedUpdate
        isGrounded = CheckGrounded();

        // Cooldowns
        if (jumpCooldownTimer > 0f)
            jumpCooldownTimer -= Time.fixedDeltaTime;

        // Movement (Camera aligned)
        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;

            if (moveDirection.sqrMagnitude > 0.01f)
            {
                Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

                if (isGrounded)
                {
                    // GROUND MOVEMENT (Original code)
                    float alignment = 0f;
                    if (horizontalVelocity.magnitude > 0.1f)
                        alignment = Vector3.Dot(horizontalVelocity.normalized, moveDirection);

                    if (alignment < 0.8f)
                        rb.AddForce(-horizontalVelocity * 0.5f, ForceMode.Force);

                    Vector3 right = Vector3.Cross(Vector3.up, moveDirection);
                    float lateralSpeed = Vector3.Dot(rb.linearVelocity, right);
                    rb.AddForce(-right * lateralSpeed * 2f, ForceMode.Force);

                    if (rb.linearVelocity.magnitude < maxSpeed)
                        rb.AddForce(moveDirection * moveForce, ForceMode.Force);

                    Vector3 torqueDir = new Vector3(moveDirection.z, 0f, -moveDirection.x);
                    rb.AddTorque(torqueDir * moveForce * 0.5f, ForceMode.Force);
                }
                else
                {
                    // 💡 NEW: AIR CONTROL - Simple steering while airborne
                    if (horizontalVelocity.magnitude < maxSpeed)
                    {
                        rb.AddForce(moveDirection * moveForce * airControlMultiplier, ForceMode.Force);
                    }
                }

                // 💡 NEW: VISUAL ROLLING - Rotate visual model based on velocity
                if (visualModel != null && horizontalVelocity.magnitude > 0.1f)
                {
                    Vector3 rollAxis = Vector3.Cross(Vector3.up, horizontalVelocity.normalized);
                    float rollAmount = horizontalVelocity.magnitude * rollSpeed * Time.fixedDeltaTime;
                    visualModel.Rotate(rollAxis, rollAmount, Space.World);
                }
            }
        }

        // Jump
        if (jumpRequested && isGrounded && jumpCooldownTimer <= 0f)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpCooldownTimer = jumpCooldownSeconds;
            timeSinceJump = 0f; // 🔥 CRITICAL: Reset jump timer
            isGrounded = false;
            consecutiveGroundedFrames = 0; // 🔥 NEW: Reset grounded frames
        }

        jumpRequested = false;
    }

    // ================================
    // NEW INPUT METHODS (Unity Events)
    // ================================
    public void OnMove(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    public void OnJump(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            jumpRequested = true;
    }
    // ================================

    private void LockCursor(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            isCursorLocked = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            isCursorLocked = false;
        }
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            LockCursor(!isCursorLocked);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Hit: " + collision.gameObject.name);
    }
}