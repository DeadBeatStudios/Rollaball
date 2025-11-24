using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyPhysicsController : MonoBehaviour
{
    [Header("Targeting")]
    public Transform target;
    public FlagPickup flag;

    [Header("Flag Scoring")]
    public Transform goalTarget;

    [Header("Movement")]
    public float moveForce = 45f;
    public float maxSpeed = 14f;
    public float turnSharpness = 10f;
    public float steerResponsiveness = 8f;

    [Header("Physics")]
    public float mass = 3f;
    public float radius = 0.5f;
    public float linearDamping = 0.35f;
    public float angularDamping = 0.1f;

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

    // --- Private State ---
    private Rigidbody rb;
    private Vector3 desiredDirection;
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
        // ---------------------------------------
        // FLAG → TARGETING LOGIC
        // ---------------------------------------
        if (flag != null)
        {
            if (!flag.IsHeld)
            {
                // Flag on ground → chase it
                target = flag.transform;
            }
            else
            {
                Transform holder = flag.CurrentHolder;

                if (holder != null && holder != transform)
                {
                    // Someone else has the flag → chase them
                    target = holder;
                }
                else
                {
                    // WE HAVE THE FLAG → score!
                    target = goalTarget;
                }
            }
        }

        if (!target) return;

        // ---------------------------------------
        // Predictive movement toward target
        // ---------------------------------------
        Vector3 targetVel = Vector3.zero;
        if (target.TryGetComponent<Rigidbody>(out var targetRb))
            targetVel = targetRb.linearVelocity;

        Vector3 predictedPos = target.position + targetVel * predictionTime;
        Vector3 toTarget = predictedPos - transform.position;
        float distance = toTarget.magnitude;
        Vector3 seekDir = toTarget.normalized;

        // ---------------------------------------
        // EDGE DETECTION
        // ---------------------------------------
        float speed = rb.linearVelocity.magnitude;
        if (speed < 0.1f) speed = 0.1f;

        float speedRatio = Mathf.Clamp01(speed / maxSpeed);
        float lookDistance = Mathf.Lerp(edgeCheckDistance, edgeCheckDistance * 3f, speedRatio);

        Vector3 headOffset = Vector3.up * (radius + 1.0f);
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
                Debug.Log($"{name}: ⚠ Avoiding edge!");
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

        // ---------------------------------------
        // OBSTACLE AVOIDANCE
        // ---------------------------------------
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, obstacleCheckDistance, obstacleMask))
        {
            Vector3 avoidDir = Vector3.Reflect(seekDir, hit.normal);
            seekDir = Vector3.Lerp(seekDir, avoidDir, 0.9f);
        }

        // ---------------------------------------
        // STEERING & MOVEMENT
        // ---------------------------------------
        float turnRate = avoidingEdge ? turnSharpness * 0.4f : turnSharpness;

        desiredDirection = Vector3.Lerp(desiredDirection, seekDir, Time.fixedDeltaTime * turnRate);
        desiredDirection.y = 0f;
        desiredDirection.Normalize();

        Vector3 horizontalVel = rb.linearVelocity;
        horizontalVel.y = 0f;
        Vector3 steering = (desiredDirection - horizontalVel.normalized) * steerResponsiveness;
        steering.y = 0f;

        float speedMultiplier = avoidingEdge ? 0.2f : 1f;
        float currentSpeed = horizontalVel.magnitude;

        if (currentSpeed < maxSpeed)
        {
            rb.AddForce((desiredDirection * moveForce * speedMultiplier + steering) * Time.fixedDeltaTime,
                ForceMode.VelocityChange);
        }

        // Brake if close
        if (distance < closeBrakeDistance)
            rb.linearVelocity *= 0.97f;

        // Remove upward drift
        Vector3 v = rb.linearVelocity;
        if (v.y > 0.05f)
            v.y = Mathf.Lerp(v.y, 0f, 0.5f);
        rb.linearVelocity = v;

        // 🎯 Scoring is now handled by GoalTrigger.OnTriggerEnter()
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = _groundDetected ? Color.green : Color.red;
        Gizmos.DrawSphere(transform.position + Vector3.up * (radius + 1.0f), 0.1f);
    }
}