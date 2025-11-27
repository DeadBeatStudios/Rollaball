using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Scoreboard UI")]
    public GameObject scoreboardPanel;
    public Transform scoreboardContent;
    public GameObject scoreboardRowPrefab;

    /// <summary>
    /// Rebuild the scoreboard UI from scratch based on player scores.
    /// </summary>
    public void RefreshScoreboard(IReadOnlyDictionary<int, int> scores)
    {
        if (scoreboardContent == null || scoreboardRowPrefab == null)
        {
            Debug.LogError("UIManager: ScoreboardContent or RowPrefab not assigned!");
            return;
        }

        // Clear old rows
        foreach (Transform child in scoreboardContent)
            Destroy(child.gameObject);

        // Sort scores descending
        var sorted = new List<KeyValuePair<int, int>>(scores);
        sorted.Sort((a, b) => b.Value.CompareTo(a.Value));

        // Spawn one row per player
        foreach (var kvp in sorted)
        {
            int id = kvp.Key;
            int score = kvp.Value;

            GameObject rowObj = Instantiate(scoreboardRowPrefab, scoreboardContent);
            ScoreboardRowUI rowUI = rowObj.GetComponent<ScoreboardRowUI>();

            if (rowUI != null)
                rowUI.SetRow(id, score);
            else
                Debug.LogError("UIManager: ScoreboardRow prefab is missing ScoreboardRowUI script!");
        }
    }
}
