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
        if (!other.CompareTag("Player") && !other.CompareTag("Enemy"))
            return;

        if (isHeld) return;

        // 🔥 FIX: ALWAYS USE ROOT — NEVER VisualModel
        holder = other.transform;

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

    // --------------------------
    // TERRAIN-FIRST RESPAWNING
    // --------------------------

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
            else return spawn;
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
            .Where(c => c != null).ToArray();

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
