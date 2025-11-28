using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Scoreboard UI")]
    public GameObject scoreboardPanel;
    public Transform scoreboardContent;
    public GameObject scoreboardRowPrefab;

    [Header("Settings")]
    [SerializeField] private bool showHeaderRow = true;  // 💡 New: Toggle header

    public void RefreshScoreboard(IReadOnlyDictionary<int, int> scores, IReadOnlyDictionary<int, string> playerNames = null)  // 💡 Modified: Accept names
    {
        if (scoreboardContent == null || scoreboardRowPrefab == null)
        {
            Debug.LogError("UIManager: ScoreboardContent or RowPrefab not assigned!");
            return;
        }

        // Clear old rows
        foreach (Transform child in scoreboardContent)
            Destroy(child.gameObject);

        // 💡 New: Add header row
        if (showHeaderRow)
        {
            GameObject headerRow = Instantiate(scoreboardRowPrefab, scoreboardContent);
            ScoreboardRowUI headerUI = headerRow.GetComponent<ScoreboardRowUI>();
            if (headerUI != null)
                headerUI.SetHeader("Player", "Score");  // Uses new method
        }

        // Convert + sort
        var sorted = scores.ToList();
        sorted.Sort((a, b) => b.Value.CompareTo(a.Value));

        // Build rows
        foreach (var kvp in sorted)
        {
            GameObject rowObj = Instantiate(scoreboardRowPrefab, scoreboardContent);
            ScoreboardRowUI rowUI = rowObj.GetComponent<ScoreboardRowUI>();
            if (rowUI != null)
            {
                string name = playerNames?.GetValueOrDefault(kvp.Key, $"Player {kvp.Key}") ?? $"Player {kvp.Key}";
                rowUI.SetRow(name, kvp.Value);  // 💡 Modified: Pass name instead of ID
            }
        }
    }

    // Keep old signature for compatibility
    public void RefreshScoreboard(IReadOnlyDictionary<int, int> scores)
    {
        RefreshScoreboard(scores, null);
    }
}