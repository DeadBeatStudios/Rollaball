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
    // by PlayerInput via events)
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

    public void OnForceGameOver()
    {
        if (SimpleGameOverUI.Instance != null)
        {
            SimpleGameOverUI.Instance.ShowGameOver();
            Debug.Log("DEBUG: Forced Game Over screen.");
        }
        else
        {
            Debug.LogWarning("DEBUG: SimpleGameOverUI.Instance is null – cannot force Game Over.");
        }
    }

    public void OnForceFlagCapture()
    {
        ForceFlagCapture();
    }

    // =============================
    // Implementation
    // =============================

    private void KillAllEnemies()
    {
        EnemyFallDetector[] enemies =
            Object.FindObjectsByType<EnemyFallDetector>(FindObjectsSortMode.None);

        foreach (var e in enemies)
        {
            if (e != null)
                e.ForceKillDEBUG();
        }

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
            if (enemy == null) continue;

            float d = Vector3.Distance(p, enemy.transform.position);
            if (d < shortest)
            {
                shortest = d;
                nearest = enemy;
            }
        }

        if (nearest != null)
        {
            nearest.ForceKillDEBUG();
            Debug.Log($"DEBUG: Killed nearest enemy: {nearest.name}");
        }
    }

    private void RespawnAllEnemies()
    {
        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("DEBUG: No EnemySpawner found for RespawnAllEnemiesDEBUG.");
            return;
        }

        spawner.RespawnAllEnemiesDEBUG();
        Debug.Log("DEBUG: Respawned all enemies.");
    }

    private void ForceFlagDrop()
    {
        FlagPickup flag = Object.FindAnyObjectByType<FlagPickup>();
        if (flag == null)
        {
            Debug.LogWarning("DEBUG: No FlagPickup found for ForceFlagDrop.");
            return;
        }

        if (!flag.IsHeld)
        {
            Debug.Log("DEBUG: Flag is not currently held. Nothing to drop.");
            return;
        }

        Transform holder = flag.CurrentHolder;
        Vector3 dropPos = holder != null ? holder.position : flag.transform.position;

        // ✅ Use DropToWorld (3 args) — does NOT random respawn
        flag.DropToWorld(
            FlagPickup.FlagDropCause.SelfDestruct,
            holder,
            dropPos
        );

        Debug.Log($"DEBUG: Forced flag drop to world at {dropPos}.");
    }

    private void ForceFlagCapture()
    {
        FlagPickup flag = Object.FindAnyObjectByType<FlagPickup>();
        if (flag == null)
        {
            Debug.LogWarning("DEBUG: No FlagPickup found for ForceFlagCapture.");
            return;
        }

        if (playerRespawn == null)
        {
            Debug.LogWarning("DEBUG: No PlayerRespawn assigned for ForceFlagCapture.");
            return;
        }

        flag.AttachToHolder(playerRespawn.transform);
        Debug.Log("DEBUG: Forced flag attachment to PLAYER.");
    }
}
