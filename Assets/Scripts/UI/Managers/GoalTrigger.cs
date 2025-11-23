using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    [Header("Scoring")]
    [SerializeField] private int pointsPerScore = 100;

    [Header("References")]
    [SerializeField] private FlagPickup flag;  // ← Drag the FlagPickup prefab or instance here

    private void OnTriggerEnter(Collider other)
    {
        // Must have a PlayerScore component (Player or Enemy)
        if (!other.TryGetComponent<PlayerScore>(out var scorer))
            return;

        if (flag == null)
        {
            Debug.LogError("GoalTrigger: No Flag assigned!");
            return;
        }

        // Must be holding the flag
        if (!flag.IsHeldBy(other.transform))
            return;

        scorer.AddPoints(pointsPerScore);

        flag.DropAndRespawn();

        Debug.Log($"🏁 {other.name} scored! +{pointsPerScore}");
    }
}
