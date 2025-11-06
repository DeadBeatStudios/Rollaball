using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyPhysicsController : MonoBehaviour
{
    [Header("Targeting")]
    public Transform target;

    [Header("Movement")]
    public float moveForce = 45f;
    public float maxSpeed = 14f;
    public float turnSharpness = 10f;      // higher = quicker direction change
    public float steerResponsiveness = 8f; // how fast we realign velocity

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
    public float edgeAvoidStrength = 50f;

    [Header("AI Tuning")]
    public float predictionTime = 0.35f;
    public float closeBrakeDistance = 2.5f;

    private Rigidbody rb;
    private Vector3 desiredDirection;

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

        // --- Obstacle Avoidance ---
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit obstacleHit, obstacleCheckDistance, obstacleMask))
        {
            Vector3 avoidDir = Vector3.Reflect(seekDir, obstacleHit.normal);
            seekDir = Vector3.Lerp(seekDir, avoidDir, 0.9f);
        }

        // --- Edge Detection (no upward push) ---
        Vector3 edgeOrigin = transform.position + transform.forward * 1.2f;
        bool hasGroundAhead = Physics.Raycast(edgeOrigin, Vector3.down, edgeCheckDistance, groundMask);
        if (!hasGroundAhead)
        {
            // steer hard away from edge, not upward
            seekDir = Vector3.Lerp(seekDir, -transform.forward, 0.9f);
        }

        // --- Smooth Steering Vector ---
        desiredDirection = Vector3.Lerp(desiredDirection, seekDir, Time.fixedDeltaTime * turnSharpness);

        // keep motion planar
        desiredDirection.y = 0f;
        desiredDirection.Normalize();

        // --- Calculate velocity-based steering ---
        Vector3 horizontalVel = rb.linearVelocity;
        horizontalVel.y = 0f; // remove vertical

        Vector3 steering = (desiredDirection - horizontalVel.normalized) * steerResponsiveness;
        steering.y = 0f; // keep it planar

        // --- Main Movement Force ---
        float currentSpeed = horizontalVel.magnitude;
        if (currentSpeed < maxSpeed)
            rb.AddForce((desiredDirection * moveForce + steering) * Time.fixedDeltaTime, ForceMode.VelocityChange);

        // --- Braking Near Target ---
        if (distance < closeBrakeDistance)
            rb.linearVelocity *= 0.97f;

        // --- Clamp small upward velocity (safety net) ---
        Vector3 vel = rb.linearVelocity;
        if (vel.y > 0.05f)
            vel.y = Mathf.Lerp(vel.y, 0f, 0.5f);
        rb.linearVelocity = vel;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * obstacleCheckDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position + transform.forward, transform.position + transform.forward + Vector3.down * edgeCheckDistance);
    }
}
