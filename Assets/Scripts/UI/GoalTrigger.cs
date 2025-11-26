using UnityEngine;
using System.Collections;

public class GoalTrigger : MonoBehaviour
{
    [Header("Scoring")]
    [SerializeField] private int pointsPerScore = 100;
    [SerializeField] private float moveDelayAfterScore = 1.5f;  // Time for SFX/VFX to play

    [Header("References")]
    [SerializeField] private FlagPickup flag;

    [Header("Spawn Positions")]
    [SerializeField] private Transform[] spawnPoints;

    private int lastSpawnIndex = -1;
    private bool isProcessingScore = false;  // Prevent double-scoring during delay

    private void OnTriggerEnter(Collider other)
    {
        // Prevent scoring while goal is about to move
        if (isProcessingScore)
            return;

        // Must have a PlayerScore component
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

        // Start the scoring sequence
        StartCoroutine(ScoreSequence(scorer, other.name));
    }

    private IEnumerator ScoreSequence(PlayerScore scorer, string scorerName)
    {
        isProcessingScore = true;

        // Award points
        scorer.AddPoints(pointsPerScore);

        // Drop flag and respawn it
        flag.DropAndRespawn();

        Debug.Log($"🏁 {scorerName} scored! +{pointsPerScore}");

        // 🎵 SFX/VFX play here (handled by other systems listening for score events)

        // Wait for SFX to finish
        yield return new WaitForSeconds(moveDelayAfterScore);

        // Move goal to new position
        MoveToRandomPosition();

        isProcessingScore = false;
    }

    private void MoveToRandomPosition()
    {
        if (spawnPoints == null || spawnPoints.Length < 2)
        {
            Debug.LogWarning("GoalTrigger: Need at least 2 spawn points!");
            return;
        }

        int newIndex;

        // Keep rolling until we get a different position
        do
        {
            newIndex = Random.Range(0, spawnPoints.Length);
        }
        while (newIndex == lastSpawnIndex);

        // Move the goal
        transform.position = spawnPoints[newIndex].position;
        transform.rotation = spawnPoints[newIndex].rotation;

        lastSpawnIndex = newIndex;

        Debug.Log($"📍 Goal moved to position {newIndex + 1}");
    }

    private void Start()
    {
        // Start at random position
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int startIndex = Random.Range(0, spawnPoints.Length);
            transform.position = spawnPoints[startIndex].position;
            transform.rotation = spawnPoints[startIndex].rotation;
            lastSpawnIndex = startIndex;
        }
    }
}