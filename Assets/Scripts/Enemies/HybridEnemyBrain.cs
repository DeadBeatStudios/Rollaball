using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(HybridEnemyController))]
public class HybridEnemyBrain : MonoBehaviour
{
    public Transform flag;
    public Transform flagHolder;
    public Transform[] players;

    private HybridEnemyController controller;

    void Awake() => controller = GetComponent<HybridEnemyController>();

    void Update()
    {
        if (!flag) return;

        // Evaluate priorities
        Transform bestTarget = null;
        float bestScore = -Mathf.Infinity;

        foreach (Transform player in players)
        {
            bool isHolder = (player == flagHolder);
            float distance = Vector3.Distance(transform.position, player.position);

            float score = 0f;
            if (isHolder) score += 100f / Mathf.Max(distance, 1f);  // chase flag holder
            else score += 40f / Mathf.Max(distance, 1f);            // general aggression

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = player;
            }
        }

        // If flag is unclaimed, go get it
        if (!flagHolder)
        {
            float flagScore = 200f / Mathf.Max(Vector3.Distance(transform.position, flag.position), 1f);
            if (flagScore > bestScore)
            {
                bestScore = flagScore;
                bestTarget = flag;
            }
        }

        controller.target = bestTarget;
    }
}
