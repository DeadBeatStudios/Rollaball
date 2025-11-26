using UnityEngine;

public class ChunkExplosionSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject playerModelRoot;

    [Tooltip("Auto-loaded from Resources/DeathFX.")]
    [SerializeField] private GameObject explosionChunkPrefab;

    [Tooltip("Player material applied to the chunks.")]
    [SerializeField] private Material playerMaterial;

    private void Awake()
    {
        // Auto-load explosion prefab if not manually assigned
        if (explosionChunkPrefab == null)
        {
            explosionChunkPrefab = FXLoader.LoadChunkExplosion();

            if (explosionChunkPrefab == null)
            {
                Debug.LogError("❌ Cannot load Explosion_ChunkSet from Resources/DeathFX/");
            }
        }
    }

    public void SpawnChunkExplosion()
    {
        if (playerModelRoot != null)
            playerModelRoot.SetActive(false);

        if (explosionChunkPrefab == null)
        {
            Debug.LogError("❌ Explosion chunk prefab is missing.");
            return;
        }

        Vector3 spawnPos =
            playerModelRoot != null ? playerModelRoot.transform.position : transform.position;

        GameObject explosionInstance = Instantiate(
            explosionChunkPrefab,
            spawnPos,
            transform.rotation
        );

        // Apply player material to all renderers
        if (playerMaterial != null)
        {
            MeshRenderer[] renderers = explosionInstance.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers)
                r.material = playerMaterial;
        }

        // Trigger explosion physics
        ChunkDeathExplosion explosion = explosionInstance.GetComponent<ChunkDeathExplosion>();
        if (explosion != null)
            explosion.TriggerExplosion(spawnPos);
    }

    public void RestorePlayerModel()
    {
        if (playerModelRoot != null)
            playerModelRoot.SetActive(true);
    }
}
