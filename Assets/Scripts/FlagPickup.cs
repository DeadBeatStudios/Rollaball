using UnityEngine;

public class FlagPickup : MonoBehaviour
{
    public enum FlagDropCause
    {
        KilledByEnemy,
        SelfDestruct,
        FellOffMap,
        Unknown
    }

    [Header("Attachment Settings")]
    [Tooltip("Vertical offset above holder’s pivot when attached.")]
    [SerializeField] private float attachHeightOffset = 1.5f;

    [Header("Respawn Settings")]
    [Tooltip("How far above the ground the flag respawns.")]
    [SerializeField] private float respawnHeightOffset = 0.5f;
    [SerializeField] private float edgePaddingPercent = 0.05f;

    [Header("Safety")]
    [Tooltip("If the holder falls below this Y value, the flag will auto-drop and respawn.")]
    [SerializeField] private float autoDropY = -5f;

    private Transform holder;
    private bool collected = false;
    private Quaternion initialWorldRotation;

    private void Start()
    {
        initialWorldRotation = transform.rotation;

        // Particle flag: static until collected
        if (TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only players or enemies can pick up
        if (!other.CompareTag("Player") && !other.CompareTag("Enemy"))
            return;

        if (collected) return; // already held

        holder = other.transform;
        collected = true;

        // Disable collider while held
        if (TryGetComponent(out Collider col))
            col.enabled = false;

        // Attach to holder’s center (offset upward)
        transform.SetParent(holder, worldPositionStays: true);
        transform.position = holder.position + Vector3.up * attachHeightOffset;

        // Keep upright
        transform.rotation = initialWorldRotation;

        Debug.Log($"🏁 Flag collected by: {holder.name}");
    }

    private void LateUpdate()
    {
        if (!collected) return;

        // If holder destroyed or disabled -> respawn
        if (holder == null || !holder.gameObject.activeInHierarchy)
        {
            DropAndRespawn(FlagDropCause.Unknown);
            return;
        }

        // Follow holder with offset, stay upright
        transform.position = holder.position + Vector3.up * attachHeightOffset;
        transform.rotation = initialWorldRotation;

        // Fell off map? -> auto drop & respawn
        if (holder.position.y < autoDropY)
        {
            DropAndRespawn(FlagDropCause.FellOffMap);
        }
    }

    public void DropAndRespawn(FlagDropCause cause, Transform killer = null, Vector3? deathPosition = null)
    {
        transform.SetParent(null);
        collected = false;

        // Re-enable collider
        if (TryGetComponent(out Collider col))
            col.enabled = true;

        // Keep kinematic
        if (TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        transform.rotation = initialWorldRotation;

        Vector3 spawnPos;

        switch (cause)
        {
            case FlagDropCause.KilledByEnemy:
                if (killer != null)
                {
                    // Transfer instantly to killer
                    holder = killer;
                    collected = true;

                    transform.SetParent(killer, worldPositionStays: true);
                    transform.position = killer.position + Vector3.up * attachHeightOffset;
                    transform.rotation = initialWorldRotation;

                    Debug.Log($"🏁 Flag transferred to {killer.name}");
                    return;
                }
                spawnPos = GetRandomSpawn();
                break;

            case FlagDropCause.SelfDestruct:
            case FlagDropCause.FellOffMap:
            case FlagDropCause.Unknown:
                spawnPos = deathPosition ?? GetRandomSpawn();
                break;

            default:
                spawnPos = deathPosition ?? GetRandomSpawn();
                break;
        }

        // ✅ Ensure flag spawns slightly above ground
        transform.position = spawnPos + Vector3.up * respawnHeightOffset;
        Debug.Log($"Flag respawned at {transform.position} due to {cause}");
    }

    private Vector3 GetRandomSpawn()
    {
        Bounds floorBounds = GetPhysicsFloorBounds();
        float padX = floorBounds.extents.x * edgePaddingPercent;
        float padZ = floorBounds.extents.z * edgePaddingPercent;

        float rx = Random.Range(floorBounds.min.x + padX, floorBounds.max.x - padX);
        float rz = Random.Range(floorBounds.min.z + padZ, floorBounds.max.z - padZ);
        float rayStartY = floorBounds.max.y + 5f;

        // Detect ground height
        if (Physics.Raycast(new Vector3(rx, rayStartY, rz), Vector3.down, out RaycastHit hit, 10f, LayerMask.GetMask("Ground")))
            return hit.point;

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

    // Helper accessors
    public bool IsHeldBy(Transform t) => holder != null && holder == t;
    public Transform CurrentHolder => holder;
    public bool IsHeld => holder != null;
}
