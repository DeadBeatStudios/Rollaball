using UnityEngine;

public class HazardRockExploding : MonoBehaviour
{
    [Header("Fall Settings")]
    [SerializeField] private float initialDownwardSpeed = 30f;

    [Header("Fractured Prefab")]
    [SerializeField] private GameObject fracturedPrefab;

    [Header("Explosion Settings")]
    [SerializeField] private float explosionForce = 300f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float upwardsModifier = 0.4f;

    [Header("Debris Settings")]
    [SerializeField] private float debrisHazardDuration = 0.5f;
    [SerializeField] private float debrisLifetime = 1.0f;

    [Header("Lifetime")]
    [SerializeField] private float maxLifetime = 10f;

    private GameObject impactEffectPrefab;
    private bool hasExploded = false;

    private void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.down * initialDownwardSpeed;
        }

        impactEffectPrefab = Resources.Load<GameObject>("SFX/RockImpactEffect");
        if (impactEffectPrefab == null)
            Debug.LogWarning("⚠️ Could not find RockImpactEffect in Resources/SFX folder");

        Destroy(gameObject, maxLifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;
        hasExploded = true;

        HandleImpactDamage(collision.collider);

        Vector3 impactPoint = collision.contacts[0].point;
        Explode(impactPoint);
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

    private void Explode(Vector3 impactPoint)
    {
        Vector3 explosionCenter = transform.position;
        Quaternion rotation = transform.rotation;
        Vector3 scale = transform.localScale;

        // Spawn impact effect at actual hit location
        if (impactEffectPrefab != null)
        {
            GameObject fx = Instantiate(impactEffectPrefab, impactPoint, Quaternion.identity);
            Destroy(fx, 3f);
        }

        // Spawn fractured version
        if (fracturedPrefab != null)
        {
            GameObject fractured = Instantiate(fracturedPrefab, explosionCenter, rotation);
            fractured.transform.localScale = scale;

            foreach (Transform child in fractured.transform)
            {
                Rigidbody rb = child.GetComponent<Rigidbody>();
                if (rb == null) continue;

                child.SetParent(null);
                rb.isKinematic = false;
                rb.AddExplosionForce(explosionForce, explosionCenter, explosionRadius, upwardsModifier, ForceMode.Impulse);

                HazardRockDebris debris = child.gameObject.AddComponent<HazardRockDebris>();
                debris.Initialize(debrisHazardDuration, debrisLifetime);
            }

            Destroy(fractured);
        }

        Destroy(gameObject);
    }
}