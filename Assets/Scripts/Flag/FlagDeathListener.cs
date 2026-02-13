using UnityEngine;

/// <summary>
/// Listens for DeathEvents and reacts when the current flag carrier dies.
/// This removes the need for FlagPickup to poll PlayerRespawn.IsDead.
/// </summary>
[RequireComponent(typeof(FlagPickup))]
public class FlagDeathListener : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FlagPickup flag;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private void Awake()
    {
        // Preferred when attached to the flag object
        if (flag == null)
            flag = GetComponent<FlagPickup>();

        // Fallback (in case someone attaches this elsewhere)
        if (flag == null)
            flag = FindAnyObjectByType<FlagPickup>();
    }

    private void OnEnable()
    {
        DeathEvents.OnDeath += HandleDeathEvent;
    }

    private void OnDisable()
    {
        DeathEvents.OnDeath -= HandleDeathEvent;
    }

    private void HandleDeathEvent(
        GameObject victim,
        DeathEvents.DeathCause cause,
        GameObject instigator)
    {
        if (flag == null || victim == null)
            return;

        if (!flag.IsHeld)
            return;

        Transform holder = flag.CurrentHolder;
        if (holder == null)
            return;

        Transform victimT = victim.transform;

        // Robust match: victim can be the holder, a parent of holder, or a child under holder
        bool victimIsCarrier =
            victimT == holder ||
            holder.IsChildOf(victimT) ||
            victimT.IsChildOf(holder);

        if (!victimIsCarrier)
            return;

        if (debugLogs)
            Debug.Log($"FlagDeathListener: Carrier death detected. Victim={victim.name}, Cause={cause}");

        flag.OnCarrierDied(cause, instigator);
    }
}
