using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LavaKillTrigger : MonoBehaviour
{
    [Header("Filter")]
    [Tooltip("Only objects with these tags will be killed.")]
    [SerializeField] private string[] killTags = { "Player", "Enemy" };

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || other.isTrigger)
            return;

        Transform victimRoot = GetVictimRoot(other);
        if (victimRoot == null)
            return;

        if (!HasKillTag(victimRoot))
            return;

        DeathEvents.ReportDeath(
            victimRoot.gameObject,
            DeathEvents.DeathCause.Lava,
            gameObject
        );
    }

    private Transform GetVictimRoot(Collider other)
    {
        if (other.attachedRigidbody != null)
            return other.attachedRigidbody.transform.root;

        return other.transform.root;
    }

    private bool HasKillTag(Transform victimRoot)
    {
        if (killTags == null || killTags.Length == 0)
            return true; // if you clear the list, lava kills anything (not recommended)

        for (int i = 0; i < killTags.Length; i++)
        {
            if (victimRoot.CompareTag(killTags[i]))
                return true;
        }

        return false;
    }
}
