using System.Collections;
using UnityEngine;

public class LavaBall : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 4f;
    [SerializeField] private float arriveThreshold = 0.15f;

    [Header("Trail")]
    [SerializeField] private float trailSpawnInterval = 0.15f;

    [Header("Collision")]
    [SerializeField] private LayerMask floorLayer;
    [SerializeField] private float floorCheckDistance = 1.2f;

    [Header("Lifetime")]
    [SerializeField] private float lingerTime = 4f;

    private Vector3 targetPosition;
    private bool isMoving;
    private float trailTimer;

    // ============================
    // INIT
    // ============================
    public void Initialize(Vector3 target)
    {
        targetPosition = target;
        isMoving = true;
        trailTimer = 0f;
    }

    private void Update()
    {
        if (!isMoving) return;

        Move();
        SpawnTrail();
        CheckFloor();
    }

    // ============================
    // MOVEMENT
    // ============================
    private void Move()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) <= arriveThreshold)
        {
            HitFloor();
        }
    }

    // ============================
    // TRAIL
    // ============================
    private void SpawnTrail()
    {
        trailTimer += Time.deltaTime;

        if (trailTimer >= trailSpawnInterval)
        {
            GameObject trail = Instantiate(gameObject, transform.position, Quaternion.identity);

            Destroy(trail.GetComponent<LavaBall>());
            Destroy(trail, lingerTime);

            trailTimer = 0f;
        }
    }

    // ============================
    // FLOOR CHECK
    // ============================
    private void CheckFloor()
    {
        if (Physics.Raycast(transform.position, Vector3.down,
            out RaycastHit hit, floorCheckDistance, floorLayer))
        {
            HitFloor();
        }
    }

    // ============================
    // IMPACT
    // ============================
    private void HitFloor()
    {
        isMoving = false;

        // Optional later:
        // GetComponent<LavaDissolve>()?.StartDissolve();

        Destroy(gameObject, lingerTime);
    }
}
