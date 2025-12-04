using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    // --------------------------------------------------------------
    //  MOVEMENT SETTINGS
    // --------------------------------------------------------------
    [Header("Movement Settings")]
    public float maxSpeed = 14f;
    public float jumpForce = 7f;
    public Transform cameraTransform;

    [Header("Arcade Tuning")]
    public float groundAcceleration = 60f;
    public float groundDeceleration = 80f;
    public float reverseBoostMultiplier = 1.3f;

    [Header("Air Control")]
    public float airAcceleration = 25f;
    public float airDeceleration = 5f;

    [Header("Visual Rolling")]
    public Transform visualModel;
    public float visualRadius = 0.5f;

    private Rigidbody rb;
    private KnockbackHandler knockback;   // NEW
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
        knockback = GetComponent<KnockbackHandler>(); // NEW

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

    //External Movement Lock (used by dash, future abilities, etc.)
    private bool externalMovementLocked = false;
    public void SetExternalMovementLock(bool locked)
        { externalMovementLocked = locked; }    

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

    // --------------------------------------------------------------
    //  FIXED UPDATE — MAIN PHYSICS LOOP
    // --------------------------------------------------------------
    private void FixedUpdate()
    {
        // ------------------------------------------------------
        //  KNOCKBACK / STAGGER OVERRIDE  (NEW)
        // ------------------------------------------------------
        if ((knockback != null && knockback.IsStaggered) || externalMovementLocked)
            return;

        // ------------------------------------------------------
        //  EXISTING MOVEMENT LOGIC
        // ------------------------------------------------------
        timeSinceJump += Time.fixedDeltaTime;
        if (jumpCooldownTimer > 0f)
            jumpCooldownTimer -= Time.fixedDeltaTime;

        UpdateGroundState();

        if (cameraTransform == null)
        {
            HandleJump();
            return;
        }

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x);
        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        Vector3 currentVel = rb.linearVelocity;
        Vector3 horizontalVel = new Vector3(currentVel.x, 0f, currentVel.z);
        float currentSpeed = horizontalVel.magnitude;

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            Vector3 desiredDir = moveDirection.normalized;
            float targetSpeed = maxSpeed;

            Vector3 targetVel = desiredDir * targetSpeed;
            Vector3 deltaVel = targetVel - horizontalVel;

            float dot = 0f;
            if (currentSpeed > 0.01f)
                dot = Vector3.Dot(horizontalVel.normalized, desiredDir);

            bool reversing = dot < 0f;

            float accel = isGrounded ? groundAcceleration : airAcceleration;
            if (reversing && isGrounded)
                accel *= reverseBoostMultiplier;

            float maxDelta = accel * Time.fixedDeltaTime;

            if (deltaVel.magnitude > maxDelta)
                deltaVel = deltaVel.normalized * maxDelta;

            Vector3 velocityChange = new Vector3(deltaVel.x, 0f, deltaVel.z);
            rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }
        else
        {
            if (currentSpeed > 0.01f)
            {
                float decel = isGrounded ? groundDeceleration : airDeceleration;
                float maxDelta = decel * Time.fixedDeltaTime;

                float reduceBy = Mathf.Min(maxDelta, currentSpeed);
                Vector3 deltaVel = -horizontalVel.normalized * reduceBy;

                Vector3 velocityChange = new Vector3(deltaVel.x, 0f, deltaVel.z);
                rb.AddForce(velocityChange, ForceMode.VelocityChange);
            }
        }

        currentVel = rb.linearVelocity;
        horizontalVel = new Vector3(currentVel.x, 0f, currentVel.z);

        if (horizontalVel.magnitude > maxSpeed)
        {
            horizontalVel = horizontalVel.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(horizontalVel.x, currentVel.y, horizontalVel.z);
        }

        if (visualModel != null)
        {
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

        HandleJump();
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
