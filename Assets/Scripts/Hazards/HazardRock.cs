using UnityEngine;

public class HazardRock : MonoBehaviour
{
    [Header("Rock Settings")]
    public float lifetime = 10f;
    public float minScale = 5f;
    public float maxScale = 10f;

    [Header("Impact Settings")]
    [Tooltip("How long the rock rolls after hitting the ground.")]
    public float postImpactLifetime = 1.0f;

    private bool hasHitGround = false;

    private void Start()
    {
        // Randomize rock size
        float size = Random.Range(minScale, maxScale);
        transform.localScale = Vector3.one * size;

        // Auto-destroy later if nothing happens
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Collider other = collision.collider;
        if (other == null) return;

        // ----- PLAYER OR ENEMY HIT -----
        if (other.CompareTag("Player") || other.CompareTag("Enemy"))
        {
            // Reporter-only: no respawn/spawner/flag logic here.
            DeathEvents.ReportDeath(other.gameObject, DeathEvents.DeathCause.Hazard, gameObject);

            Destroy(gameObject);
            return;
        }

        // ----- GROUND / ANYTHING ELSE -----
        if (!hasHitGround)
        {
            hasHitGround = true;

            // Let it roll/slide for a bit
            Destroy(gameObject, postImpactLifetime);
        }
    }
}
