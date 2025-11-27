using UnityEngine;
using System.Linq;

/// <summary>
/// Flag pickup system with terrain-first spawning.
/// Supports Unity Terrain, tagged ground objects, and graceful fallbacks.
/// </summary>
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
    [SerializeField] private float attachHeightOffset = 1.5f;

    [Header("Respawn Settings")]
    [SerializeField] private float respawnHeightOffset = 0.5f;
    [SerializeField, Range(0f, 0.5f)] private float edgePaddingPercent = 0.05f;

    [Header("Terrain Spawn Validation")]
    [SerializeField, Range(0f, 90f)] private float maxSpawnSlope = 30f;
    [SerializeField] private int maxSpawnAttempts = 10;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // State
    private Transform holder;
    private bool isHeld = false;
    private Quaternion initialWorldRotation;

    private Bounds cachedBounds;

    private void Start()
    {
        initialWorldRotation = transform.rotation;

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        cachedBounds = GetGroundBounds();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isHeld) return;

        // Only players or enemies may pick up
        if (!other.CompareTag("Player") && !other.CompareTag("Enemy"))
            return;

        AttachToHolder(other.transform);
    }

    private void LateUpdate()
    {
        if (!isHeld || holder == null)
            return;

        // If holder has a PlayerRespawn and is dead → respawn flag
        PlayerRespawn pr = holder.GetComponentInParent<PlayerRespawn>();
        if (pr != null && pr.IsDead)
        {
            RespawnAtRandom();
            return;
        }

        // Stick to holder
        transform.position = holder.position + Vector3.up * attachHeightOffset;
        transform.rotation = initialWorldRotation;
    }

    // -------------------------------------------------
    // ATTACH / DROP API
    // -------------------------------------------------

    /// <summary>
    /// Attach the flag to a player or enemy root.
    /// </summary>
    public void AttachToHolder(Transform newHolder)
    {
        if (newHolder == null) return;

        holder = newHolder;
        isHeld = true;

        if (TryGetComponent(out Collider col))
            col.enabled = false;

        transform.SetParent(null);
        transform.position = newHolder.position + Vector3.up * attachHeightOffset;
        transform.rotation = initialWorldRotation;

        if (showDebugLogs)
            Debug.Log($"🏁 Flag attached to {newHolder.name}");
    }

    /// <summary>
    /// Drop the flag into the world near a position (used by knockback / hits).
    /// Does NOT random-respawn, it just places the flag on the ground.
    /// </summary>
    public void DropToWorld(
        FlagDropCause cause = FlagDropCause.Unknown,
        Transform killer = null,
        Vector3? dropPos = null)
    {
        isHeld = false;
        Transform prevHolder = holder;
        holder = null;

        if (TryGetComponent(out Collider col))
            col.enabled = true;

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;   // keep it as a pickup, not physics debris
            rb.useGravity = false;
        }

        // Where should we drop it from?
        Vector3 basePos = dropPos ?? (prevHolder != null ? prevHolder.position : transform.position);

        // Snap to ground
        float groundY = GetGroundHeight(basePos);
        Vector3 finalPos = new Vector3(basePos.x, groundY + respawnHeightOffset, basePos.z);

        transform.position = finalPos;
        transform.rotation = initialWorldRotation;

        if (showDebugLogs)
            Debug.Log($"🏁 Flag dropped on ground at {finalPos} (cause: {cause})");
    }

    /// <summary>
    /// Only for scoring & death logic (random respawn in arena).
    /// </summary>
    public void DropAndRespawn(FlagDropCause cause = FlagDropCause.Unknown)
    {
        RespawnAtRandom();
    }

    // -------------------------------------------------
    // RESPAWN / GROUND HELPERS
    // -------------------------------------------------

    private void RespawnAtRandom()
    {
        isHeld = false;
        holder = null;

        if (TryGetComponent(out Collider col))
            col.enabled = true;

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Vector3 point = GetRandomPointInArena();
        transform.position = point + Vector3.up * respawnHeightOffset;
        transform.rotation = initialWorldRotation;

        if (showDebugLogs)
            Debug.Log($"🏁 Flag respawned randomly at {transform.position}");
    }

    private Bounds GetGroundBounds()
    {
        // 1) Terrain first
        if (Terrain.activeTerrain != null)
        {
            Terrain t = Terrain.activeTerrain;
            Bounds b = t.terrainData.bounds;
            b.center += t.transform.position;
            return b;
        }

        // 2) Ground-tagged colliders
        var cols = GameObject.FindGameObjectsWithTag("Ground")
            .Select(g => g.GetComponent<Collider>())
            .Where(c => c != null)
            .ToArray();

        if (cols.Length == 0)
        {
            Debug.LogWarning("FlagPickup: No terrain or Ground-tagged colliders found. Using fallback bounds.");
            return new Bounds(Vector3.zero, new Vector3(10f, 1f, 10f));
        }

        Bounds combined = cols[0].bounds;
        for (int i = 1; i < cols.Length; i++)
            combined.Encapsulate(cols[i].bounds);

        return combined;
    }

    private float GetGroundHeight(Vector3 world)
    {
        if (Terrain.activeTerrain != null)
        {
            Terrain t = Terrain.activeTerrain;
            return t.SampleHeight(world) + t.transform.position.y;
        }

        if (Physics.Raycast(world + Vector3.up * 10f, Vector3.down, out var hit, 50f, LayerMask.GetMask("Ground")))
            return hit.point.y;

        return world.y; // fallback: don't snap if nothing hit
    }

    private Vector3 GetRandomPointInArena()
    {
        Bounds a = cachedBounds;
        float px = a.extents.x * edgePaddingPercent;
        float pz = a.extents.z * edgePaddingPercent;

        float rx = Random.Range(a.min.x + px, a.max.x - px);
        float rz = Random.Range(a.min.z + pz, a.max.z - pz);
        float ry = GetGroundHeight(new Vector3(rx, a.center.y, rz));

        return new Vector3(rx, ry, rz);
    }

    // -------------------------------------------------
    // PUBLIC API
    // -------------------------------------------------

    public bool IsHeldBy(Transform t)
        => isHeld && holder != null && (holder == t || holder.IsChildOf(t));

    public Transform CurrentHolder => holder;
    public bool IsHeld => isHeld;
}
