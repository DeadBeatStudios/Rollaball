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

    // Player scores stored using ID → Score
    private readonly Dictionary<int, int> scores = new Dictionary<int, int>();
    private readonly Dictionary<int, string> playerNames = new Dictionary<int, string>();

    public IReadOnlyDictionary<int, int> Scores => scores;
    public IReadOnlyDictionary<int, string> PlayerNames => playerNames;

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
    /// Unregisters a player/enemy by ID (useful when despawning or resetting a match).
    /// </summary>
    public void UnregisterPlayer(int id)
    {
        scores.Remove(id);
        playerNames.Remove(id);

        if (uiManager != null)
            uiManager.RefreshScoreboard(scores, playerNames);
    }

    /// <summary>
    /// Sets/updates player name.
    /// </summary>
    public void SetPlayerName(int id, string name)
    {
        playerNames[id] = name;

        // Optional: refresh when name changes so UI updates immediately
        if (uiManager != null)
            uiManager.RefreshScoreboard(scores, playerNames);
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

        if (uiManager != null)
            uiManager.RefreshScoreboard(scores, playerNames);
    }

    /// <summary>
    /// Gets score for an ID safely.
    /// </summary>
    public int GetScore(int id)
    {
        return scores.TryGetValue(id, out int score) ? score : 0;
    }

    /// <summary>
    /// Clears all registered players/enemies and scores (useful on match restart / returning to menu).
    /// </summary>
    public void ClearAll()
    {
        scores.Clear();
        playerNames.Clear();

        if (uiManager != null)
            uiManager.RefreshScoreboard(scores, playerNames);
    }
}
