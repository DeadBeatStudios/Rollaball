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
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Jump Timing")]
    public float jumpCooldownSeconds = 1f;
    private float jumpCooldownTimer = 0f;

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
        // Ground check
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

            if (moveDirection.sqrMagnitude > 0.01f && isGrounded)
            {
                Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
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
        }

        // Jump
        if (jumpRequested && isGrounded && jumpCooldownTimer <= 0f)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpCooldownTimer = jumpCooldownSeconds;
            isGrounded = false;
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
