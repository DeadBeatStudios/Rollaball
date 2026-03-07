using UnityEngine;

public class ChunkExplosionSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject playerModelRoot;

    [Tooltip("Auto-loaded from Resources/DeathFX.")]
    [SerializeField] private GameObject explosionChunkPrefab;

    [Header("Appearance Source")]
    [Tooltip("Reads the live applied player appearance color from this controller.")]
    [SerializeField] private PlayerAppearanceController appearanceController;

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

        // Try to auto-find appearance controller if not manually assigned
        if (appearanceController == null)
        {
            appearanceController = GetComponent<PlayerAppearanceController>();

            if (appearanceController == null)
            {
                Debug.LogWarning(
                    $"⚠️ {name}: ChunkExplosionSpawner could not find PlayerAppearanceController. " +
                    "Chunks will use their default material colors."
                );
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

        ApplyAppearanceToChunks(explosionInstance);

        ChunkDeathExplosion explosion = explosionInstance.GetComponent<ChunkDeathExplosion>();
        if (explosion != null)
            explosion.TriggerExplosion(spawnPos);
    }

    private void ApplyAppearanceToChunks(GameObject explosionInstance)
    {
        if (appearanceController == null)
            return;

        Color chunkColor = appearanceController.AppliedColor;

        Renderer[] renderers = explosionInstance.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in renderers)
        {
            Material[] materials = r.materials;

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null)
                    continue;

                materials[i].color = chunkColor;
            }
        }
    }

    public void RestorePlayerModel()
    {
        if (playerModelRoot != null)
            playerModelRoot.SetActive(true);
    }
}