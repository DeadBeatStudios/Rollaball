using UnityEngine;

/// <summary>
/// Listens for DeathEvents and routes enemy-specific death handling
/// to the EnemySpawner (respawn) without detectors calling the spawner directly.
/// </summary>
public class EnemyDeathListener : MonoBehaviour
{
    [Header("Optional Overrides")]
    [Tooltip("If left empty, the listener will auto-find an EnemySpawner in the scene.")]
    [SerializeField] private EnemySpawner spawner;

    [Tooltip("Optional: If assigned, plays death VFX here when this enemy dies. " +
             "If you already play VFX in the detector (ex: EnemyFallDetector), leave this null.")]
    [SerializeField] private ChunkExplosionSpawner explosionSpawner;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private void Awake()
    {
        if (spawner == null)
            spawner = FindAnyObjectByType<EnemySpawner>();
    }

    private void OnEnable()
    {
        DeathEvents.OnDeath += HandleDeathEvent;
    }

    private void OnDisable()
    {
        DeathEvents.OnDeath -= HandleDeathEvent;
    }

    private void HandleDeathEvent(
        GameObject victim,
        DeathEvents.DeathCause cause,
        GameObject instigator)
    {
        // Only handle deaths for THIS enemy
        if (victim != gameObject)
            return;

        // Optional local VFX (keep null if handled elsewhere)
        if (explosionSpawner != null)
            explosionSpawner.SpawnChunkExplosion();

        // Notify spawner (respawn logic stays centralized there)
        if (spawner != null)
        {
            spawner.HandleEnemyDeath(gameObject);
        }
        else if (debugLogs)
        {
            Debug.LogWarning($"{name}: EnemyDeathListener has no EnemySpawner reference.");
        }

        if (debugLogs)
            Debug.Log($"{name}: Death handled (cause: {cause})");
    }
}
