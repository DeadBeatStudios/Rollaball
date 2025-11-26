using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Scoreboard UI")]
    [SerializeField] private Transform scoreboardRoot;   // Parent container for rows
    [SerializeField] private GameObject scoreboardRowPrefab;

    // Internal storage (so we don’t keep clearing/recreating rows)
    private Dictionary<int, TextMeshProUGUI> rowDisplays = new Dictionary<int, TextMeshProUGUI>();

    private void Awake()
    {
        Instance = this;
    }

    // Refresh entire scoreboard
    public void RefreshScoreboard(IReadOnlyDictionary<int, int> scores)
    {
        foreach (var entry in scores)
        {
            int id = entry.Key;
            int score = entry.Value;

            if (!rowDisplays.ContainsKey(id))
            {
                GameObject row = Instantiate(scoreboardRowPrefab, scoreboardRoot);
                TextMeshProUGUI text = row.GetComponentInChildren<TextMeshProUGUI>();

                rowDisplays[id] = text;
            }

            rowDisplays[id].text = $"ID {id}: {score}";
        }
    }
}
