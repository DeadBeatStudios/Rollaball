using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent), typeof(Rigidbody))]
public class HybridEnemyController : MonoBehaviour
{
    [Header("References")]
    public Transform target;

    [Header("Movement Settings")]
    public float engageRange = 12f;
    public float attackRange = 2f;
    public float moveForce = 15f;
    public float radius = 0.5f;
    public float rotationMultiplier = 1.0f;
    public float rotationSmooth = 8f;
    public float maxSpeed = 8f;

    [Header("Physics Settings")]
    public float knockbackForce = 10f;
    public float knockbackDuration = 0.4f;

    [Header("Respawn")]
    public float respawnYThreshold = -5f;
    public Transform[] respawnPoints;

    private NavMeshAgent agent;
    private Rigidbody rb;
    private bool isKnockedBack;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;

        agent.updateRotation = false;
        agent.updatePosition = true;
    }

    private void Update()
    {
        if (isKnockedBack || target == null)
            return;

        // --- Respawn Check ---
        if (transform.position.y < respawnYThreshold)
        {
            Respawn();
            return;
        }

        // --- Engage & Chase ---
        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > engageRange)
        {
            agent.ResetPath();
        }
        else
        {
            agent.SetDestination(target.position);
        }

        // --- Ram when close ---
        if (distance <= attackRange && !isKnockedBack)
        {
            Vector3 attackDir = (target.position - transform.position).normalized;
            StartCoroutine(RamPush(attackDir));
        }

        // --- Rolling Visual Animation ---
        Vector3 velocity = agent.velocity;

        if (velocity.sqrMagnitude > 0.001f)
        {
            float distanceMoved = velocity.magnitude * Time.deltaTime;
            float angle = (distanceMoved / (2f * Mathf.PI * radius)) * 360f * rotationMultiplier;
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, velocity.normalized);

            transform.Rotate(rotationAxis, angle, Space.World);
        }
    }

    private IEnumerator RamPush(Vector3 dir)
    {
        isKnockedBack = true;
        agent.enabled = false;
        rb.isKinematic = false;

        // Gentle forward pressure (not an impulse)
        rb.AddForce(dir * moveForce, ForceMode.Force);

        yield return new WaitForSeconds(knockbackDuration);

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        agent.enabled = true;

        isKnockedBack = false;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!target || isKnockedBack) return;

        if (collision.transform == target)
        {
            Vector3 pushDir = (target.position - transform.position).normalized;
            rb.AddForce(pushDir * moveForce * 0.5f, ForceMode.Force);
        }
    }

    private void Respawn()
    {
        if (respawnPoints == null || respawnPoints.Length == 0)
        {
            transform.position = new Vector3(transform.position.x, 1f, transform.position.z);
            return;
        }

        int i = Random.Range(0, respawnPoints.Length);
        Transform spawn = respawnPoints[i];

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        agent.enabled = true;

        transform.position = spawn.position + Vector3.up * 0.5f;
        transform.rotation = Quaternion.identity;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, engageRange);
    }
    private void Start()
    {
        Debug.Log("Agent updateRotation is " + agent.updateRotation);
    }
}
