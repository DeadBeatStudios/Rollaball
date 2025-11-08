using UnityEngine;

public class EnemyFallDetector : MonoBehaviour
{
    [SerializeField] private float fallThresholdY = -10f;
    private EnemySpawner spawner;
    private bool hasTriggered = false; // prevent duplicate respawn calls

    private void Start()
    {
        spawner = FindObjectOfType<EnemySpawner>();
    }

    private void Update()
    {
        if (!hasTriggered && transform.position.y < fallThresholdY)
        {
            hasTriggered = true;

            if (spawner != null)
            {
                // Disable immediately to prevent multiple calls
                gameObject.SetActive(false);

                spawner.HandleEnemyDeath(gameObject);
                Debug.Log($"{name} fell below map. Respawning...");
            }
            else
            {
                Debug.LogWarning("EnemyFallDetector: No EnemySpawner found!");
            }
        }
    }
}
