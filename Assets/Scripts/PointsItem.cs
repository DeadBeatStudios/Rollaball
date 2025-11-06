using UnityEngine;

public class PointsItem : MonoBehaviour
{
    [SerializeField] private int pointsValue = 1;

    private void OnTriggerEnter(Collider other)
    {
        // Ignore anything that is NOT the Player
        if (!other.CompareTag("Player"))
            return;

        // Optional: safety log for testing
        Debug.Log($"PointsItem collected by: {other.name}");

        // Add points (only if GameManager exists)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddPoints(0, pointsValue); // playerID = 0 for now
        }

        // Remove the collectible
        Destroy(gameObject);
    }

    private void Start()
    {
        // Optional: ensure this object has the correct physics setup
        var rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }
}