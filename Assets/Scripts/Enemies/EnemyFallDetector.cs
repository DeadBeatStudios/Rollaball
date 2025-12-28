using UnityEngine;

public class EnemyFallDetector : MonoBehaviour
{
    [SerializeField] private float fallThresholdY = -10f;

    private bool hasTriggered = false;

    [Header("Death Effects")]
    [SerializeField] private ChunkExplosionSpawner explosionSpawner;

    private void Update()
    {
        if (!hasTriggered && transform.position.y < fallThresholdY)
        {
            HandleDeath();
        }
    }

    public void ForceKillDEBUG()
    {
        if (hasTriggered) return;
        HandleDeath();
    }

    public void ForceKill()
    {
        if (hasTriggered) return;
        HandleDeath();
    }

    // --------------------------------------------------------------
    //  DEATH HANDLING
    // --------------------------------------------------------------
    private void HandleDeath()
    {
        hasTriggered = true;

        // Visuals first
        if (explosionSpawner != null)
            explosionSpawner.SpawnChunkExplosion();

        // Report death (authoritative)
        DeathEvents.ReportDeath(
            gameObject,
            DeathEvents.DeathCause.FellOffMap,
            null
        );

        // Disable enemy (no spawner calls here)
        gameObject.SetActive(false);
    }
}
