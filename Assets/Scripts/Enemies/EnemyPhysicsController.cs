using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyPhysicsController : MonoBehaviour
{
    [Header("Targeting")]
    public Transform target;
    public FlagPickup flag; // reference to FlagPickup in scene

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
    public float edgeRecoveryDelay = 1.5f; // seconds to wait before chasing again

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

        // Auto-find the flag if not assigned
        if (flag == null)
        {
            FlagPickup foundFlag = Object.FindAnyObjectByType<FlagPickup>();
            if (foundFlag != null)
            {
                flag = foundFlag;
                Debug.Log($"{name}: Found flag automatically ({flag.name})");
            }
            else
            {
                Debug.LogWarning($"{name}: No FlagPickup found in scene!");
            }
        }
    }

    private void FixedUpdate()
    {
        // --- Dynamic Target Selection ---
        if (flag != null)
        {
            if (!flag.IsHeld)
            {
                // Flag is free on the ground
                target = flag.transform;
            }
            else
            {
                Transform holder = flag.CurrentHolder;
                if (holder != null && holder != transform)
                {
                    // Chase whoever has the flag
                    target = holder;
                }
                else
                {
                    // We are holding the flag, stop chasing
                    target = null;
                }
            }
        }

        if (!target) return;

        // --- Predictive Targeting ---
        Vector3 targetVel = Vector3.zero;
        if (target.TryGetComponent<Rigidbody>(out var targetRb))
            targetVel = targetRb.linearVelocity;

        Vector3 predictedPos = target.position + targetVel * predictionTime;
        Vector3 toTarget = predictedPos - transform.position;
        float distance = toTarget.magnitude;
        Vector3 seekDir = toTarget.normalized;

        // --- Helmet-Cam Edge Detection (velocity-based, slight upward tilt) ---
        float speed = rb.linearVelocity.magnitude;
        if (speed < 0.1f) speed = 0.1f;

        float speedRatio = Mathf.Clamp01(speed / maxSpeed);
        float lookDistance = Mathf.Lerp(edgeCheckDistance, edgeCheckDistance * 3f, speedRatio);

        // Raise origin slightly higher
        Vector3 headHeightOffset = Vector3.up * (radius + 1.0f);
        Vector3 origin = transform.position + headHeightOffset;

        // Movement direction (not rotation)
        Vector3 moveDir = rb.linearVelocity.sqrMagnitude > 0.01f
            ? rb.linearVelocity.normalized
            : transform.forward;

        // Slightly upward angled rays
        float verticalTilt = -0.25f;
        Vector3 forwardDown = (moveDir + Vector3.up * verticalTilt).normalized;
        Vector3 leftDown = (Quaternion.Euler(0, -25f, 0) * moveDir + Vector3.up * verticalTilt).normalized;
        Vector3 rightDown = (Quaternion.Euler(0, 25f, 0) * moveDir + Vector3.up * verticalTilt).normalized;

        // Perform raycasts
        bool centerHit = Physics.Raycast(origin, forwardDown, out RaycastHit hitC, lookDistance, groundMask);
        bool leftHit = Physics.Raycast(origin, leftDown, out RaycastHit hitL, lookDistance, groundMask);
        bool rightHit = Physics.Raycast(origin, rightDown, out RaycastHit hitR, lookDistance, groundMask);

        // Combine
        _groundDetected = centerHit || leftHit || rightHit;

        // Debug visualize
        Color rayColor = _groundDetected ? Color.green : Color.red;
        Debug.DrawRay(origin, forwardDown * lookDistance, rayColor);
        Debug.DrawRay(origin, leftDown * lookDistance, rayColor);
        Debug.DrawRay(origin, rightDown * lookDistance, rayColor);

        // --- Edge Detection Logic ---
        if (!_groundDetected)
        {
            if (!avoidingEdge)
            {
                avoidingEdge = true;
                edgeTimer = edgeRecoveryDelay;
                Debug.Log($"{name}: ⚠️ Avoiding edge!");
            }

            // Brake hard proportional to speed
            float brakeStrength = Mathf.Lerp(edgeBrakeForce, edgeBrakeForce * 2f, speedRatio);
            rb.AddForce(-rb.linearVelocity * brakeStrength * Time.fixedDeltaTime, ForceMode.Acceleration);

            // Turn back toward last safe direction
            Vector3 avoidDir = Vector3.Lerp(-moveDir, lastSafeDirection, 0.6f);
            desiredDirection = Vector3.Lerp(desiredDirection, avoidDir, 0.8f);
        }
        else
        {
            if (avoidingEdge)
            {
                edgeTimer -= Time.fixedDeltaTime;
                if (edgeTimer <= 0f)
                {
                    avoidingEdge = false;
                    Debug.Log($"{name}: ✅ Back to chase mode!");
                }
            }

            lastSafeDirection = moveDir;
        }

        // --- Obstacle Avoidance ---
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit obstacleHit, obstacleCheckDistance, obstacleMask))
        {
            Vector3 avoidDir = Vector3.Reflect(seekDir, obstacleHit.normal);
            seekDir = Vector3.Lerp(seekDir, avoidDir, 0.9f);
        }

        // --- Smooth Steering ---
        float turnRate = avoidingEdge ? turnSharpness * 0.4f : turnSharpness;
        desiredDirection = Vector3.Lerp(desiredDirection, seekDir, Time.fixedDeltaTime * turnRate);
        desiredDirection.y = 0f;
        desiredDirection.Normalize();

        // --- Steering Force ---
        Vector3 horizontalVel = rb.linearVelocity;
        horizontalVel.y = 0f;
        Vector3 steering = (desiredDirection - horizontalVel.normalized) * steerResponsiveness;
        steering.y = 0f;

        // --- Movement Force ---
        float speedMultiplier = avoidingEdge ? 0.2f : 1f;
        float currentSpeed = horizontalVel.magnitude;
        if (currentSpeed < maxSpeed)
            rb.AddForce((desiredDirection * moveForce * speedMultiplier + steering) * Time.fixedDeltaTime, ForceMode.VelocityChange);

        // --- Brake when close to target ---
        if (distance < closeBrakeDistance)
            rb.linearVelocity *= 0.97f;

        // --- Clamp vertical drift ---
        Vector3 vel = rb.linearVelocity;
        if (vel.y > 0.05f)
            vel.y = Mathf.Lerp(vel.y, 0f, 0.5f);
        rb.linearVelocity = vel;
    }

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Color rayColor = _groundDetected ? Color.green : Color.red;
            Gizmos.color = rayColor;
            Gizmos.DrawSphere(transform.position + Vector3.up * (radius + 1.0f), 0.1f);
        }
    }
}
