using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Scoreboard UI")]
    public GameObject scoreboardPanel;
    public Transform scoreboardContent;
    public GameObject scoreboardRowPrefab;

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

        // Convert + sort
        var sorted = scores.ToList();
        sorted.Sort((a, b) => b.Value.CompareTo(a.Value));

        // Build rows
        foreach (var kvp in sorted)
        {
            GameObject rowObj = Instantiate(scoreboardRowPrefab, scoreboardContent);
            ScoreboardRowUI rowUI = rowObj.GetComponent<ScoreboardRowUI>();

            if (rowUI != null)
                rowUI.SetRow(kvp.Key, kvp.Value);
        }
    }
}
