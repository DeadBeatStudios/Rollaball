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
            SpawnEnemyAt(spawnPos);
        }
    }

    private void SpawnEnemyAt(Vector3 position)
    {
        Vector3 finalPos = SnapToGroundIfNeeded(position) + Vector3.up * spawnLift;
        GameObject enemy = Instantiate(enemyPrefab, finalPos, Quaternion.identity);
        activeEnemies.Add(enemy);

        ApplyRoleAssignment(enemy);

        if (showDebug)
            Debug.Log($"EnemySpawner: Spawned enemy at {finalPos}");
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
        // Decoupled: EnemySpawner does NOT touch Flag logic.
        // Carrier death is handled by FlagDeathListener via DeathEvents.

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
            // Failsafe if enemy wasn't tracked correctly
            activeEnemies.Remove(enemy);
            Destroy(enemy);
        }
    }

    private IEnumerator RespawnEnemy(Vector3 spawnPos, int index)
    {
        yield return new WaitForSeconds(respawnDelay);

        Vector3 finalPos = SnapToGroundIfNeeded(spawnPos) + Vector3.up * spawnLift;
        GameObject enemy = Instantiate(enemyPrefab, finalPos, Quaternion.identity);

        ApplyRoleAssignment(enemy);

        if (index >= 0 && index <= activeEnemies.Count)
            activeEnemies.Insert(index, enemy);
        else
            activeEnemies.Add(enemy);

        if (showDebug)
            Debug.Log($"EnemySpawner: Respawned enemy at {finalPos}");
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
