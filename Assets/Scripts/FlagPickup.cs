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

    [Header("Safety")]
    [SerializeField] private float autoDropY = -5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // State
    private Transform holder;
    private bool isHeld = false;
    private Quaternion initialWorldRotation;

    private Bounds cachedGroundBounds;
    private bool groundBoundsCached = false;

    private void Start()
    {
        initialWorldRotation = transform.rotation;

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        CacheGroundBounds();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only players or enemies may pick up
        if (!other.CompareTag("Player") && !other.CompareTag("Enemy"))
            return;

        if (isHeld)
            return;

        AttachToHolder(other.transform);
    }

    private void LateUpdate()
    {
        if (!isHeld)
            return;

        if (HolderMissingOrDead())
        {
            RespawnAtRandom();
            return;
        }

        transform.position = holder.position + Vector3.up * attachHeightOffset;
        transform.rotation = initialWorldRotation;

        if (holder.position.y < autoDropY)
        {
            RespawnAtRandom();
        }
    }

    private bool HolderMissingOrDead()
    {
        if (holder == null)
            return true;

        PlayerRespawn player = holder.GetComponentInParent<PlayerRespawn>();
        if (player != null && player.IsDead)
            return true;

        if (!holder.gameObject.activeInHierarchy)
            return true;

        return false;
    }

    /// <summary>
    /// Original API: used for deaths / scoring where we want a random respawn.
    /// </summary>
    public void DropAndRespawn(
        FlagDropCause cause = FlagDropCause.Unknown,
        Transform killer = null,
        Vector3? deathPosition = null)
    {
        RespawnAtRandom();
    }

    /// <summary>
    /// NEW: Drop the flag into the world near a position (used by knockback / hits).
    /// Does NOT random-respawn, it just places the flag on the ground.
    /// </summary>
    public void DropToWorld(
        FlagDropCause cause = FlagDropCause.Unknown,
        Transform killer = null,
        Vector3? dropPosition = null)
    {
        Transform oldHolder = holder;

        holder = null;
        isHeld = false;
        transform.SetParent(null);

        if (TryGetComponent(out Collider col))
            col.enabled = true;

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;   // still behaves like a pickup
            rb.useGravity = false;
        }

        Vector3 basePos = dropPosition ?? (oldHolder != null ? oldHolder.position : transform.position);

        // Align to ground if possible
        float groundY = GetGroundHeight(basePos);
        Vector3 worldPos = new Vector3(basePos.x, groundY, basePos.z);

        transform.position = worldPos + Vector3.up * respawnHeightOffset;
        transform.rotation = initialWorldRotation;

        if (showDebugLogs)
            Debug.Log($"🏁 Flag dropped to world at {transform.position} (cause: {cause})");
    }

    /// <summary>
    /// NEW: Attach flag to a given holder (player or enemy root).
    /// Used by trigger and can be re-used by debug / special logic.
    /// </summary>
    public void AttachToHolder(Transform newHolder)
    {
        if (newHolder == null)
            return;

        holder = newHolder;
        isHeld = true;

        if (TryGetComponent(out Collider col))
            col.enabled = false;

        transform.SetParent(null);
        transform.position = holder.position + Vector3.up * attachHeightOffset;
        transform.rotation = initialWorldRotation;

        if (showDebugLogs)
            Debug.Log($"🏁 Flag attached to: {holder.name}");
    }

    // --------------------------
    // TERRAIN-FIRST RESPAWNING
    // --------------------------

    private void RespawnAtRandom()
    {
        transform.SetParent(null);
        holder = null;
        isHeld = false;

        if (TryGetComponent(out Collider col))
            col.enabled = true;

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Vector3 groundPos = GetRandomSpawn();
        transform.position = groundPos + Vector3.up * respawnHeightOffset;
        transform.rotation = initialWorldRotation;

        if (showDebugLogs)
            Debug.Log($"🏁 Flag respawned at {transform.position}");
    }

    private void CacheGroundBounds()
    {
        cachedGroundBounds = GetGroundBounds();
        groundBoundsCached = true;
    }

    public void RefreshGroundBounds()
    {
        groundBoundsCached = false;
        CacheGroundBounds();
    }

    private Vector3 GetRandomSpawn()
    {
        Bounds arena = cachedGroundBounds;
        float padX = arena.extents.x * edgePaddingPercent;
        float padZ = arena.extents.z * edgePaddingPercent;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            float rx = Random.Range(arena.min.x + padX, arena.max.x - padX);
            float rz = Random.Range(arena.min.z + padZ, arena.max.z - padZ);

            float ry = GetGroundHeight(new Vector3(rx, arena.center.y, rz));
            Vector3 spawn = new Vector3(rx, ry, rz);

            if (Terrain.activeTerrain != null)
            {
                float slope = GetSlopeAtPosition(spawn);
                if (slope <= maxSpawnSlope)
                    return spawn;
            }
            else
            {
                return spawn;
            }
        }

        Vector3 fallback = new Vector3(
            arena.center.x,
            GetGroundHeight(arena.center),
            arena.center.z
        );

        Debug.LogWarning("FlagPickup: Using fallback center spawn");
        return fallback;
    }

    private Bounds GetGroundBounds()
    {
        if (Terrain.activeTerrain != null)
        {
            Terrain t = Terrain.activeTerrain;
            Bounds b = t.terrainData.bounds;
            b.center += t.transform.position;
            return b;
        }

        GameObject[] groundObjects = GameObject.FindGameObjectsWithTag("Ground");
        Collider[] cols = groundObjects
            .Select(g => g.GetComponent<Collider>())
            .Where(c => c != null)
            .ToArray();

        if (cols.Length > 0)
        {
            Bounds b = cols[0].bounds;
            for (int i = 1; i < cols.Length; i++)
                b.Encapsulate(cols[i].bounds);
            return b;
        }

        return new Bounds(Vector3.zero, new Vector3(10, 1, 10));
    }

    private float GetGroundHeight(Vector3 world)
    {
        if (Terrain.activeTerrain != null)
            return Terrain.activeTerrain.SampleHeight(world) + Terrain.activeTerrain.transform.position.y;

        if (Physics.Raycast(world + Vector3.up * 10f, Vector3.down, out var hit, 50f, LayerMask.GetMask("Ground")))
            return hit.point.y;

        return 0f;
    }

    private float GetSlopeAtPosition(Vector3 pos)
    {
        if (Terrain.activeTerrain == null) return 0f;

        Terrain t = Terrain.activeTerrain;
        Vector3 p = t.transform.position;
        TerrainData data = t.terrainData;

        float nx = Mathf.Clamp01((pos.x - p.x) / data.size.x);
        float nz = Mathf.Clamp01((pos.z - p.z) / data.size.z);

        return data.GetSteepness(nx, nz);
    }

    // --------------------------
    // PUBLIC API
    // --------------------------

    public bool IsHeldBy(Transform t)
    {
        return isHeld && holder != null && (holder == t || holder.IsChildOf(t));
    }

    public Transform CurrentHolder => holder;
    public bool IsHeld => isHeld;
}
