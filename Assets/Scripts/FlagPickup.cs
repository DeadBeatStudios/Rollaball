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
    [Tooltip("Vertical offset above holder's pivot when attached.")]
    [SerializeField] private float attachHeightOffset = 1.5f;

    [Header("Respawn Settings")]
    [Tooltip("How far above the ground the flag respawns.")]
    [SerializeField] private float respawnHeightOffset = 0.5f;

    [Tooltip("Percent of arena bounds to avoid near edges when random spawning.")]
    [SerializeField, Range(0f, 0.5f)] private float edgePaddingPercent = 0.05f;

    [Header("Terrain Spawn Validation")]
    [Tooltip("Maximum slope angle (degrees) for valid spawn points. Set to 90 to disable.")]
    [SerializeField, Range(0f, 90f)] private float maxSpawnSlope = 30f;

    [Tooltip("Number of attempts to find valid flat spawn location.")]
    [SerializeField] private int maxSpawnAttempts = 10;

    [Header("Safety")]
    [Tooltip("If the holder falls below this Y value, the flag will auto-drop and respawn.")]
    [SerializeField] private float autoDropY = -5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // State
    private Transform holder;
    private bool isHeld = false;
    private Quaternion initialWorldRotation;

    // Cached references (performance optimization)
    private GameObject cachedGroundObject;
    private Bounds cachedGroundBounds;
    private bool groundBoundsCached = false;

    private void Start()
    {
        initialWorldRotation = transform.rotation;

        // Ensure the flag behaves like a pickup, not physics-driven
        if (TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // 🔥 ENHANCEMENT: Cache ground bounds at start
        CacheGroundBounds();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only players or enemies can pick up
        if (!other.CompareTag("Player") && !other.CompareTag("Enemy"))
            return;

        if (isHeld) return;

        // Prefer a child "VisualModel" if present
        Transform visualRoot = other.transform.Find("VisualModel");
        holder = visualRoot != null ? visualRoot : other.transform;

        isHeld = true;

        if (TryGetComponent(out Collider col))
            col.enabled = false;

        transform.position = holder.position + Vector3.up * attachHeightOffset;
        transform.rotation = initialWorldRotation;

        if (showDebugLogs)
            Debug.Log($"🏁 Flag collected by: {holder.name}");
    }

    private void LateUpdate()
    {
        if (!isHeld) return;

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

    public void DropAndRespawn(
        FlagDropCause cause = FlagDropCause.Unknown,
        Transform killer = null,
        Vector3? deathPosition = null)
    {
        RespawnAtRandom();
    }

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

    // ============================================================
    // TERRAIN-FIRST GROUND SYSTEM (ENHANCED)
    // ============================================================

    /// <summary>
    /// Cache ground bounds on startup for performance
    /// </summary>
    private void CacheGroundBounds()
    {
        cachedGroundBounds = GetGroundBounds();
        groundBoundsCached = true;

        if (showDebugLogs)
            Debug.Log($"FlagPickup: Cached ground bounds {cachedGroundBounds}");
    }

    /// <summary>
    /// Public method to refresh ground bounds if level changes at runtime
    /// </summary>
    public void RefreshGroundBounds()
    {
        groundBoundsCached = false;
        CacheGroundBounds();

        if (showDebugLogs)
            Debug.Log("FlagPickup: Ground bounds refreshed");
    }

    /// <summary>
    /// Get random spawn location with slope validation
    /// </summary>
    private Vector3 GetRandomSpawn()
    {
        Bounds arena = groundBoundsCached ? cachedGroundBounds : GetGroundBounds();

        float padX = arena.extents.x * edgePaddingPercent;
        float padZ = arena.extents.z * edgePaddingPercent;

        // 🔥 ENHANCEMENT: Attempt to find valid flat spawn location
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            float rx = Random.Range(arena.min.x + padX, arena.max.x - padX);
            float rz = Random.Range(arena.min.z + padZ, arena.max.z - padZ);
            Vector3 testPoint = new Vector3(rx, arena.max.y + 2f, rz);

            float ry = GetGroundHeight(testPoint);
            Vector3 spawnPos = new Vector3(rx, ry, rz);

            // 🔥 ENHANCEMENT: Validate slope angle for terrain spawns
            if (Terrain.activeTerrain != null && maxSpawnSlope < 90f)
            {
                float slope = GetSlopeAtPosition(spawnPos);

                if (slope <= maxSpawnSlope)
                {
                    if (showDebugLogs && attempt > 0)
                        Debug.Log($"FlagPickup: Found valid spawn after {attempt + 1} attempts (slope: {slope:F1}°)");
                    return spawnPos;
                }

                if (showDebugLogs && attempt == maxSpawnAttempts - 1)
                    Debug.LogWarning($"FlagPickup: All {maxSpawnAttempts} spawn attempts exceeded max slope {maxSpawnSlope}°");
            }
            else
            {
                // No terrain or slope check disabled, accept immediately
                return spawnPos;
            }
        }

        // 🔥 ENHANCEMENT: Fallback to arena center if all attempts failed
        Vector3 centerPos = new Vector3(arena.center.x, GetGroundHeight(arena.center), arena.center.z);

        if (showDebugLogs)
            Debug.LogWarning($"FlagPickup: Using fallback center spawn at {centerPos}");

        return centerPos;
    }

    /// <summary>
    /// Get ground bounds with terrain-first priority and combined multi-object support
    /// </summary>
    private Bounds GetGroundBounds()
    {
        // 1. If terrain exists, use its world bounds
        if (Terrain.activeTerrain != null)
        {
            Terrain t = Terrain.activeTerrain;
            Bounds b = t.terrainData.bounds;
            b.center += t.transform.position;

            if (showDebugLogs)
                Debug.Log($"FlagPickup: Using Terrain bounds {b}");

            return b;
        }

        // 🔥 ENHANCEMENT: 2. Combine ALL Ground-tagged objects
        GameObject[] groundObjects = GameObject.FindGameObjectsWithTag("Ground");
        Collider[] groundColliders = groundObjects
            .Select(go => go.GetComponent<Collider>())
            .Where(col => col != null)
            .ToArray();

        if (groundColliders.Length > 0)
        {
            Bounds combinedBounds = groundColliders[0].bounds;

            for (int i = 1; i < groundColliders.Length; i++)
            {
                combinedBounds.Encapsulate(groundColliders[i].bounds);
            }

            if (showDebugLogs)
                Debug.Log($"FlagPickup: Combined {groundColliders.Length} Ground-tagged colliders, bounds: {combinedBounds}");

            return combinedBounds;
        }

        // 3. Final fallback
        if (showDebugLogs)
            Debug.LogWarning("FlagPickup: No terrain or Ground-tagged objects found, using fallback bounds");

        return new Bounds(Vector3.zero, new Vector3(10f, 1f, 10f));
    }

    /// <summary>
    /// Get ground height at world position with terrain-first sampling
    /// </summary>
    private float GetGroundHeight(Vector3 worldPoint)
    {
        // Terrain-first height sampling
        if (Terrain.activeTerrain != null)
        {
            Terrain t = Terrain.activeTerrain;
            return t.SampleHeight(worldPoint) + t.transform.position.y;
        }

        // 🔥 FIX: Use Ground layer mask for raycast
        if (Physics.Raycast(worldPoint, Vector3.down, out RaycastHit hit, 50f, LayerMask.GetMask("Ground")))
        {
            return hit.point.y;
        }

        // 🔥 ENHANCEMENT: Better fallback with warning
        if (showDebugLogs)
            Debug.LogWarning($"FlagPickup: No ground found at {worldPoint}, using fallback height 0");

        return 0f;
    }

    /// <summary>
    /// Get slope angle (in degrees) at world position on active terrain
    /// </summary>
    private float GetSlopeAtPosition(Vector3 worldPos)
    {
        if (Terrain.activeTerrain == null)
            return 0f;

        Terrain t = Terrain.activeTerrain;
        TerrainData data = t.terrainData;
        Vector3 terrainPos = t.transform.position;

        // Convert world position to normalized terrain coordinates (0-1)
        float normalizedX = (worldPos.x - terrainPos.x) / data.size.x;
        float normalizedZ = (worldPos.z - terrainPos.z) / data.size.z;

        // Clamp to terrain bounds
        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedZ = Mathf.Clamp01(normalizedZ);

        // Get steepness at this position
        return data.GetSteepness(normalizedX, normalizedZ);
    }

    // ============================================================
    // PUBLIC API
    // ============================================================

    /// <summary>
    /// Check if flag is held by specific transform
    /// </summary>
    public bool IsHeldBy(Transform t)
    {
        if (!isHeld || holder == null || t == null)
            return false;

        return holder == t || holder.IsChildOf(t);
    }

    /// <summary>
    /// Current holder transform (null if not held)
    /// </summary>
    public Transform CurrentHolder => holder;

    /// <summary>
    /// Is flag currently being held
    /// </summary>
    public bool IsHeld => isHeld;

    // ============================================================
    // DEBUG VISUALIZATION
    // ============================================================

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

        // Draw ground bounds
        if (groundBoundsCached)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(cachedGroundBounds.center, cachedGroundBounds.size);
        }

        // Draw padded spawn area
        if (groundBoundsCached)
        {
            float padX = cachedGroundBounds.extents.x * edgePaddingPercent;
            float padZ = cachedGroundBounds.extents.z * edgePaddingPercent;

            Vector3 paddedSize = new Vector3(
                cachedGroundBounds.size.x - (padX * 2f),
                cachedGroundBounds.size.y,
                cachedGroundBounds.size.z - (padZ * 2f)
            );

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(cachedGroundBounds.center, paddedSize);
        }

        // Draw attachment point if held
        if (isHeld && holder != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(holder.position, transform.position);
            Gizmos.DrawWireSphere(holder.position + Vector3.up * attachHeightOffset, 0.3f);
        }
    }
}