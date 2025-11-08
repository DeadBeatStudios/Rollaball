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

    [Header("Spawn Area")]
    [SerializeField, Range(0.01f, 0.5f)]
    private float edgePaddingPercent = 0.05f; // same logic as FlagPickup

    [Header("Target Settings")]
    [SerializeField] private Transform playerTarget;

    private List<GameObject> activeEnemies = new List<GameObject>();

    private void Start()
    {
        SpawnInitialEnemies();
    }

    private void SpawnInitialEnemies()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: Missing enemy prefab!");
            return;
        }

        Vector3 spawnPos = GetRandomSpawn();
        GameObject enemy = Instantiate(enemyPrefab, spawnPos + Vector3.up * spawnLift, Quaternion.identity);

        // Automatically assign player target
        if (enemy.TryGetComponent(out EnemyPhysicsController enemyAI))
        {
            enemyAI.target = playerTarget;
        }

        activeEnemies.Add(enemy);
    }

    public void HandleEnemyDeath(GameObject enemy)
    {
        if (!respawnOnDeath) return;

        StartCoroutine(RespawnEnemyAfterDelay(enemy));
    }

    private IEnumerator RespawnEnemyAfterDelay(GameObject oldEnemy)
    {
        yield return new WaitForSeconds(respawnDelay);

        if (oldEnemy != null)
        {
            activeEnemies.Remove(oldEnemy);
            Destroy(oldEnemy);
        }

        SpawnEnemy();
    }

    // -------------------------------
    //   RANDOM SPAWN LOGIC (same as FlagPickup)
    // -------------------------------
    private Vector3 GetRandomSpawn()
    {
        Bounds floorBounds = GetPhysicsFloorBounds();
        float padX = floorBounds.extents.x * edgePaddingPercent;
        float padZ = floorBounds.extents.z * edgePaddingPercent;

        float rx = Random.Range(floorBounds.min.x + padX, floorBounds.max.x - padX);
        float rz = Random.Range(floorBounds.min.z + padZ, floorBounds.max.z - padZ);
        float rayStartY = floorBounds.max.y + 5f;

        if (Physics.Raycast(new Vector3(rx, rayStartY, rz), Vector3.down, out RaycastHit hit, 10f, LayerMask.GetMask("Ground")))
            return hit.point;

        return new Vector3(rx, floorBounds.max.y, rz);
    }

    private Bounds GetPhysicsFloorBounds()
    {
        GameObject floor = GameObject.Find("PhysicsFloor");
        if (floor != null && floor.TryGetComponent(out BoxCollider col))
            return col.bounds;

        Debug.LogWarning("PhysicsFloor not found — using fallback bounds.");
        return new Bounds(Vector3.zero, new Vector3(10f, 1f, 8f));
    }
}
