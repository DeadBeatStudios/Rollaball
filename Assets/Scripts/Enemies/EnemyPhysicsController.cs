using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyPhysicsController : MonoBehaviour
{
    // --------------------------------------------------------------
    //  TARGETING
    // --------------------------------------------------------------
    [Header("Targeting")]
    public Transform target;
    public FlagPickup flag;

    [Header("Flag Scoring")]
    public Transform goalTarget;

    // --------------------------------------------------------------
    //  MOVEMENT (Arcade)
    // --------------------------------------------------------------
    [Header("Movement")]
    public float maxSpeed = 14f;

    [Header("Arcade Movement")]
    [Tooltip("How fast the enemy accelerates toward its desired direction (m/s²).")]
    public float groundAcceleration = 50f;

    [Tooltip("How fast the enemy slows down when direction changes occur (m/s²).")]
    public float groundDeceleration = 70f;

    [Tooltip("Extra snap when reversing direction (dot < 0).")]
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
    public float obstacleAvoidStrength = 35f;

    [Header("Edge Detection")]
    public float edgeCheckDistance = 3.5f;
    public float edgeBrakeForce = 8f;
    public float safeTurnForce = 25f;
    public float edgeRecoveryDelay = 1.5f;

    [Header("AI Tuning")]
    public float predictionTime = 0.35f;
    public float closeBrakeDistance = 2.5f;

    // --------------------------------------------------------------
    //  VISUAL ROLLING (MATCH PLAYER)
    // --------------------------------------------------------------
    [Header("Visual Rolling")]
    public Transform visualModel;
    public float visualRadius = 0.5f;

    // --------------------------------------------------------------
    //  PRIVATE STATE
    // --------------------------------------------------------------
    private Rigidbody rb;
    private Vector3 desiredDirection = Vector3.forward;
    private bool avoidingEdge = false;
    private bool _groundDetected = true;
    private Vector3 lastSafeDirection = Vector3.forward;
    private float edgeTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = true;
        rb.mass = mass;
        rb.linearDamping = linearDamping;
        rb.angularDamping = angularDamping;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // AUTO-FIND FLAG
        if (flag == null)
        {
            flag = FindAnyObjectByType<FlagPickup>();
            if (flag != null)
                Debug.Log($"{name}: Found FlagPickup automatically");
        }

        // AUTO-FIND GOAL TARGET
        if (goalTarget == null)
        {
            var goal = FindAnyObjectByType<GoalTrigger>();
            if (goal != null)
            {
                goalTarget = goal.transform;
                Debug.Log($"{name}: Auto-assigned GoalTarget → {goalTarget.name}");
            }
            else
            {
                Debug.LogWarning($"{name}: No GoalTrigger found in scene!");
            }
        }
    }

    private void FixedUpdate()
    {
        // --------------------------------------------------------------
        // FLAG → TARGETING LOGIC
        // --------------------------------------------------------------
        if (flag != null)
        {
            if (!flag.IsHeld)
            {
                target = flag.transform; // Flag on ground
            }
            else
            {
                Transform holder = flag.CurrentHolder;

                if (holder != null && holder != transform)
                {
                    target = holder; // Chase whoever holds it
                }
                else
                {
                    target = goalTarget; // We have flag → score!
                }
            }
        }

        if (!target)
            return;

        // --------------------------------------------------------------
        // PREDICTIVE TARGET POSITION
        // --------------------------------------------------------------
        Vector3 targetVelocityEstimate = Vector3.zero;
        if (target.TryGetComponent<Rigidbody>(out var targetRb))
            targetVelocityEstimate = targetRb.linearVelocity;

        Vector3 predictedPos = target.position + targetVelocityEstimate * predictionTime;
        Vector3 toTarget = predictedPos - transform.position;
        float distance = toTarget.magnitude;
        Vector3 seekDir = toTarget.normalized;

        // --------------------------------------------------------------
        // EDGE DETECTION
        // --------------------------------------------------------------
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

        bool centerHit = Physics.Raycast(origin, forwardDown, out _, lookDistance, groundMask);
        bool leftHit = Physics.Raycast(origin, leftDown, out _, lookDistance, groundMask);
        bool rightHit = Physics.Raycast(origin, rightDown, out _, lookDistance, groundMask);

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

        // --------------------------------------------------------------
        // OBSTACLE AVOIDANCE
        // --------------------------------------------------------------
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, obstacleCheckDistance, obstacleMask))
        {
            Vector3 avoidDir = Vector3.Reflect(seekDir, hit.normal);
            seekDir = Vector3.Lerp(seekDir, avoidDir, 0.9f);
        }

        // --------------------------------------------------------------
        // DIRECTION STEERING
        // --------------------------------------------------------------
        float turnRate = avoidingEdge ? safeTurnForce * 0.4f : safeTurnForce;

        desiredDirection = Vector3.Lerp(desiredDirection, seekDir, Time.fixedDeltaTime * turnRate);
        desiredDirection.y = 0f;
        if (desiredDirection.sqrMagnitude < 0.001f)
            desiredDirection = transform.forward;
        desiredDirection.Normalize();

        // --------------------------------------------------------------
        // ARCADE MOVEMENT (MATCH PLAYER)
        // --------------------------------------------------------------
        Vector3 currentVel = rb.linearVelocity;
        Vector3 horizVel = new Vector3(currentVel.x, 0f, currentVel.z);
        float currentSpeed = horizVel.magnitude;

        float targetSpeed = maxSpeed;

        if (avoidingEdge)
            targetSpeed *= 0.4f;

        // Brake when close to target
        if (distance < closeBrakeDistance)
        {
            float slowFactor = Mathf.Clamp01(distance / closeBrakeDistance);
            targetSpeed *= slowFactor;
        }

        Vector3 desiredVel = desiredDirection * targetSpeed;
        Vector3 deltaVel = desiredVel - horizVel;

        float dot = 1f;
        if (currentSpeed > 0.01f && desiredDirection.sqrMagnitude > 0.0001f)
            dot = Vector3.Dot(horizVel.normalized, desiredDirection);

        bool reversing = dot < 0f;

        float accel = groundAcceleration;
        if (reversing)
            accel *= reverseBoostMultiplier;

        if (targetSpeed < 0.1f)
            accel = groundDeceleration;

        float maxDelta = accel * Time.fixedDeltaTime;

        if (deltaVel.magnitude > maxDelta)
            deltaVel = deltaVel.normalized * maxDelta;

        rb.AddForce(new Vector3(deltaVel.x, 0f, deltaVel.z), ForceMode.VelocityChange);

        // Clamp horizontal speed
        currentVel = rb.linearVelocity;
        horizVel = new Vector3(currentVel.x, 0f, currentVel.z);
        if (horizVel.magnitude > maxSpeed)
        {
            horizVel = horizVel.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(horizVel.x, currentVel.y, horizVel.z);
        }

        // --------------------------------------------------------------
        // ROLLING VISUAL (MATCH PLAYER)
        // --------------------------------------------------------------
        if (visualModel != null)
        {
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

        // --------------------------------------------------------------
        // VERTICAL CLEANUP
        // --------------------------------------------------------------
        Vector3 v = rb.linearVelocity;
        if (v.y > 0.05f)
            v.y = Mathf.Lerp(v.y, 0f, 0.5f);
        rb.linearVelocity = v;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = _groundDetected ? Color.green : Color.red;
        Gizmos.DrawSphere(transform.position + Vector3.up * (radius + 1.0f), 0.1f);
    }
}
