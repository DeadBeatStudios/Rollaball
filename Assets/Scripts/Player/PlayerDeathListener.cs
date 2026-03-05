using UnityEngine;

/// <summary>
/// Listens for global DeathEvents and routes
/// player-specific deaths to PlayerRespawn.
/// </summary>
[RequireComponent(typeof(PlayerRespawn))]
public class PlayerDeathListener : MonoBehaviour
{
    private PlayerRespawn respawn;

    private void Awake()
    {
        respawn = GetComponent<PlayerRespawn>();
    }

    private void OnEnable()
    {
        DeathEvents.OnDeath += HandleDeathEvent;
    }

    private void OnDisable()
    {
        DeathEvents.OnDeath -= HandleDeathEvent;
    }

    // --------------------------------------------------------------
    //  EVENT HANDLER
    // --------------------------------------------------------------

    private void HandleDeathEvent(
        GameObject victim,
        DeathEvents.DeathCause cause,
        GameObject instigator)
    {
        if (victim != gameObject)
            return;

        // Guard against double-death
        if (respawn.IsDead)
            return;

        respawn.Kill(cause, instigator);
    }
}
