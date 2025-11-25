using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveForce = 20f;
    public float maxSpeed = 10f;
    public float jumpForce = 5f;
    public Transform cameraTransform;

    [Header("Visual Rolling")]
    public Transform visualModel;
    public float rollSpeed = 50f;

    [Header("Air Control")]
    public float airControlMultiplier = 0.5f;

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
    private float timeSinceJump = Mathf.Infinity;
    private int consecutiveGroundedFrames = 0;
    public int requiredGroundedFrames = 2;

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

    private bool CheckGrounded()
    {
        if (timeSinceJump < 0.15f)
            return false;

        if (rb.linearVelocity.y > 0.5f)
            return false;

        float sphereRadius = 0.45f;
        float castDistance = groundCheckDistance + 0.05f;

        bool hit = Physics.SphereCast(
            transform.position,
            sphereRadius,
            Vector3.down,
            out _,
            castDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        return hit;
    }

    private void FixedUpdate()
    {
        timeSinceJump += Time.fixedDeltaTime;
        isGrounded = CheckGrounded();

        if (jumpCooldownTimer > 0f)
            jumpCooldownTimer -= Time.fixedDeltaTime;

        // Movement
        if (cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            forward.y = 0;
            right.y = 0;

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

                    if (alignment < 0.8f)
                        rb.AddForce(-horizontalVel * 0.5f, ForceMode.Force);

                    Vector3 rightDir = Vector3.Cross(Vector3.up, moveDirection);
                    float lateralSpeed = Vector3.Dot(rb.linearVelocity, rightDir);
                    rb.AddForce(-rightDir * lateralSpeed * 2f, ForceMode.Force);

                    if (rb.linearVelocity.magnitude < maxSpeed)
                        rb.AddForce(moveDirection * moveForce, ForceMode.Force);

                    Vector3 torqueDir = new Vector3(moveDirection.z, 0, -moveDirection.x);
                    rb.AddTorque(torqueDir * moveForce * 0.5f, ForceMode.Force);
                }
                else
                {
                    if (horizontalVel.magnitude < maxSpeed)
                        rb.AddForce(moveDirection * moveForce * airControlMultiplier, ForceMode.Force);
                }

                // --- REALISTIC VISUAL ROLLING ---
                if (visualModel != null && horizontalVel.magnitude > 0.1f)
                {
                    float radius = 0.5f; // Adjust if your ball is larger
                    Vector3 rollAxis = Vector3.Cross(Vector3.up, horizontalVel.normalized);

                    // Angular velocity from linear velocity
                    float angularRate = horizontalVel.magnitude / radius;

                    // Convert rad/sec → degrees
                    float rotationAmount = angularRate * Mathf.Rad2Deg * Time.fixedDeltaTime;

                    visualModel.Rotate(rollAxis, rotationAmount, Space.World);
                }
            }
        }

        // Jump
        if (jumpRequested && isGrounded && jumpCooldownTimer <= 0f)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpCooldownTimer = jumpCooldownSeconds;
            timeSinceJump = 0f;
            isGrounded = false;
            consecutiveGroundedFrames = 0;
        }

        jumpRequested = false;
    }

    // INPUT SYSTEM CALLBACKS
    public void OnMove(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            jumpRequested = true;
    }

#if !UNITY_EDITOR
    // Optional ESC handling for builds
    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            LockCursor(!isCursorLocked);
    }
#endif
}
