using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    // ============================================================
    //  ENEMY SETTINGS
    // ============================================================
    [Header("Enemy Settings")]
    [SerializeField] private GameObject enemyPrefab;
    public GameObject EnemyPrefab => enemyPrefab;

    [SerializeField] private int spawnCount = 4;

    [Tooltip("If true, enemies will respawn after dying.")]
    [SerializeField] private bool respawnOnDeath = true;

    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private float spawnLift = 0.3f;

    // ============================================================
    //  STABLE IDENTITY (NEW)
    // ============================================================
    [Header("Stable Enemy Identity")]
    [Tooltip("Base ID for enemies. Slot index is added to this (ex: 2000 + slotIndex).")]
    [SerializeField] private int enemyIdBase = 2000;

    [Tooltip("Name pool used once at match start to assign stable names per slot.")]
    [SerializeField]
    private List<string> enemyNamePool = new List<string>
    {
        "Adrian", "Ethan", "Benjamin", "Corey", "Dennis",
        "Connie", "Miranda", "Zoe", "Christy", "Mike"
    };

    // slot -> stable name
    private string[] slotNames;

    // ============================================================
    //  AI ROLE ASSIGNMENT
    // ============================================================
    [Header("AI Role Assignment")]
    [SerializeField]
    private EnemyAIController.AIRole[] allowedRoles =
    {
        EnemyAIController.AIRole.BasicChaser,
        EnemyAIController.AIRole.Defender,
        EnemyAIController.AIRole.FlagChaser
    };

    [SerializeField] private Transform[] guardPoints;
    [SerializeField] private Transform[] resetPoints;
    [SerializeField] private bool randomizeRoles = true;

    // ============================================================
    //  MANUAL SPAWN AREA
    // ============================================================
    [Header("Manual Spawn Box (Center + Size)")]
    [SerializeField] private Transform spawnCenter;
    [SerializeField] private Vector3 boxSize = new Vector3(20f, 5f, 20f);
    [SerializeField] private bool randomizeY = false;

    // ============================================================
    //  GROUND SNAP & SLOPE VALIDATION
    // ============================================================
    [Header("Ground & Slope")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField, Range(0f, 90f)] private float maxSpawnSlope = 90f;
    [SerializeField] private int maxSpawnAttempts = 10;

    // ============================================================
    //  DEBUG
    // ============================================================
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    private readonly List<GameObject> activeEnemies = new();
    private readonly List<Vector3> originalSpawnPositions = new();

    // ============================================================
    //  STARTUP
    // ============================================================
    private void Start()
    {
        // Prepare stable names for each slot (once per match).
        BuildStableSlotNames();

        StartCoroutine(DelayedSpawn());
    }

    private IEnumerator DelayedSpawn()
    {
        yield return null;
        SpawnInitialEnemies();
    }

    // ============================================================
    //  INITIAL SPAWNING
    // ============================================================
    private void SpawnInitialEnemies()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPos = GetBoxSpawnPoint();
            originalSpawnPositions.Add(spawnPos);
            SpawnEnemyAt(spawnPos, i);
        }
    }

    private void SpawnEnemyAt(Vector3 position, int slotIndex)
    {
        Vector3 finalPos = SnapToGroundIfNeeded(position) + Vector3.up * spawnLift;
        GameObject enemy = Instantiate(enemyPrefab, finalPos, Quaternion.identity);

        // Track in slot order
        activeEnemies.Add(enemy);

        // Apply stable identity BEFORE enemy Start() runs
        ApplyStableIdentity(enemy, slotIndex);

        ApplyRoleAssignment(enemy);

        if (showDebug)
            Debug.Log($"EnemySpawner: Spawned {enemy.name} at {finalPos}");
    }

    // ============================================================
    //  STABLE IDENTITY HELPERS
    // ============================================================
    private void BuildStableSlotNames()
    {
        if (spawnCount <= 0)
            spawnCount = 1;

        slotNames = new string[spawnCount];

        // Copy pool so we can pick without duplicates until exhausted
        List<string> pool = new List<string>(enemyNamePool);

        for (int i = 0; i < spawnCount; i++)
        {
            string chosen;

            if (pool.Count > 0)
            {
                int pick = Random.Range(0, pool.Count);
                chosen = pool[pick];
                pool.RemoveAt(pick);
            }
            else
            {
                // Pool exhausted: reuse base names with a suffix
                string baseName = enemyNamePool.Count > 0 ? enemyNamePool[Random.Range(0, enemyNamePool.Count)] : "Enemy";
                chosen = $"{baseName}_{i + 1}";
            }

            slotNames[i] = chosen;
        }
    }

    private void ApplyStableIdentity(GameObject enemy, int slotIndex)
    {
        if (enemy == null) return;

        int stableId = enemyIdBase + Mathf.Max(0, slotIndex);
        string stableName = (slotNames != null && slotIndex >= 0 && slotIndex < slotNames.Length)
            ? slotNames[slotIndex]
            : $"Enemy_{slotIndex + 1:00}";

        // Rename object (debug clarity)
        enemy.name = stableName;

        // Push identity into PlayerScore so GameManager keys are stable
        if (enemy.TryGetComponent<PlayerScore>(out var score))
        {
            score.SetIdentity(stableId, stableName);
        }
        else if (showDebug)
        {
            Debug.LogWarning($"EnemySpawner: {enemy.name} has no PlayerScore component. Stable ID/name not applied.");
        }
    }

    // ============================================================
    //  SPAWN POSITIONING
    // ============================================================
    private Vector3 GetBoxSpawnPoint()
    {
        Transform center = spawnCenter != null ? spawnCenter : transform;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            float x = Random.Range(-boxSize.x * 0.5f, boxSize.x * 0.5f);
            float z = Random.Range(-boxSize.z * 0.5f, boxSize.z * 0.5f);
            float y = randomizeY
                ? Random.Range(-boxSize.y * 0.5f, boxSize.y * 0.5f)
                : 5f;

            Vector3 candidate = center.position + new Vector3(x, y, z);

            if (maxSpawnSlope < 90f && IsSlopeTooSteep(candidate))
                continue;

            return candidate;
        }

        return spawnCenter != null ? spawnCenter.position : transform.position;
    }

    private bool IsSlopeTooSteep(Vector3 worldPos)
    {
        if (!Physics.Raycast(worldPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, groundMask))
            return false;

        float slope = Vector3.Angle(hit.normal, Vector3.up);
        return slope > maxSpawnSlope;
    }

    private Vector3 SnapToGroundIfNeeded(Vector3 pos)
    {
        if (randomizeY)
            return pos;

        if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 50f, groundMask))
            return new Vector3(pos.x, hit.point.y, pos.z);

        return pos;
    }

    // ============================================================
    //  RESPAWN LOGIC (CALLED BY EnemyDeathListener)
    // ============================================================
    public void HandleEnemyDeath(GameObject enemy)
    {
        if (enemy == null)
            return;

        int index = activeEnemies.IndexOf(enemy);

        if (!respawnOnDeath)
        {
            if (index >= 0)
                activeEnemies.RemoveAt(index);
            else
                activeEnemies.Remove(enemy);

            Destroy(enemy);
            return;
        }

        if (index >= 0)
        {
            activeEnemies.RemoveAt(index);
            Destroy(enemy);

            Vector3 respawnPoint =
                index < originalSpawnPositions.Count
                    ? originalSpawnPositions[index]
                    : GetBoxSpawnPoint();

            StartCoroutine(RespawnEnemy(respawnPoint, index));
        }
        else
        {
            // Failsafe
            activeEnemies.Remove(enemy);
            Destroy(enemy);
        }
    }

    private IEnumerator RespawnEnemy(Vector3 spawnPos, int slotIndex)
    {
        yield return new WaitForSeconds(respawnDelay);

        Vector3 finalPos = SnapToGroundIfNeeded(spawnPos) + Vector3.up * spawnLift;
        GameObject enemy = Instantiate(enemyPrefab, finalPos, Quaternion.identity);

        // Apply stable identity BEFORE Start() on the clone runs
        ApplyStableIdentity(enemy, slotIndex);

        ApplyRoleAssignment(enemy);

        if (slotIndex >= 0 && slotIndex <= activeEnemies.Count)
            activeEnemies.Insert(slotIndex, enemy);
        else
            activeEnemies.Add(enemy);

        if (showDebug)
            Debug.Log($"EnemySpawner: Respawned {enemy.name} at {finalPos}");
    }

    // ============================================================
    //  AI ROLE ASSIGNMENT
    // ============================================================
    private void ApplyRoleAssignment(GameObject enemy)
    {
        EnemyAIController ai = enemy.GetComponent<EnemyAIController>();
        if (ai == null)
            return;

        if (randomizeRoles && allowedRoles.Length > 0)
            ai.role = allowedRoles[Random.Range(0, allowedRoles.Length)];

        if (ai.role == EnemyAIController.AIRole.Defender && guardPoints.Length > 0)
            ai.guardPoint = guardPoints[Random.Range(0, guardPoints.Length)];

        if (resetPoints.Length > 0)
            ai.resetPoint = resetPoints[Random.Range(0, resetPoints.Length)];

        if (showDebug)
            Debug.Log($"EnemySpawner: {enemy.name} assigned role → {ai.role}");
    }

    // ============================================================
    //  GIZMOS
    // ============================================================
    private void OnDrawGizmosSelected()
    {
        Transform center = spawnCenter != null ? spawnCenter : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center.position, boxSize);
    }
}
