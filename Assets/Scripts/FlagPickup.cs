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
    [Tooltip("Vertical offset above holder's pivot when attached.")]
    [SerializeField] private float attachHeightOffset = 1.5f;

    [Header("Respawn Settings")]
    [Tooltip("How far above the ground the flag respawns.")]
    [SerializeField] private float respawnHeightOffset = 0.5f;

    [Tooltip("Percent of arena bounds to avoid near edges when random spawning.")]
    [SerializeField, Range(0f, 0.5f)] private float edgePaddingPercent = 0.05f;

    [Header("Safety")]
    [Tooltip("If the holder falls below this Y value, the flag will auto-drop and respawn.")]
    [SerializeField] private float autoDropY = -5f;

    private Transform holder;
    private bool isHeld = false;
    private Quaternion initialWorldRotation;

    private void Start()
    {
        initialWorldRotation = transform.rotation;

        // Ensure the flag behaves like a VFX object, not a physics projectile.
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

        if (isHeld) return; // already attached to someone

        // Prefer a child "VisualModel" if present, otherwise use root
        Transform visualRoot = other.transform.Find("VisualModel");
        holder = visualRoot != null ? visualRoot : other.transform;

        isHeld = true;

        // Disable our own collider while held
        if (TryGetComponent(out Collider col))
            col.enabled = false;

        // ⭐ CRITICAL FIX: DO NOT parent the flag - just follow it manually
        // This prevents the flag from becoming inactive when VisualModel is hidden
        transform.position = holder.position + Vector3.up * attachHeightOffset;
        transform.rotation = initialWorldRotation;

        Debug.Log($"🏁 Flag collected by: {holder.name}");
    }

    private void LateUpdate()
    {
        if (!isHeld) return;

        // If holder is gone or considered dead → drop + respawn.
        if (HolderMissingOrDead())
        {
            RespawnAtRandom();
            return;
        }

        // Follow holder, stay upright
        transform.position = holder.position + Vector3.up * attachHeightOffset;
        transform.rotation = initialWorldRotation;

        // If holder falls below threshold → drop + respawn
        if (holder.position.y < autoDropY)
        {
            RespawnAtRandom();
        }
    }

    private bool HolderMissingOrDead()
    {
        if (holder == null)
            return true;

        // If this holder belongs to a player, consult PlayerRespawn
        PlayerRespawn player = holder.GetComponentInParent<PlayerRespawn>();
        if (player != null)
        {
            if (player.IsDead)
                return true;
        }

        // For enemies or other entities, treat inactive hierarchy as dead
        if (!holder.gameObject.activeInHierarchy)
            return true;

        return false;
    }

    /// <summary>
    /// Public API kept for compatibility.
    /// Internally we now always do a clean random respawn.
    /// </summary>
    public void DropAndRespawn(
        FlagDropCause cause = FlagDropCause.Unknown,
        Transform killer = null,
        Vector3? deathPosition = null)
    {
        // Ignore killer / deathPosition for now – keep behavior simple and robust.
        RespawnAtRandom();
    }

    private void RespawnAtRandom()
    {
        // ⭐ CRITICAL FIX: Ensure parent is cleared (in case it was set elsewhere)
        transform.SetParent(null);
        holder = null;
        isHeld = false;

        // Re-enable collider so it can be picked up again
        if (TryGetComponent(out Collider col))
            col.enabled = true;

        // Keep rigidbody safe & kinematic
        if (TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Pick a safe ground position
        Vector3 groundPos = GetRandomSpawn();
        transform.position = groundPos + Vector3.up * respawnHeightOffset;
        transform.rotation = initialWorldRotation;

        Debug.Log($"🏁 Flag respawned at {transform.position}");
    }

    private Vector3 GetRandomSpawn()
    {
        Bounds floorBounds = GetPhysicsFloorBounds();
        float padX = floorBounds.extents.x * edgePaddingPercent;
        float padZ = floorBounds.extents.z * edgePaddingPercent;

        float rx = Random.Range(floorBounds.min.x + padX, floorBounds.max.x - padX);
        float rz = Random.Range(floorBounds.min.z + padZ, floorBounds.max.z - padZ);
        float rayStartY = floorBounds.max.y + 5f;

        Vector3 rayOrigin = new Vector3(rx, rayStartY, rz);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 10f, LayerMask.GetMask("Ground")))
            return hit.point;

        // Fallback if raycast misses
        return new Vector3(rx, floorBounds.max.y, rz);
    }

    private Bounds GetPhysicsFloorBounds()
    {
        GameObject floor = GameObject.Find("PhysicsFloor");
        if (floor != null && floor.TryGetComponent(out BoxCollider col))
            return col.bounds;

        Debug.LogWarning("FlagPickup: PhysicsFloor not found — using fallback bounds.");
        return new Bounds(Vector3.zero, new Vector3(10f, 1f, 8f));
    }

    // Helper accessors
    public bool IsHeldBy(Transform t)
    {
        if (!isHeld || holder == null || t == null)
            return false;

        // True if the holder is this transform or a child of it
        return holder == t || holder.IsChildOf(t);
    }

    public Transform CurrentHolder => holder;
    public bool IsHeld => isHeld;
}