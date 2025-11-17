using UnityEngine;

public class ChunkExplosionSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject playerModelRoot;
    [SerializeField] private GameObject explosionChunkPrefab;

    [Tooltip("Assign the player material so the chunks match the player’s look.")]
    [SerializeField] private Material playerMaterial;

    /// <summary>
    /// Swaps model → chunk prefab AND triggers the explosion.
    /// </summary>
    public void SpawnChunkExplosion()
    {
        if (playerModelRoot != null)
            playerModelRoot.SetActive(false);

        if (explosionChunkPrefab == null)
        {
            Debug.LogError("Explosion chunk prefab not assigned!");
            return;
        }

        // 🔥 Use the exact position of the visual mesh
        Vector3 spawnPos = playerModelRoot != null
            ? playerModelRoot.transform.position
            : transform.position;

        // Spawn chunk set
        GameObject explosionInstance = Instantiate(
            explosionChunkPrefab,
            spawnPos,
            transform.rotation
        );

        // Apply player material
        if (playerMaterial != null)
        {
            MeshRenderer[] renderers = explosionInstance.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers)
                r.material = playerMaterial;
        }

        // Trigger explosion physics at correct position
        ChunkDeathExplosion explosion = explosionInstance.GetComponent<ChunkDeathExplosion>();
        if (explosion != null)
            explosion.TriggerExplosion(spawnPos);
    }


    /// <summary>
    /// Called by PlayerRespawn to restore visibility.
    /// </summary>
    public void RestorePlayerModel()
    {
        if (playerModelRoot != null)
            playerModelRoot.SetActive(true);
    }
}
