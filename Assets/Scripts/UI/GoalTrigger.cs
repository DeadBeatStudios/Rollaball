using UnityEngine;
using System.Collections;

public class GoalTrigger : MonoBehaviour
{
    [Header("Scoring")]
    [SerializeField] private int pointsPerScore = 100;
    [SerializeField] private float moveDelayAfterScore = 1.5f;

    [Header("References")]
    [SerializeField] private FlagPickup flag;

    [Header("Spawn Positions")]
    [SerializeField] private Transform[] spawnPoints;

    private int lastSpawnIndex = -1;
    private bool isProcessingScore = false;

    private void Start()
    {
        if (flag == null)
            flag = FindAnyObjectByType<FlagPickup>();

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int startIndex = Random.Range(0, spawnPoints.Length);
            transform.position = spawnPoints[startIndex].position;
            transform.rotation = spawnPoints[startIndex].rotation;
            lastSpawnIndex = startIndex;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isProcessingScore)
            return;

        if (!other.TryGetComponent<PlayerScore>(out var scorer))
            return;

        if (flag == null)
        {
            Debug.LogError("GoalTrigger: No Flag assigned!");
            return;
        }

        if (!flag.IsHeldBy(other.transform))
            return;

        StartCoroutine(ScoreSequence(scorer, other.name));
    }

    private IEnumerator ScoreSequence(PlayerScore scorer, string scorerName)
    {
        isProcessingScore = true;

        scorer.AddPoints(pointsPerScore);
        flag.DropAndRespawn(FlagPickup.FlagDropCause.SelfDestruct);

        Debug.Log($"🏁 {scorerName} scored! +{pointsPerScore}");

        yield return new WaitForSeconds(moveDelayAfterScore);

        MoveToRandomPosition();
        isProcessingScore = false;
    }

    private void MoveToRandomPosition()
    {
        if (spawnPoints == null || spawnPoints.Length < 2)
            return;

        int newIndex;
        do
        {
            newIndex = Random.Range(0, spawnPoints.Length);
        } while (newIndex == lastSpawnIndex);

        transform.position = spawnPoints[newIndex].position;
        transform.rotation = spawnPoints[newIndex].rotation;
        lastSpawnIndex = newIndex;
    }
}
