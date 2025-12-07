using UnityEngine;

public class HazardRockExploding : MonoBehaviour
{
    [Header("Explosion Settings")]
    [SerializeField] private float explosionForce = 300f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float upwardsModifier = 0.4f;

    [Header("Debris Settings")]
    [SerializeField] private float debrisHazardDuration = 1.5f;
    [SerializeField] private float debrisLifetime = 4f;

    [Header("Lifetime")]
    [SerializeField] private float maxLifetime = 10f;

    private bool hasExploded = false;

    private void Start()
    {
        // Safety cleanup if rock never hits anything
        Destroy(gameObject, maxLifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;
        hasExploded = true;

        // Damage on initial impact (before explosion)
        HandleImpactDamage(collision.collider);

        Explode();
    }

    private void HandleImpactDamage(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerRespawn pr = other.GetComponent<PlayerRespawn>();
            if (pr != null)
                pr.HandleDeath(FlagPickup.FlagDropCause.SelfDestruct);
        }
        else if (other.CompareTag("Enemy"))
        {
            EnemyFallDetector efd = other.GetComponent<EnemyFallDetector>();
            if (efd != null)
                efd.ForceKillDEBUG();
        }
    }

    private void Explode()
    {
        Vector3 explosionCenter = transform.position;

        // Process all child pieces
        foreach (Transform child in transform)
        {
            Rigidbody rb = child.GetComponent<Rigidbody>();
            if (rb == null) continue;

            // Unparent so debris moves independently
            child.SetParent(null);

            // Enable physics
            rb.isKinematic = false;

            // Apply explosive force
            rb.AddExplosionForce(explosionForce, explosionCenter, explosionRadius, upwardsModifier, ForceMode.Impulse);

            // Add hazard behavior
            HazardRockDebris debris = child.gameObject.AddComponent<HazardRockDebris>();
            debris.Initialize(debrisHazardDuration, debrisLifetime);
        }

        // Destroy empty parent
        Destroy(gameObject);
    }
}