using UnityEngine;

public class VolcanoRock : MonoBehaviour
{
    [Header("Lifetime")]
    [SerializeField] float lifetime = 8f;

    [Header("Collision Settings")]
    [SerializeField] float damageForceThreshold = 2.5f;

    private void Start()
    {
        // Auto destroy
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Ignore weak impacts (small bounces)
        if (collision.relativeVelocity.magnitude < damageForceThreshold)
            return;

        // Hit a player?
        if (collision.collider.TryGetComponent<PlayerRespawn>(out var player))
        {
            player.HandleDeath(FlagPickup.FlagDropCause.KilledByEnemy);
            Destroy(gameObject);
            return;
        }

        // Destroy on ground/wall impact
        Destroy(gameObject);
    }
}
