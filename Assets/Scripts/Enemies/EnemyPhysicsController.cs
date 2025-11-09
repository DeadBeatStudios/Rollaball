using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyPhysicsController : MonoBehaviour
{
    [Header("Targeting")]
    public Transform target;

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
    public float edgeCheckDistance = 1.75f;   // base distance
    public float edgeBrakeForce = 5f;
    public float safeTurnForce = 20f;

    [Header("AI Tuning")]
    public float predictionTime = 0.35f;
    public float closeBrakeDistance = 2.5f;

    // --- Private State ---
    private Rigidbody rb;
    private Vector3 desiredDirection;
    private bool avoidingEdge = false;
    private bool _groundDetected = true;
    private Vector3 lastSafeDirection = Vector3.forward;
    private Vector3 edgeOrigin;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.mass = mass;
        rb.linearDamping = linearDamping;
        rb.angularDamping = angularDamping;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void FixedUpdate()
    {
        if (!target) return;

        // --- Predictive Targeting ---
        Vector3 targetVel = Vector3.zero;
        if (target.TryGetComponent<Rigidbody>(out var targetRb))
            targetVel = targetRb.linearVelocity;

        Vector3 predictedPos = target.position + targetVel * predictionTime;
        Vector3 toTarget = predictedPos - transform.position;
        float distance = toTarget.magnitude;
        Vector3 seekDir = toTarget.normalized;

        // --- Helmet-Cam Edge Detection (velocity-based, world-downward tilt) ---
        float speed = rb.linearVelocity.magnitude;
        if (speed < 0.1f) speed = 0.1f;

        float speedRatio = Mathf.Clamp01(speed / maxSpeed);
        float lookDistance = Mathf.Lerp(edgeCheckDistance, edgeCheckDistance * 3f, speedRatio);

        // Origin slightly above enemy
        Vector3 headHeightOffset = Vector3.up * (radius + 0.75f);
        Vector3 origin = transform.position + headHeightOffset;

        // Movement direction (not rotation)
        Vector3 moveDir = rb.linearVelocity.sqrMagnitude > 0.01f
            ? rb.linearVelocity.normalized
            : transform.forward;

        // Always tilt slightly downward toward ground in world space
        Vector3 forwardDown = (moveDir + Vector3.down * 0.5f).normalized;
        Vector3 leftDown = (Quaternion.Euler(0, -25f, 0) * moveDir + Vector3.down * 0.5f).normalized;
        Vector3 rightDown = (Quaternion.Euler(0, 25f, 0) * moveDir + Vector3.down * 0.5f).normalized;

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

        // Reaction
        if (!_groundDetected)
        {
            avoidingEdge = true;

            // Brake proportional to speed
            float brakeStrength = Mathf.Lerp(edgeBrakeForce, edgeBrakeForce * 2f, speedRatio);
            rb.AddForce(-rb.linearVelocity * brakeStrength * Time.fixedDeltaTime, ForceMode.Acceleration);

            // Turn back toward last safe direction
            Vector3 avoidDir = Vector3.Lerp(-moveDir, lastSafeDirection, 0.6f);
            desiredDirection = Vector3.Lerp(desiredDirection, avoidDir, 0.8f);
        }
        else
        {
            avoidingEdge = false;
            lastSafeDirection = moveDir;
        }

        // --- Obstacle Avoidance ---
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit obstacleHit, obstacleCheckDistance, obstacleMask))
        {
            Vector3 avoidDir = Vector3.Reflect(seekDir, obstacleHit.normal);
            seekDir = Vector3.Lerp(seekDir, avoidDir, 0.9f);
        }

        // --- Smooth Steering ---
        desiredDirection = Vector3.Lerp(desiredDirection, seekDir, Time.fixedDeltaTime * turnSharpness);
        desiredDirection.y = 0f;
        desiredDirection.Normalize();

        // --- Steering Force ---
        Vector3 horizontalVel = rb.linearVelocity;
        horizontalVel.y = 0f;
        Vector3 steering = (desiredDirection - horizontalVel.normalized) * steerResponsiveness;
        steering.y = 0f;

        // --- Movement Force ---
        float speedMultiplier = avoidingEdge ? 0.6f : 1f;
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
            Gizmos.DrawSphere(transform.position + Vector3.up * (radius + 0.75f), 0.1f);
        }
    }
}
