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

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool jumpRequested;

    [Header("Cursor Settings")]
    public bool lockCursorOnStart = true;
    private bool isCursorLocked = false;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.2f; // ray length below player
    public LayerMask groundLayer;            // set this to "Ground" in Inspector
    private bool isGrounded;                 // runtime state

    // NEW: prevents rapid double-jumps caused by brief post-takeoff grounding
    [Header("Jump Timing")]
    public float jumpCooldownSeconds = 1f;   // tweak 0.15–0.25
    private float jumpCooldownTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Recommended Rigidbody setup for rolling
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.None; // allow rotation
    }

    private void Start()
    {
        if (lockCursorOnStart)
            LockCursor(true);
    }

    // Helper with a unique name to avoid clashes with any property named "IsGrounded"
    private bool CheckGrounded()
    {
        // For a (1,1,1) sphere, collider radius ~0.5
        float sphereRadius = 0.45f;                   // slightly smaller than collider
        float castDistance = groundCheckDistance + 0.05f; // tight reach
        Vector3 origin = transform.position;          // from sphere center

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
        // --- GROUND CHECK ---
        isGrounded = CheckGrounded();

        // --- decrement timers ---
        if (jumpCooldownTimer > 0f)
            jumpCooldownTimer -= Time.fixedDeltaTime;

        // --- CAMERA-ALIGNED MOVEMENT ---
        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;

            if (moveDirection.sqrMagnitude > 0.01f && isGrounded)
            {
                Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                float alignment = 0f;
                if (horizontalVelocity.magnitude > 0.1f)
                    alignment = Vector3.Dot(horizontalVelocity.normalized, moveDirection);

                // Apply extra grip when turning sharply (alignment < 0.8)
                if (alignment < 0.8f)
                    rb.AddForce(-horizontalVelocity * 0.5f, ForceMode.Force); // counter-slide

                // Limit lateral slip
                Vector3 right = Vector3.Cross(Vector3.up, moveDirection);
                float lateralSpeed = Vector3.Dot(rb.linearVelocity, right);
                rb.AddForce(-right * lateralSpeed * 2f, ForceMode.Force);

                // Main movement force
                if (rb.linearVelocity.magnitude < maxSpeed)
                    rb.AddForce(moveDirection * moveForce, ForceMode.Force);

                // Rolling torque boost
                Vector3 torqueDir = new Vector3(moveDirection.z, 0f, -moveDirection.x);
                rb.AddTorque(torqueDir * moveForce * 0.5f, ForceMode.Force);
            }
        }

        // --- JUMP (gated by grounded + cooldown) ---
        if (jumpRequested && isGrounded && jumpCooldownTimer <= 0f)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            // lock out rapid re-jumps while ground check is still momentarily true
            jumpCooldownTimer = jumpCooldownSeconds;

            // optional: force airborne for this frame to be extra safe
            isGrounded = false;
        }

        jumpRequested = false; // always clear request
    }

    // Input System (PlayerInput Behavior = Send Messages)
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed)
            jumpRequested = true;
    }

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
        // Allow toggling the cursor lock with ESC key
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