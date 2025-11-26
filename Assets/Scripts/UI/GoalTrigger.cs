using UnityEngine;
using System.Collections;

public class GoalTrigger : MonoBehaviour
{
    [Header("Scoring")]
    [SerializeField] private int pointsPerScore = 100;
    [SerializeField] private float moveDelayAfterScore = 1.25f;

    [Header("References")]
    [SerializeField] private FlagPickup flag;

    [Header("Respawn Locations")]
    [SerializeField] private Transform[] spawnPoints;

    private bool isProcessingScore = false;
    private int lastSpawnIndex = -1;

    private void Awake()
    {
        // Auto-assign flag if missing
        if (flag == null)
            flag = FindFirstObjectByType<FlagPickup>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isProcessingScore) return;

        // Must have PlayerScore (works for players AND enemies)
        if (!other.TryGetComponent<PlayerScore>(out var scorer))
            return;

        // Must be holding the flag
        if (flag == null || !flag.IsHeldBy(other.transform))
            return;

        StartCoroutine(ScoreSequence(scorer));
    }

    private IEnumerator ScoreSequence(PlayerScore scorer)
    {
        isProcessingScore = true;

        // --- Award Score ---
        int scorerID = scorer.ID;   // NEW: InstanceID system
        GameManager.Instance.AddPoints(scorerID, pointsPerScore);

        Debug.Log($"🏁 Score! ID {scorerID} earns {pointsPerScore} points.");

        // Drop & random respawn the flag
        flag.DropAndRespawn();

        // Optional VFX/SFX can go here

        yield return new WaitForSeconds(moveDelayAfterScore);

        MoveGoalToNewPosition();
        isProcessingScore = false;
    }

    private void MoveGoalToNewPosition()
    {
        if (spawnPoints == null || spawnPoints.Length < 2)
        {
            Debug.LogWarning("GoalTrigger: Not enough spawn points (need 2+)");
            return;
        }

        int newIndex;
        do
        {
            newIndex = Random.Range(0, spawnPoints.Length);
        }
        while (newIndex == lastSpawnIndex);

        transform.position = spawnPoints[newIndex].position;
        transform.rotation = spawnPoints[newIndex].rotation;

        lastSpawnIndex = newIndex;

        Debug.Log($"📍 Goal moved to spawn index {newIndex}");
    }

    private void Start()
    {
        // Start goal at random
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int startIndex = Random.Range(0, spawnPoints.Length);
            transform.position = spawnPoints[startIndex].position;
            transform.rotation = spawnPoints[startIndex].rotation;
            lastSpawnIndex = startIndex;
        }
    }
}
