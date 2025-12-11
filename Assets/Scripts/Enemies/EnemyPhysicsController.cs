using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyPhysicsController : MonoBehaviour
{
    // --------------------------------------------------------------
    //  MOVEMENT (Arcade)
    // --------------------------------------------------------------
    [Header("Movement")]
    public float maxSpeed = 14f;

    [Header("Arcade Movement")]
    public float groundAcceleration = 50f;
    public float groundDeceleration = 70f;
    public float reverseBoostMultiplier = 1.15f;

    // --------------------------------------------------------------
    //  PHYSICS
    // --------------------------------------------------------------
    [Header("Physics")]
    public float mass = 3f;
    public float radius = 0.5f;
    public float linearDamping = 0.35f;
    public float angularDamping = 0.1f;

    // --------------------------------------------------------------
    //  ENVIRONMENT & AVOIDANCE
    // --------------------------------------------------------------
    [Header("Environment")]
    public LayerMask groundMask;
    public LayerMask obstacleMask;

    [Header("Obstacle Avoidance")]
    public float obstacleCheckDistance = 5f;

    [Header("Edge Detection")]
    public float edgeCheckDistance = 3.5f;
    public float edgeBrakeForce = 8f;
    public float safeTurnForce = 25f;
    public float edgeRecoveryDelay = 1.5f;

    // --------------------------------------------------------------
    //  VISUAL ROLLING
    // --------------------------------------------------------------
    [Header("Visual Rolling")]
    public Transform visualModel;
    public float visualRadius = 0.5f;

    // --------------------------------------------------------------
    //  PRIVATE STATE
    // --------------------------------------------------------------
    private Rigidbody rb;
    private KnockbackHandler knockback;
    private Vector3 desiredDirection = Vector3.forward;
    private bool avoidingEdge = false;
    private bool _groundDetected = true;
    private Vector3 lastSafeDirection = Vector3.forward;
    private float edgeTimer = 0f;

    // --------------------------------------------------------------
    //  AI OVERRIDE SYSTEM
    // --------------------------------------------------------------
    private bool hasAIMove = false;
    private Vector3 aiMoveDirection = Vector3.zero;

    public void SetAIMMoveDirection(Vector3 dir)
    {
        aiMoveDirection = dir;
        hasAIMove = true;
    }

    public void ClearAIMMove()
    {
        hasAIMove = false;
        aiMoveDirection = Vector3.zero;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        knockback = GetComponent<KnockbackHandler>();

        rb.useGravity = true;
        rb.mass = mass;
        rb.linearDamping = linearDamping;
        rb.angularDamping = angularDamping;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void FixedUpdate()
    {
        bool canMove = knockback == null || !knockback.IsStaggered;

        if (canMove)
            ProcessAIMovement();

        UpdateVisualRoll();
    }

    // --------------------------------------------------------------
    //  AI MOVEMENT PROCESSING
    // --------------------------------------------------------------
    private void ProcessAIMovement()
    {
        // AIIntent overrides all directional logic
        if (hasAIMove)
        {
            Vector3 dir = aiMoveDirection;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
                desiredDirection = dir.normalized;
        }

        // --------------------------------------------------------------
        //  ARCADE FORCE MOVEMENT
        // --------------------------------------------------------------
        Vector3 currentVel = rb.linearVelocity;
        Vector3 horizVel = new Vector3(currentVel.x, 0f, currentVel.z);
        float currentSpeed = horizVel.magnitude;

        float targetSpeed = maxSpeed;

        if (avoidingEdge)
            targetSpeed *= 0.4f;

        Vector3 desiredVel = desiredDirection * targetSpeed;
        Vector3 deltaVel = desiredVel - horizVel;

        float dot = 1f;
        if (currentSpeed > 0.01f)
            dot = Vector3.Dot(horizVel.normalized, desiredDirection);

        bool reversing = dot < 0f;

        float accel = groundAcceleration;
        if (reversing)
            accel *= reverseBoostMultiplier;

        float maxDelta = accel * Time.fixedDeltaTime;

        if (deltaVel.magnitude > maxDelta)
            deltaVel = deltaVel.normalized * maxDelta;

        rb.AddForce(new Vector3(deltaVel.x, 0f, deltaVel.z), ForceMode.VelocityChange);

        // --------------------------------------------------------------
        //  SPEED LIMIT
        // --------------------------------------------------------------
        currentVel = rb.linearVelocity;
        horizVel = new Vector3(currentVel.x, 0f, currentVel.z);

        if (horizVel.magnitude > maxSpeed)
        {
            horizVel = horizVel.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(horizVel.x, currentVel.y, horizVel.z);
        }

        // --------------------------------------------------------------
        //  EDGE DETECTION
        // --------------------------------------------------------------
        ProcessEdgeDetection();

        // --------------------------------------------------------------
        //  VERTICAL CLEANUP
        // --------------------------------------------------------------
        Vector3 v = rb.linearVelocity;
        if (v.y > 0.05f)
            v.y = Mathf.Lerp(v.y, 0f, 0.5f);
        rb.linearVelocity = v;
    }

    // --------------------------------------------------------------
    //  EDGE DETECTION SYSTEM
    // --------------------------------------------------------------
    private void ProcessEdgeDetection()
    {
        float speed = rb.linearVelocity.magnitude;
        float speedRatio = Mathf.Clamp01(speed / maxSpeed);

        float lookDistance = Mathf.Lerp(edgeCheckDistance, edgeCheckDistance * 3f, speedRatio);
        Vector3 headOffset = Vector3.up * (radius + 1f);
        Vector3 origin = transform.position + headOffset;

        Vector3 moveDir = rb.linearVelocity.sqrMagnitude > 0.01f
            ? rb.linearVelocity.normalized
            : transform.forward;

        float tilt = -0.25f;
        Vector3 forwardDown = (moveDir + Vector3.up * tilt).normalized;
        Vector3 leftDown = (Quaternion.Euler(0, -25f, 0) * moveDir + Vector3.up * tilt).normalized;
        Vector3 rightDown = (Quaternion.Euler(0, 25f, 0) * moveDir + Vector3.up * tilt).normalized;

        bool centerHit = Physics.Raycast(origin, forwardDown, lookDistance, groundMask);
        bool leftHit = Physics.Raycast(origin, leftDown, lookDistance, groundMask);
        bool rightHit = Physics.Raycast(origin, rightDown, lookDistance, groundMask);

        _groundDetected = centerHit || leftHit || rightHit;

        if (!_groundDetected)
        {
            if (!avoidingEdge)
            {
                avoidingEdge = true;
                edgeTimer = edgeRecoveryDelay;
            }

            float brake = Mathf.Lerp(edgeBrakeForce, edgeBrakeForce * 2f, speedRatio);
            rb.AddForce(-rb.linearVelocity * brake * Time.fixedDeltaTime, ForceMode.Acceleration);

            Vector3 avoidDir = Vector3.Lerp(-moveDir, lastSafeDirection, 0.6f);
            desiredDirection = Vector3.Lerp(desiredDirection, avoidDir, 0.8f);
        }
        else
        {
            if (avoidingEdge)
            {
                edgeTimer -= Time.fixedDeltaTime;
                if (edgeTimer <= 0f)
                    avoidingEdge = false;
            }

            lastSafeDirection = moveDir;
        }
    }

    // --------------------------------------------------------------
    //  VISUAL ROLLING
    // --------------------------------------------------------------
    private void UpdateVisualRoll()
    {
        if (visualModel == null)
            return;

        Vector3 rollVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float rollSpeed = rollVel.magnitude;

        if (rollSpeed > 0.05f && visualRadius > 0.001f)
        {
            Vector3 rollAxis = Vector3.Cross(Vector3.up, rollVel.normalized);
            float angularRate = rollSpeed / visualRadius;
            float rotationAmount = angularRate * Mathf.Rad2Deg * Time.fixedDeltaTime;
            visualModel.Rotate(rollAxis, rotationAmount, Space.World);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = _groundDetected ? Color.green : Color.red;
        Gizmos.DrawSphere(transform.position + Vector3.up * (radius + 1.0f), 0.1f);
    }
}
