using UnityEngine;

public class LavaBall : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 4f;
    [SerializeField] private float arriveThreshold = 0.15f;

    [Header("Collision")]
    [SerializeField] private LayerMask floorLayer;
    [SerializeField] private float floorCheckDistance = 1.2f;

    [Header("Lifetime")]
    [SerializeField] private float lingerTime = 4f;

    private Vector3 targetPosition;
    private bool isMoving;

    public void Initialize(Vector3 target)
    {
        targetPosition = target;
        isMoving = true;
    }

    private void Update()
    {
        if (!isMoving) return;

        Move();
        CheckFloor();
    }

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

    private void CheckFloor()
    {
        if (Physics.Raycast(transform.position, Vector3.down,
            out RaycastHit hit, floorCheckDistance, floorLayer))
        {
            HitFloor();
        }
    }

    private void HitFloor()
    {
        isMoving = false;

        // Tell particles / dissolve here if needed
        // GetComponent<LavaDissolve>()?.StartDissolve();

        Destroy(gameObject, lingerTime);
    }
}
