using UnityEngine;
using static DeathEvents;

[RequireComponent(typeof(Collider))]
public class LavaKillTrigger : MonoBehaviour
{
    private void Reset()
    {
        // Ensure trigger collider
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore triggers
        if (other.isTrigger)
            return;

        // We only care about root objects (player / enemy)
        Transform root = other.attachedRigidbody != null
            ? other.attachedRigidbody.transform
            : other.transform;

        // Report instant death
        DeathEvents.ReportDeath(
            root.gameObject,
            DeathCause.Lava
        );
    }
}
