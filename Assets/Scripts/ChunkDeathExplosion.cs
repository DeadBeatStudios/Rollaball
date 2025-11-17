using System.Collections.Generic;
using UnityEngine;

public class ChunkDeathExplosion : MonoBehaviour
{
    [Header("Chunk Setup")]
    [Tooltip("Rigidbodies for each chunk piece. Typically children of this object, disabled by default.")]
    [SerializeField] private List<Rigidbody> chunkRigidbodies = new List<Rigidbody>();

    [Tooltip("If true, chunks will be detached from this object on explosion.")]
    [SerializeField] private bool chunksAreChildren = true;

    [Header("Explosion Settings")]
    [SerializeField] private float explosionForce = 12f;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float upwardsModifier = 0.5f;

    [Tooltip("Randomizes force by ± this fraction of explosionForce.")]
    [SerializeField, Range(0f, 1f)] private float randomForceJitter = 0.25f;

    [Tooltip("Maximum random torque applied to each axis.")]
    [SerializeField] private float randomTorque = 5f;

    [Header("Lifetime Settings")]
    [Tooltip("How long chunks live before being destroyed. Set to 0 to keep them permanently.")]
    [SerializeField] private float chunkLifetime = 4f;

    [Tooltip("Destroy the root object (this) shortly after triggering the explosion.")]
    [SerializeField] private bool destroyRootAfterExplosion = true;

    [SerializeField] private float rootDestroyDelay = 0.05f;

    [Header("References")]
    [Tooltip("Main visual to disable when exploding (mesh, model, etc). If null, the GameObject itself is used.")]
    [SerializeField] private GameObject mainVisualRoot;

    [Tooltip("Any colliders to disable when the object 'dies'.")]
    [SerializeField] private Collider[] collidersToDisable;

    private bool hasExploded;

    /// <summary>
    /// Call this from your health/death logic to trigger the chunk explosion.
    /// </summary>
    /// <param name="explosionPositionOverride">
    /// Optional world position for the center of the explosion. If null, uses transform.position.
    /// </param>
    public void TriggerExplosion(Vector3? explosionPositionOverride = null)
    {
        if (hasExploded) return;
        hasExploded = true;

        Vector3 explosionPosition = explosionPositionOverride ?? transform.position;

        // Disable main visuals
        if (mainVisualRoot != null)
        {
            mainVisualRoot.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }

        // Disable colliders
        if (collidersToDisable != null)
        {
            for (int i = 0; i < collidersToDisable.Length; i++)
            {
                if (collidersToDisable[i] != null)
                {
                    collidersToDisable[i].enabled = false;
                }
            }
        }

        if (chunkRigidbodies == null || chunkRigidbodies.Count == 0)
        {
            Debug.LogWarning($"[ChunkDeathExplosion] No chunk rigidbodies assigned on {name}.", this);
            return;
        }

        // Activate and throw chunks
        foreach (Rigidbody rb in chunkRigidbodies)
        {
            if (rb == null) continue;

            if (chunksAreChildren)
            {
                // Detach from parent and enable
                rb.transform.SetParent(null, true);
                rb.gameObject.SetActive(true);
            }

            // Randomize force a bit per chunk
            float jitter = 1f + Random.Range(-randomForceJitter, randomForceJitter);
            float finalForce = explosionForce * jitter;

            // Add explosion force
            rb.AddExplosionForce(finalForce, explosionPosition, explosionRadius, upwardsModifier, ForceMode.Impulse);

            // Add random torque
            Vector3 torque = new Vector3(
                Random.Range(-randomTorque, randomTorque),
                Random.Range(-randomTorque, randomTorque),
                Random.Range(-randomTorque, randomTorque)
            );

            rb.AddTorque(torque, ForceMode.Impulse);

            // Schedule chunk cleanup
            if (chunkLifetime > 0f)
            {
                Destroy(rb.gameObject, chunkLifetime);
            }
        }

        // Optionally destroy the original root
        if (destroyRootAfterExplosion)
        {
            Destroy(gameObject, rootDestroyDelay);
        }
    }

    // Convenience function to make setup easier in the Inspector.
    private void Reset()
    {
        if (mainVisualRoot == null)
        {
            mainVisualRoot = gameObject;
        }

        if (collidersToDisable == null || collidersToDisable.Length == 0)
        {
            collidersToDisable = GetComponentsInChildren<Collider>();
        }

        if (chunkRigidbodies == null || chunkRigidbodies.Count == 0)
        {
            // Auto-collect child rigidbodies (disabled chunks) but skip self if it has one.
            Rigidbody[] childBodies = GetComponentsInChildren<Rigidbody>(true);
            foreach (Rigidbody rb in childBodies)
            {
                if (rb.gameObject == gameObject) continue;
                chunkRigidbodies.Add(rb);
                rb.gameObject.SetActive(false);
            }
        }
    }

    // Handy for quick testing directly from the Inspector.
    [ContextMenu("Test Explosion")]
    private void TestExplosionFromContextMenu()
    {
        TriggerExplosion();
    }
}
