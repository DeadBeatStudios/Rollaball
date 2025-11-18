using UnityEngine;

public class DebugControls : MonoBehaviour
{
    [Header("Player Debug References")]
    [SerializeField] private PlayerRespawn playerRespawn;
    [SerializeField] private ChunkExplosionSpawner playerExplosion;

    [Header("Enemy Debug Settings")]
    [SerializeField] private bool killAllEnemies = true;

    // =============================
    // Input System Event Handlers
    // (These methods are called
    // by PlayerInput automatically)
    // =============================

    public void OnKillPlayer()
    {
        if (playerExplosion != null)
            playerExplosion.SpawnChunkExplosion();

        if (playerRespawn != null)
            playerRespawn.HandleDeath(FlagPickup.FlagDropCause.SelfDestruct);

        Debug.Log("DEBUG: Forced PLAYER death.");
    }

    public void OnKillEnemies()
    {
        if (killAllEnemies)
            KillAllEnemies();
        else
            KillNearestEnemy();
    }

    public void OnForceFlagDrop()
    {
        ForceFlagDrop();
    }

    public void OnTeleportPlayer()
    {
        if (playerRespawn == null) return;

        playerRespawn.transform.position = new Vector3(0, 2, 0);
        Debug.Log("DEBUG: Teleported player.");
    }

    public void OnRespawnEnemies()
    {
        RespawnAllEnemies();
    }

    // =============================
    // Implementation
    // =============================

    private void KillAllEnemies()
    {
        EnemyFallDetector[] enemies =
            Object.FindObjectsByType<EnemyFallDetector>(FindObjectsSortMode.None);

        foreach (var e in enemies)
            e.ForceKillDEBUG();

        Debug.Log("DEBUG: Killed ALL enemies.");
    }

    private void KillNearestEnemy()
    {
        EnemyFallDetector[] enemies =
            Object.FindObjectsByType<EnemyFallDetector>(FindObjectsSortMode.None);

        if (enemies.Length == 0 || playerRespawn == null)
            return;

        float shortest = Mathf.Infinity;
        EnemyFallDetector nearest = null;
        Vector3 p = playerRespawn.transform.position;

        foreach (var enemy in enemies)
        {
            float d = Vector3.Distance(p, enemy.transform.position);
            if (d < shortest)
            {
                shortest = d;
                nearest = enemy;
            }
        }

        if (nearest != null)
            nearest.ForceKillDEBUG();
    }

    private void RespawnAllEnemies()
    {
        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        if (spawner == null) return;

        spawner.RespawnAllEnemiesDEBUG();
        Debug.Log("DEBUG: Respawned all enemies.");
    }

    private void ForceFlagDrop()
    {
        FlagPickup flag = Object.FindAnyObjectByType<FlagPickup>();
        if (flag == null) return;

        if (flag.IsHeld)
        {
            flag.DropAndRespawn(
                FlagPickup.FlagDropCause.SelfDestruct,
                null,
                flag.transform.position
            );
        }

        Debug.Log("DEBUG: Forced flag drop.");
    }
}
