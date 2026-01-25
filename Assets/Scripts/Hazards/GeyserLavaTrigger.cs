using UnityEngine;

public class GeyserLavaTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LavaSpawner lavaSpawner;

    [Header("Automatic Emission")]
    [SerializeField] private bool useTimer = false;
    [SerializeField] private float emitInterval = 5f;

    [Header("Proximity Emission")]
    [SerializeField] private bool useProximity = true;
    [SerializeField] private float triggerRadius = 3f;
    [SerializeField] private string[] triggerTags = { "Player" };
    [SerializeField] private float proximityEmitInterval = 1.5f;

    private float timer;
    private float proximityTimer;

    private void Update()
    {
        // ============================
        // TIMER-BASED EMISSION
        // ============================
        if (useTimer)
        {
            timer += Time.deltaTime;
            if (timer >= emitInterval)
            {
                lavaSpawner.Emit();
                timer = 0f;
            }
        }

        // ============================
        // PROXIMITY-BASED EMISSION
        // ============================
        if (useProximity)
        {
            HandleProximityEmission();
        }
    }

    private void HandleProximityEmission()
    {
        bool targetInside = false;

        Collider[] hits = Physics.OverlapSphere(transform.position, triggerRadius);

        foreach (Collider hit in hits)
        {
            foreach (string tag in triggerTags)
            {
                if (hit.CompareTag(tag))
                {
                    targetInside = true;
                    break;
                }
            }
            if (targetInside) break;
        }

        if (targetInside)
        {
            proximityTimer += Time.deltaTime;

            if (proximityTimer >= proximityEmitInterval)
            {
                lavaSpawner.Emit();
                proximityTimer = 0f;
            }
        }
        else
        {
            // Reset timer when nothing is inside
            proximityTimer = 0f;
        }
    }

    // ============================
    // EDITOR VISUALIZATION
    // ============================
    private void OnDrawGizmosSelected()
    {
        if (useProximity)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }
    }
}
