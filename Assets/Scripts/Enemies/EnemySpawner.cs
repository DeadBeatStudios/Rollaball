using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int spawnCount = 4;
    [SerializeField] private bool respawnOnDeath = true;
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private float spawnLift = 0.3f;

    [Header("Spawn Area (Random on Start)")]
    [SerializeField, Range(0.01f, 0.5f)]
    private float edgePaddingPercent = 0.05f;

    [Header("Terrain Spawn Validation")]
    [Tooltip("Maximum slope angle (degrees) for valid enemy spawn points. Set to 90 to disable.")]
    [SerializeField, Range(0f, 90f)] private float maxSpawnSlope = 30f;

    [Tooltip("Number of attempts to find a valid flat spawn location.")]
    [SerializeField] private int maxSpawnAttempts = 15;

    [Header("Layers")]
    [Tooltip("Ground layer(s) used for raycast fallback when no Terrain is present.")]
    [SerializeField] private LayerMask groundMask;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    // --- Internal Lists ---
    private readonly List<GameObject> activeEnemies = new List<GameObject>();
    private readonly List<Vector3> originalSpawnPositions = new List<Vector3>();

    // Cached bounds for performance
    private Bounds cachedGroundBounds;
    private bool groundBoundsCached = false;

    private void Reset()
    {
        // Automatically assign Ground layer
        groundMask = LayerMask.GetMask("Ground");
    }

    private void Start()
    {
        StartCoroutine(SpawnAfterDelay());
    }

    private IEnumerator SpawnAfterDelay()
    {
        // wait one frame so colliders and terrain initialize
        yield return null;

        CacheGroundBounds();
        SpawnInitialEnemies();
    }

    // ============================================================
    // SPAWNING
    // ============================================================

    private void SpawnInitialEnemies()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPos = GetRandomSpawn();
            originalSpawnPositions.Add(spawnPos);   // remember this spot for future respawns
            SpawnEnemyAt(spawnPos);
        }
    }

    private void SpawnEnemyAt(Vector3 position)
    {
        // Snap to ground and lift slightly
        Vector3 groundPos = SnapToGround(position);
        Vector3 spawnPos = groundPos + Vector3.up * spawnLift;

        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        activeEnemies.Add(enemy);

        if (showDebug)
            Debug.Log($"EnemySpawner: Spawned enemy at {spawnPos}");
    }

    public void HandleEnemyDeath(GameObject enemy)
    {
        if (!respawnOnDeath) return;

        int index = activeEnemies.IndexOf(enemy);

        if (index >= 0)
        {
            activeEnemies.RemoveAt(index);

            Vector3 respawnPosition = (index < originalSpawnPositions.Count)
                ? originalSpawnPositions[index]
                : GetRandomSpawn();

            // Destroy immediately to prevent multiple triggers
            Destroy(enemy);

            StartCoroutine(RespawnEnemyAfterDelay(respawnPosition, index));
        }
        else
        {
            // not tracked (e.g. manually spawned enemy)
            Destroy(enemy);
        }
    }

    private IEnumerator RespawnEnemyAfterDelay(Vector3 respawnPosition, int index)
    {
        yield return new WaitForSeconds(respawnDelay);

        Vector3 groundPos = SnapToGround(respawnPosition);
        Vector3 spawnPos = groundPos + Vector3.up * spawnLift;

        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        // Keep list slot consistent for future respawns
        if (index >= 0 && index <= activeEnemies.Count)
            activeEnemies.Insert(index, newEnemy);
        else
            activeEnemies.Add(newEnemy);

        if (showDebug)
            Debug.Log($"EnemySpawner: Respawned enemy at {spawnPos}");
    }

    // DEBUG: instant full respawn
    public void RespawnAllEnemiesDEBUG()
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }

        activeEnemies.Clear();
        originalSpawnPositions.Clear();

        CacheGroundBounds();
        SpawnInitialEnemies();

        Debug.Log("EnemySpawner DEBUG: Respawned all enemies.");
    }

    // ============================================================
    // TERRAIN-FIRST GROUND & SPAWN HELPERS
    // ============================================================

    private void CacheGroundBounds()
    {
        cachedGroundBounds = GetGroundBounds();
        groundBoundsCached = true;

        if (showDebug)
            Debug.Log($"EnemySpawner: Cached ground bounds {cachedGroundBounds}");
    }

    public void RefreshGroundBounds()
    {
        groundBoundsCached = false;
        CacheGroundBounds();

        if (showDebug)
            Debug.Log("EnemySpawner: Ground bounds refreshed.");
    }

    private Vector3 GetRandomSpawn()
    {
        Bounds arena = groundBoundsCached ? cachedGroundBounds : GetGroundBounds();

        float padX = arena.extents.x * edgePaddingPercent;
        float padZ = arena.extents.z * edgePaddingPercent;

        // Try multiple times to find a spot with acceptable slope
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            float rx = Random.Range(arena.min.x + padX, arena.max.x - padX);
            float rz = Random.Range(arena.min.z + padZ, arena.max.z - padZ);

            // Sample ground height at this XZ
            Vector3 testXZ = new Vector3(rx, arena.max.y + 2f, rz);
            float ry = GetGroundHeight(testXZ);
            Vector3 candidate = new Vector3(rx, ry, rz);

            // If we have terrain and slope checking enabled
            if (Terrain.activeTerrain != null && maxSpawnSlope < 90f)
            {
                float slope = GetSlopeAtPosition(candidate);

                if (slope <= maxSpawnSlope)
                {
                    if (showDebug && attempt > 0)
                        Debug.Log($"EnemySpawner: Found valid spawn after {attempt + 1} attempts (slope: {slope:F1}°) at {candidate}");

                    return candidate;
                }

                if (showDebug)
                    Debug.Log($"EnemySpawner: Rejected spawn (slope {slope:F1}° > {maxSpawnSlope}°) at {candidate}");
            }
            else
            {
                // No terrain or slope check disabled: accept
                return candidate;
            }
        }

        // Fallback: use center of arena
        Vector3 center = new Vector3(
            arena.center.x,
            GetGroundHeight(arena.center + Vector3.up * 2f),
            arena.center.z
        );

        if (showDebug)
            Debug.LogWarning($"EnemySpawner: Using fallback center spawn {center} after {maxSpawnAttempts} failed attempts.");

        return center;
    }

    private Vector3 SnapToGround(Vector3 worldPos)
    {
        float y = GetGroundHeight(worldPos + Vector3.up * 5f);
        return new Vector3(worldPos.x, y, worldPos.z);
    }

    /// <summary>
    /// Terrain-first bounds, then combined Ground-tagged colliders, then fallback.
    /// </summary>
    private Bounds GetGroundBounds()
    {
        // 1) Terrain first
        if (Terrain.activeTerrain != null)
        {
            Terrain t = Terrain.activeTerrain;
            Bounds b = t.terrainData.bounds;
            b.center += t.transform.position;

            if (showDebug)
                Debug.Log($"EnemySpawner: Using Terrain bounds {b}");

            return b;
        }

        // 2) Combine all Ground-tagged colliders
        GameObject[] groundObjects = GameObject.FindGameObjectsWithTag("Ground");
        Collider[] groundColliders = groundObjects
            .Select(go => go.GetComponent<Collider>())
            .Where(c => c != null)
            .ToArray();

        if (groundColliders.Length > 0)
        {
            Bounds combined = groundColliders[0].bounds;
            for (int i = 1; i < groundColliders.Length; i++)
                combined.Encapsulate(groundColliders[i].bounds);

            if (showDebug)
                Debug.Log($"EnemySpawner: Combined {groundColliders.Length} Ground colliders → {combined}");

            return combined;
        }

        // 3) Fallback
        if (showDebug)
            Debug.LogWarning("EnemySpawner: No Terrain or Ground-tagged colliders found. Using fallback 10x10 bounds at origin.");

        return new Bounds(Vector3.zero, new Vector3(10f, 1f, 10f));
    }

    private float GetGroundHeight(Vector3 worldPoint)
    {
        // Terrain-first
        if (Terrain.activeTerrain != null)
        {
            Terrain t = Terrain.activeTerrain;
            float h = t.SampleHeight(worldPoint) + t.transform.position.y;
            return h;
        }

        // Raycast fallback against groundMask
        if (Physics.Raycast(worldPoint, Vector3.down, out RaycastHit hit, 100f, groundMask))
        {
            return hit.point.y;
        }

        if (showDebug)
            Debug.LogWarning($"EnemySpawner: No ground found at {worldPoint}, using height 0.");

        return 0f;
    }

    private float GetSlopeAtPosition(Vector3 worldPos)
    {
        if (Terrain.activeTerrain == null)
            return 0f;

        Terrain t = Terrain.activeTerrain;
        TerrainData data = t.terrainData;
        Vector3 terrainPos = t.transform.position;

        float nx = (worldPos.x - terrainPos.x) / data.size.x;
        float nz = (worldPos.z - terrainPos.z) / data.size.z;

        nx = Mathf.Clamp01(nx);
        nz = Mathf.Clamp01(nz);

        return data.GetSteepness(nx, nz);
    }

    // ============================================================
    // GIZMOS
    // ============================================================

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !groundBoundsCached)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(cachedGroundBounds.center, cachedGroundBounds.size);

        float padX = cachedGroundBounds.extents.x * edgePaddingPercent;
        float padZ = cachedGroundBounds.extents.z * edgePaddingPercent;

        Vector3 paddedSize = new Vector3(
            cachedGroundBounds.size.x - padX * 2f,
            cachedGroundBounds.size.y,
            cachedGroundBounds.size.z - padZ * 2f
        );

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(cachedGroundBounds.center, paddedSize);
    }
}
