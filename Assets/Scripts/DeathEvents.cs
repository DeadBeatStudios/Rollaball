using System;
using UnityEngine;

/// <summary>
/// Centralized event dispatcher for all death-related reporting.
/// This defines WHAT died and WHY — not what happens next.
/// </summary>
public static class DeathEvents
{
    // --------------------------------------------------------------
    //  DEATH CAUSE
    // --------------------------------------------------------------
    public enum DeathCause
    {
        Lava,
        FellOffMap,
        EnemyAttack,
        Hazard,
        SelfDestruct,
        Unknown
    }

    // --------------------------------------------------------------
    //  EVENT SIGNATURE
    // --------------------------------------------------------------
    // victim     → the GameObject that died
    // cause      → why it died
    // instigator → optional (enemy, player, hazard, etc.)
    public static event Action<GameObject, DeathCause, GameObject> OnDeath;

    // --------------------------------------------------------------
    //  REPORT API
    // --------------------------------------------------------------
    public static void ReportDeath(
        GameObject victim,
        DeathCause cause,
        GameObject instigator = null)
    {
        if (victim == null)
        {
            Debug.LogWarning("DeathEvents.ReportDeath called with null victim.");
            return;
        }

        OnDeath?.Invoke(victim, cause, instigator);
    }
}
