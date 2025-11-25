using UnityEngine;

public class HazardRock : MonoBehaviour
{
    [Header("Rock Settings")]
    public float lifetime = 10f;
    public float minScale = 5f;
    public float maxScale = 10f;

    private void Start()
    {
        // Randomize rock size
        float size = Random.Range(minScale, maxScale);
        transform.localScale = Vector3.one * size;

        // Auto-destroy if it never hits anything
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Ignore self collisions & emitters
        if (collision.collider.CompareTag("Enemy"))
            return;

        // 🔥 Kill the player if hit
        if (collision.collider.CompareTag("Player"))
        {
            PlayerRespawn pr = collision.collider.GetComponent<PlayerRespawn>();
            if (pr != null)
            {
                pr.HandleDeath(FlagPickup.FlagDropCause.SelfDestruct);
            }
        }

        // When rock hits anything, destroy it
        Destroy(gameObject);
    }
}
