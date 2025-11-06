using UnityEngine;

public class FlagPickup : MonoBehaviour
{
    // ---------------------------
    //   ENUM: Reason for drop
    // ---------------------------
    public enum FlagDropCause
    {
        KilledByEnemy,
        SelfDestruct,
        FellOffMap,
        Unknown
    }

    [Header("Flag Visual Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, 0f);
    [SerializeField] private float floatAmplitude = 0.2f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float rotationSpeed = 45f;
    [SerializeField, Range(0.1f, 1f)] private float shrinkScale = 0.5f;
    [SerializeField] private float shrinkSpeed = 6f;

    [Header("Respawn Settings")]
    [SerializeField] private float edgePaddingPercent = 0.05f;   // inset from PhysicsFloor edges
    [SerializeField] private float spawnLift = 0.3f;             // lift above ground surface

    private Transform holder;
    private bool collected = false;
    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;

        // Keep the flag static in the world (no gravity)
        if (TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") && !other.CompareTag("Enemy"))
            return;

        holder = other.transform;
        collected = true;

        if (TryGetComponent(out Collider col))
            col.enabled = false;

        // Shrink while held
        targetScale = originalScale * shrinkScale;
        Debug.Log($"Flag collected by: {holder.name}");
    }

    private void LateUpdate()
    {
        if (collected && holder != null)
        {
            // Shrink and follow
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * shrinkSpeed);
            float bobOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            Vector3 desiredPos = holder.position + offset + new Vector3(0f, bobOffset, 0f);
            transform.position = desiredPos;
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
        else
        {
            // Not held → ensure full size
            if (transform.localScale != originalScale)
                transform.localScale = originalScale;
        }
    }

    // 🔹 Called externally when the holder dies
    public void DropAndRespawn(FlagDropCause cause, Transform killer = null, Vector3? deathPosition = null)
    {
        // Reset ownership
        holder = null;
        collected = false;
        transform.localScale = originalScale;
        targetScale = originalScale;

        // Re-enable collider
        if (TryGetComponent(out Collider col))
            col.enabled = true;

        // Flag is static
        if (TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Vector3 spawnPos;

        switch (cause)
        {
            case FlagDropCause.KilledByEnemy:
                if (killer != null)
                {
                    // Instant transfer to killer
                    holder = killer;
                    collected = true;
                    targetScale = originalScale * shrinkScale;
                    Debug.Log($"Flag transferred to {killer.name} (kill reward)");
                    return;
                }
                spawnPos = GetRandomSpawn();
                break;

            case FlagDropCause.SelfDestruct:
            case FlagDropCause.FellOffMap:
            case FlagDropCause.Unknown:
                // Random safe respawn
                spawnPos = GetRandomSpawn();
                break;

            default:
                // Drop at death position
                spawnPos = deathPosition ?? GetRandomSpawn();
                break;
        }

        transform.position = spawnPos + Vector3.up * spawnLift;
        Debug.Log($"Flag respawned at {transform.position} due to {cause}");
    }

    // 🔹 Generates a safe random position on the PhysicsFloor
    private Vector3 GetRandomSpawn()
    {
        Bounds floorBounds = GetPhysicsFloorBounds();
        float padX = floorBounds.extents.x * edgePaddingPercent;
        float padZ = floorBounds.extents.z * edgePaddingPercent;

        float rx = Random.Range(floorBounds.min.x + padX, floorBounds.max.x - padX);
        float rz = Random.Range(floorBounds.min.z + padZ, floorBounds.max.z - padZ);
        float rayStartY = floorBounds.max.y + 5f;

        if (Physics.Raycast(new Vector3(rx, rayStartY, rz), Vector3.down, out RaycastHit hit, 10f, LayerMask.GetMask("Ground")))
            return hit.point;

        // Fallback
        return new Vector3(rx, floorBounds.max.y, rz);
    }

    private Bounds GetPhysicsFloorBounds()
    {
        GameObject floor = GameObject.Find("PhysicsFloor");
        if (floor != null && floor.TryGetComponent(out BoxCollider col))
            return col.bounds;

        Debug.LogWarning("PhysicsFloor not found — using fallback bounds.");
        return new Bounds(Vector3.zero, new Vector3(10f, 1f, 8f));
    }
    public bool IsHeldBy(Transform player)
    {
        return (holder != null && holder == player);
    }
}
