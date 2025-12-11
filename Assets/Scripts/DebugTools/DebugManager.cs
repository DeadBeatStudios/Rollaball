using UnityEngine;

public class DebugManager : MonoBehaviour
{
    [Header("References")]
    public GameObject enemyPrefab;
    public EnemySpawner spawner;
    public Transform spawnPoint;
    public FlagPickup flag;
    public Transform player;

    private bool aiFrozen = false;

    private void Awake()
    {
        if (spawner == null)
            spawner = FindAnyObjectByType<EnemySpawner>();

        if (flag == null)
            flag = FindAnyObjectByType<FlagPickup>();

        if (player == null)
        {
            var p = FindAnyObjectByType<PlayerController>();
            if (p != null) player = p.transform;
        }

        if (spawnPoint == null)
        {
            GameObject go = new GameObject("DebugSpawnPoint");
            go.transform.position = Vector3.zero + Vector3.up;
            spawnPoint = go.transform;
        }
        
        if (enemyPrefab == null && spawner != null)
            enemyPrefab = spawner.EnemyPrefab;
    }

    // ---------------------------------------------------------------------------------
    //  PUBLIC METHODS CALLED BY UNITY EVENTS
    // ---------------------------------------------------------------------------------
    public void SpawnChaser() => SpawnRole(EnemyAIController.AIRole.BasicChaser);
    public void SpawnDefender() => SpawnRole(EnemyAIController.AIRole.Defender);
    public void SpawnFlagChaser() => SpawnRole(EnemyAIController.AIRole.FlagChaser);

    private void SpawnRole(EnemyAIController.AIRole role)
    {
        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
        var ai = enemy.GetComponent<EnemyAIController>();

        ai.role = role;
        if (role == EnemyAIController.AIRole.Defender)
            ai.guardPoint = ai.goalTarget;

        Debug.Log($"DEBUG: Spawned {role}");
    }

    public void ResetEnemies()
    {
        var enemies = FindObjectsByType<EnemyAIController>(FindObjectsSortMode.None);

        foreach (var ai in enemies)
            Destroy(ai.gameObject);

        Debug.Log("DEBUG: Enemies Reset");
    }

    public void ResetFlag()
    {
        if (flag != null)
        {
            flag.ForceResetFlag();
            Debug.Log("DEBUG: Flag Reset");
        }
    }

    public void TeleportPlayer()
    {
        if (player != null)
        {
            player.position = Vector3.zero + Vector3.up;
            Debug.Log("DEBUG: Player Teleported");
        }
    }

    public void ToggleAI()
    {
        aiFrozen = !aiFrozen;

        var allAI = FindObjectsByType<EnemyAIController>(FindObjectsSortMode.None);

        foreach (var ai in allAI)
            ai.enabled = !aiFrozen;

        Debug.Log($"DEBUG: AI {(aiFrozen ? "FROZEN" : "ACTIVE")}");
    }
}
