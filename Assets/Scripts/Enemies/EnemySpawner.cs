using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    ///[Header("Target")]
    ///[SerializeField] private Transform playerTarget;

    [Header("Layers")]
    [SerializeField] private LayerMask groundMask;

    // --- Internal Lists ---
    private List<GameObject> activeEnemies = new List<GameObject>();
    private List<Vector3> originalSpawnPositions = new List<Vector3>();

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
        yield return null; // wait one frame so colliders and physics init
        SpawnInitialEnemies();
    }

    private void SpawnInitialEnemies()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPos = GetRandomSpawn();
            originalSpawnPositions.Add(spawnPos); // remember this spot for future respawns
            SpawnEnemyAt(spawnPos);
        }
    }

    private void SpawnEnemyAt(Vector3 position)
    {
        Vector3 spawnPos = SnapToGround(position) + Vector3.up * spawnLift;
        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        //if (enemy.TryGetComponent(out EnemyPhysicsController enemyAI) && playerTarget)
           // enemyAI.target = playerTarget;

        activeEnemies.Add(enemy);
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
    }

    private IEnumerator RespawnEnemyAfterDelay(Vector3 respawnPosition, int index)
    {
        yield return new WaitForSeconds(respawnDelay);

        Vector3 spawnPos = SnapToGround(respawnPosition) + Vector3.up * spawnLift;
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        //if (newEnemy.TryGetComponent(out EnemyPhysicsController enemyAI) && playerTarget)
            //enemyAI.target = playerTarget;

        activeEnemies.Insert(index, newEnemy); // reassign same slot for future respawns
    }

    // --- Spawn helpers ---
    private Vector3 GetRandomSpawn()
    {
        Bounds floorBounds = GetPhysicsFloorBounds();
        float padX = floorBounds.extents.x * edgePaddingPercent;
        float padZ = floorBounds.extents.z * edgePaddingPercent;

        float rx = Random.Range(floorBounds.min.x + padX, floorBounds.max.x - padX);
        float rz = Random.Range(floorBounds.min.z + padZ, floorBounds.max.z - padZ);
        float rayStartY = floorBounds.max.y + 5f;

        Vector3 rayStart = new Vector3(rx, rayStartY, rz);
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 12f, groundMask))
            return hit.point;

        return new Vector3(rx, floorBounds.max.y, rz);
    }

    private Vector3 SnapToGround(Vector3 worldPos)
    {
        Vector3 start = worldPos + Vector3.up * 3f;
        if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, 10f, groundMask))
        {
            Debug.DrawLine(start, hit.point, Color.green, 2f);
            return hit.point;
        }

        Debug.DrawLine(start, start + Vector3.down * 2f, Color.yellow, 2f);
        return worldPos;
    }

    private Bounds GetPhysicsFloorBounds()
    {
        GameObject floor = GameObject.Find("PhysicsFloor");
        if (floor && floor.TryGetComponent(out BoxCollider col))
            return col.bounds;

        Debug.LogWarning("EnemySpawner: PhysicsFloor not found — using fallback bounds.");
        return new Bounds(Vector3.zero, new Vector3(10f, 1f, 8f));
    }
}
