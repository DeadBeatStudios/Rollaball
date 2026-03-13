using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerPreviewController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float jumpForce = 7f;

    [Header("Ground Movement")]
    public float groundAcceleration = 60f;
    public float groundDeceleration = 80f;

    [Header("Air Movement")]
    public float airAcceleration = 25f;
    public float airDeceleration = 5f;

    [Header("Visual Rolling")]
    public Transform visualModel;
    public float visualRadius = 0.5f;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayer;

    [Header("Jump Timing")]
    public float jumpCooldownSeconds = 1f;

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool jumpRequested;

    private bool isGrounded;
    private int groundedFrames = 0;
    private int ungroundedFrames = 0;
    private float jumpCooldownTimer = 0f;
    private float timeSinceJump = Mathf.Infinity;

    public Vector2 MoveInput => moveInput;
    public bool IsGrounded => isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.None;
    }

    private bool CheckRawGrounded()
    {
        if (timeSinceJump < 0.15f)
            return false;

        if (rb.linearVelocity.y > 0.5f)
            return false;

        float radius = 0.45f;
        float distance = groundCheckDistance + 0.05f;

        return Physics.SphereCast(
            transform.position,
            radius,
            Vector3.down,
            out _,
            distance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );
    }

    private void UpdateGroundState()
    {
        bool rawGrounded = CheckRawGrounded();

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

        isGrounded = groundedFrames >= 2;
    }

    private void FixedUpdate()
    {
        timeSinceJump += Time.fixedDeltaTime;

        if (jumpCooldownTimer > 0f)
            jumpCooldownTimer -= Time.fixedDeltaTime;

        UpdateGroundState();
        ProcessMovement();
        HandleJump();
        UpdateVisualRoll();
    }

    private void ProcessMovement()
    {
        // World-space movement for static preview scene
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        Vector3 currentVel = rb.linearVelocity;
        Vector3 horizontalVel = new Vector3(currentVel.x, 0f, currentVel.z);
        float currentSpeed = horizontalVel.magnitude;

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            Vector3 desiredDir = moveDirection.normalized;
            Vector3 targetVel = desiredDir * moveSpeed;
            Vector3 deltaVel = targetVel - horizontalVel;

            float accel = isGrounded ? groundAcceleration : airAcceleration;
            float maxDelta = accel * Time.fixedDeltaTime;

            if (deltaVel.magnitude > maxDelta)
                deltaVel = deltaVel.normalized * maxDelta;

            Vector3 velocityChange = new Vector3(deltaVel.x, 0f, deltaVel.z);
            rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }
        else if (currentSpeed > 0.01f)
        {
            float decel = isGrounded ? groundDeceleration : airDeceleration;
            float maxDelta = decel * Time.fixedDeltaTime;

            float reduceBy = Mathf.Min(maxDelta, currentSpeed);
            Vector3 deltaVel = -horizontalVel.normalized * reduceBy;

            Vector3 velocityChange = new Vector3(deltaVel.x, 0f, deltaVel.z);
            rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }

        // Clamp horizontal speed
        currentVel = rb.linearVelocity;
        horizontalVel = new Vector3(currentVel.x, 0f, currentVel.z);

        if (horizontalVel.magnitude > moveSpeed)
        {
            horizontalVel = horizontalVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(horizontalVel.x, currentVel.y, horizontalVel.z);
        }
    }

    private void HandleJump()
    {
        if (jumpRequested && isGrounded && jumpCooldownTimer <= 0f)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpCooldownTimer = jumpCooldownSeconds;
            timeSinceJump = 0f;
            groundedFrames = 0;
            isGrounded = false;
        }

        jumpRequested = false;
    }

    private void UpdateVisualRoll()
    {
        if (visualModel == null)
            return;

        Vector3 horizVelForRoll = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float speed = horizVelForRoll.magnitude;

        if (speed > 0.05f && visualRadius > 0.001f)
        {
            Vector3 rollAxis = Vector3.Cross(Vector3.up, horizVelForRoll.normalized);
            float angularRate = speed / visualRadius;
            float rotationAmount = angularRate * Mathf.Rad2Deg * Time.fixedDeltaTime;
            visualModel.Rotate(rollAxis, rotationAmount, Space.World);
        }
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            jumpRequested = true;
    }
}