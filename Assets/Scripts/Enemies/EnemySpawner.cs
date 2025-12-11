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

    [Tooltip("List of allowed enemy roles. A random one will be chosen for each spawn.")]
    [SerializeField]
    private EnemyAIController.AIRole[] allowedRoles =
    {
        EnemyAIController.AIRole.BasicChaser,
        EnemyAIController.AIRole.Defender,
        EnemyAIController.AIRole.FlagChaser
    };

    [Tooltip("Guard points used ONLY for Defender role.")]
    [SerializeField] private Transform[] guardPoints;

    [Tooltip("Reset points for enemy recovery logic.")]
    [SerializeField] private Transform[] resetPoints;

    [Tooltip("If true, roles are assigned randomly from AllowedRoles.")]
    [SerializeField] private bool randomizeRoles = true;

    // ============================================================
    //  MANUAL SPAWN AREA
    // ============================================================
    [Header("Manual Spawn Box (Center + Size)")]
    [Tooltip("Optional: Assign a dedicated center transform. If left empty, the spawner's transform is used.")]
    [SerializeField] private Transform spawnCenter;

    [Tooltip("Size of the spawn box (X/Z used for horizontal area). Y is only used if RandomizeY = true.")]
    [SerializeField] private Vector3 boxSize = new Vector3(20f, 5f, 20f);

    [Tooltip("If enabled, Y coordinate is a random value within the box height instead of snapping to ground.")]
    [SerializeField] private bool randomizeY = false;

    // ============================================================
    //  GROUND SNAP & SLOPE VALIDATION
    // ============================================================
    [Header("Ground & Slope")]
    [Tooltip("Ground layers used for snapping downward.")]
    [SerializeField] private LayerMask groundMask;

    [Tooltip("Maximum allowed slope angle. Set to 90 to disable slope checks.")]
    [SerializeField, Range(0f, 90f)] private float maxSpawnSlope = 90f;

    [Tooltip("Number of attempts to find a valid ground position/slope.")]
    [SerializeField] private int maxSpawnAttempts = 10;

    // ============================================================
    //  DEBUG
    // ============================================================
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    private readonly List<GameObject> activeEnemies = new List<GameObject>();
    private readonly List<Vector3> originalSpawnPositions = new List<Vector3>();


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

        // NEW: Assign AI role + guard / reset points + color coding
        ApplyRoleAssignment(enemy);

        if (showDebug)
            Debug.Log($"EnemySpawner: Spawned enemy at {finalPos}");
    }

    // ============================================================
    //  RANDOM BOX SPAWN LOGIC
    // ============================================================
    private Vector3 GetBoxSpawnPoint()
    {
        Transform center = spawnCenter != null ? spawnCenter : transform;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            float x = Random.Range(-boxSize.x * 0.5f, boxSize.x * 0.5f);
            float z = Random.Range(-boxSize.z * 0.5f, boxSize.z * 0.5f);

            float y;

            if (randomizeY)
                y = Random.Range(-boxSize.y * 0.5f, boxSize.y * 0.5f);
            else
                y = 5f;

            Vector3 candidate = center.position + new Vector3(x, y, z);

            // Slope validation
            if (maxSpawnSlope < 90f && IsSlopeTooSteep(candidate))
            {
                if (showDebug)
                    Debug.Log($"Rejected steep spawn at {candidate}");
                continue;
            }

            return candidate;
        }

        if (showDebug)
            Debug.LogWarning("EnemySpawner: Failed to find valid spawn. Using center of box.");

        return spawnCenter != null ? spawnCenter.position : transform.position;
    }

    private bool IsSlopeTooSteep(Vector3 worldPos)
    {
        if (!Physics.Raycast(worldPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, groundMask))
            return false;

        float slope = Vector3.Angle(hit.normal, Vector3.up);
        return slope > maxSpawnSlope;
    }

    // ============================================================
    //  GROUND SNAP
    // ============================================================
    private Vector3 SnapToGroundIfNeeded(Vector3 pos)
    {
        if (randomizeY)
            return pos;

        if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 50f, groundMask))
            return new Vector3(pos.x, hit.point.y, pos.z);

        return pos;
    }

    // ============================================================
    //  RESPAWN LOGIC
    // ============================================================
    public void HandleEnemyDeath(GameObject enemy)
    {
        if (!respawnOnDeath)
        {
            Destroy(enemy);
            return;
        }

        int index = activeEnemies.IndexOf(enemy);

        if (index >= 0)
        {
            activeEnemies.RemoveAt(index);
            Destroy(enemy);

            Vector3 respawnPoint =
                index < originalSpawnPositions.Count ?
                originalSpawnPositions[index] :
                GetBoxSpawnPoint();

            StartCoroutine(RespawnEnemy(respawnPoint, index));
        }
        else
        {
            Destroy(enemy);
        }
    }

    private IEnumerator RespawnEnemy(Vector3 spawnPos, int index)
    {
        yield return new WaitForSeconds(respawnDelay);

        Vector3 finalPos = SnapToGroundIfNeeded(spawnPos) + Vector3.up * spawnLift;

        GameObject newEnemy = Instantiate(enemyPrefab, finalPos, Quaternion.identity);

        // NEW: Role assignment on respawn
        ApplyRoleAssignment(newEnemy);

        if (index >= 0 && index <= activeEnemies.Count)
            activeEnemies.Insert(index, newEnemy);
        else
            activeEnemies.Add(newEnemy);

        if (showDebug)
            Debug.Log($"EnemySpawner: Respawned at {finalPos}");
    }

    public void RespawnAllEnemiesDEBUG()
    {
        foreach (var e in activeEnemies)
        {
            if (e != null)
                Destroy(e);
        }

        activeEnemies.Clear();
        originalSpawnPositions.Clear();

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPos = GetBoxSpawnPoint();
            originalSpawnPositions.Add(spawnPos);
            SpawnEnemyAt(spawnPos);
        }

        if (showDebug)
            Debug.Log("EnemySpawner DEBUG: Respawned all enemies.");
    }

    // ============================================================
    //  AI ROLE LOGIC (NEW SECTION)
    // ============================================================
    private void ApplyRoleAssignment(GameObject enemy)
    {
        EnemyAIController ai = enemy.GetComponent<EnemyAIController>();
        if (ai == null)
            return;

        // Assign a random role if enabled
        if (randomizeRoles && allowedRoles.Length > 0)
        {
            ai.role = allowedRoles[Random.Range(0, allowedRoles.Length)];
        }

        // DEFENDER → assign guard point
        if (ai.role == EnemyAIController.AIRole.Defender && guardPoints.Length > 0)
        {
            ai.guardPoint = guardPoints[Random.Range(0, guardPoints.Length)];
        }

        // Assign reset point for all enemies
        if (resetPoints.Length > 0)
        {
            ai.resetPoint = resetPoints[Random.Range(0, resetPoints.Length)];
        }

        // COLOR CODE BY ROLE (Debug only)
        MeshRenderer renderer = enemy.GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
        {
            switch (ai.role)
            {
                case EnemyAIController.AIRole.BasicChaser:
                    renderer.material.color = Color.blue;
                    break;

                case EnemyAIController.AIRole.Defender:
                    renderer.material.color = Color.red;
                    break;

                case EnemyAIController.AIRole.FlagChaser:
                    renderer.material.color = Color.green;
                    break;
            }
        }

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
