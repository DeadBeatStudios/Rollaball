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

    private Rigidbody rb;
    private Collider pickupCollider;


    // ======================================================
    // LIFECYCLE
    // ======================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pickupCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        initialWorldRotation = transform.rotation;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        CacheGroundBounds();
    }

    private void LateUpdate()
    {
        if (!isHeld || holder == null)
            return;

        if (HolderMissingOrDead())
        {
            RespawnAtRandom();
            return;
        }

        // Follow the holder
        transform.position = holder.position + Vector3.up * attachHeightOffset;
        transform.rotation = initialWorldRotation;

        // Auto drop if falling
        if (holder.position.y < autoDropY)
            RespawnAtRandom();
    }


    // ======================================================
    // PICKUP LOGIC
    // ======================================================

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") && !other.CompareTag("Enemy"))
            return;

        if (isHeld)
            return;

        // Always attach to the ROOT object, not VisualModel
        AttachToHolder(other.transform);
    }

    public void AttachToHolder(Transform newHolder)
    {
        holder = newHolder;
        isHeld = true;

        // Disable collider so the flag is no longer pickable
        if (pickupCollider != null)
            pickupCollider.enabled = false;

        // Stop physics
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Attach visually
        transform.SetParent(holder);
        transform.localPosition = Vector3.up * attachHeightOffset;
        transform.localRotation = Quaternion.identity;

        if (showDebugLogs)
            Debug.Log($"🏁 Flag attached to holder: {holder.name}");
    }


    // ======================================================
    // DROP + RESPAWN
    // ======================================================

    public void DropAndRespawn(
        FlagDropCause cause = FlagDropCause.Unknown,
        Transform killer = null,
        Vector3? deathPosition = null)
    {
        RespawnAtRandom();
    }

    private void RespawnAtRandom()
    {
        // Unparent from previous holder
        transform.SetParent(null);

        holder = null;
        isHeld = false;

        if (pickupCollider != null)
            pickupCollider.enabled = true;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Vector3 groundPos = GetRandomSpawn();
        transform.position = groundPos + Vector3.up * respawnHeightOffset;
        transform.rotation = initialWorldRotation;

        if (showDebugLogs)
            Debug.Log($"🏁 Flag respawned at {transform.position}");
    }


    // ======================================================
    // HOLDER VALIDATION
    // ======================================================

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


    // ======================================================
    // TERRAIN-BASED SPAWNING
    // ======================================================

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


    // ======================================================
    // PUBLIC API
    // ======================================================

    public bool IsHeldBy(Transform t)
    {
        return isHeld && holder != null && (holder == t || holder.IsChildOf(t));
    }

    public Transform CurrentHolder => holder;
    public bool IsHeld => isHeld;
}
