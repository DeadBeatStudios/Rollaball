using UnityEngine;

public class EnemyFallDetector : MonoBehaviour
{
    [SerializeField] private float fallThresholdY = -10f;

    [Header("Spawner Reference")]
    [Tooltip("Assign the EnemySpawner in the scene here.")]
    [SerializeField] private EnemySpawner spawner;

    private bool hasTriggered = false;

    [Header("Death Effects")]
    [SerializeField] private ChunkExplosionSpawner explosionSpawner;

    private void Update()
    {
        if (!hasTriggered && transform.position.y < fallThresholdY)
        {
            hasTriggered = true;

            // 🔥 spawn explosion effect first
            if (explosionSpawner != null)
            {
                explosionSpawner.SpawnChunkExplosion();
            }

            // delay disable
            StartCoroutine(DisableAndNotify());
        }
    }

    public void ForceKillDEBUG()
    {
        if (hasTriggered) return;

        hasTriggered = true;

        if (explosionSpawner != null)
            explosionSpawner.SpawnChunkExplosion();

        StartCoroutine(DisableAndNotify());
    }

    public void ForceKill()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        // Spawn explosion
        if (explosionSpawner != null)
            explosionSpawner.SpawnChunkExplosion();

        // Mirror fall-death behavior
        StartCoroutine(DisableAndNotify());
    }

    private System.Collections.IEnumerator DisableAndNotify()
    {
        yield return null; // wait 1 frame

        // Disable enemy
        gameObject.SetActive(false);

        // Tell the spawner
        if (spawner != null)
        {
            spawner.HandleEnemyDeath(gameObject);
            Debug.Log($"{name} fell off map. Respawning...");
        }
        else
        {
            Debug.LogWarning("EnemyFallDetector: No EnemySpawner assigned!");
        }
    }
}
