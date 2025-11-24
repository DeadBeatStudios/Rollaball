using UnityEngine;

public class DebugControls : MonoBehaviour
{
    [Header("Player Debug References")]
    [SerializeField] private PlayerRespawn playerRespawn;
    [SerializeField] private ChunkExplosionSpawner playerExplosion;

    [Header("Enemy Debug Settings")]
    [SerializeField] private bool killAllEnemies = true;

    // ============================================================
    //  DEBUG ACTION METHODS (connected through Input Action Events)
    // ============================================================

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

    // ========= NEW DEBUG COMMANDS ========= //

    public void OnForceGameOver()
    {
        var goUI = SimpleGameOverUI.Instance;
        if (goUI == null)
        {
            Debug.LogError("DEBUG: SimpleGameOverUI.Instance is NULL — cannot force Game Over!");
            return;
        }

        Debug.Log("DEBUG: Forcing Game Over…");
        goUI.ShowGameOver();
    }

    public void OnForceFlagCapture()
    {
        FlagPickup flag = Object.FindAnyObjectByType<FlagPickup>();
        if (flag == null)
        {
            Debug.LogError("DEBUG: No FlagPickup found.");
            return;
        }

        if (playerRespawn == null)
        {
            Debug.LogError("DEBUG: No PlayerRespawn assigned.");
            return;
        }

        Transform player = playerRespawn.transform;

        // Attach flag to player exactly as game logic expects
        flag.AttachToHolder(player);

        Debug.Log("DEBUG: Forced flag capture by player.");
    }

    // ============================================================
    //  INTERNAL IMPLEMENTATIONS
    // ============================================================

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
        if (spawner == null)
        {
            Debug.LogError("DEBUG: No EnemySpawner found.");
            return;
        }

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
