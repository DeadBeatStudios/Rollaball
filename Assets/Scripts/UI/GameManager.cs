using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central authority for scoring, scoreboard updates, and player registration.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [Tooltip("Assign your UIManager in Inspector.")]
    public UIManager uiManager;

    // Player scores stored using InstanceID → Score
    private Dictionary<int, int> scores = new Dictionary<int, int>();
    private Dictionary<int, string> playerNames = new Dictionary<int, string>();  // 💡 New: Store names

    public IReadOnlyDictionary<int, int> Scores => scores;
    public IReadOnlyDictionary<int, string> PlayerNames => playerNames;  // 💡 New: Expose names

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Registers a player by ID if not already stored.
    /// </summary>
    public void RegisterPlayer(int id)
    {
        if (!scores.ContainsKey(id))
            scores.Add(id, 0);
    }

    /// <summary>
    /// Sets/updates player name  // 💡 New method
    /// </summary>
    public void SetPlayerName(int id, string name)
    {
        playerNames[id] = name;
    }

    /// <summary>
    /// Adds points to a player and updates the scoreboard.
    /// </summary>
    public void AddPoints(int id, int value)
    {
        if (!scores.ContainsKey(id))
            scores[id] = 0;

        scores[id] += value;
        Debug.Log($"🏆 Player {playerNames.GetValueOrDefault(id, id.ToString())} scored! New score = {scores[id]}");

        // Update UI if available - pass names too
        if (uiManager != null)
            uiManager.RefreshScoreboard(scores, playerNames);  // 💡 Modified: Pass names
    }

    /// <summary>
    /// Gets score for an ID safely.
    /// </summary>
    public int GetScore(int id)
    {
        return scores.TryGetValue(id, out int score) ? score : 0;
    }
}