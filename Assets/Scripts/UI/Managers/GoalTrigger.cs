using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    [Header("Scoring")]
    [SerializeField] private int pointsPerScore = 100;

    private void OnTriggerEnter(Collider other)
    {
        // Only react to players
        if (!other.CompareTag("Player"))
            return;

        // Does this player have PlayerScore?
        PlayerScore scorer = other.GetComponent<PlayerScore>();
        if (scorer == null)
            return;

        // Does the player currently hold the flag?
        FlagPickup flag = FindObjectOfType<FlagPickup>();
        if (flag == null)
            return;

        if (!flag.IsHeldBy(other.transform))
            return;

        // Award points
        scorer.AddPoints(pointsPerScore);

        // Reset the flag
        flag.DropAndRespawn(FlagPickup.FlagDropCause.SelfDestruct);

        Debug.Log($"🏁 Player {scorer.playerID} scored! +{pointsPerScore} points");
    }
}
