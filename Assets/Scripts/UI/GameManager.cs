using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // ID → Score
    private Dictionary<int, int> playerScores = new Dictionary<int, int>();

    public IReadOnlyDictionary<int, int> Scores => playerScores;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Register any player or enemy
    public void RegisterPlayer(int id)
    {
        if (!playerScores.ContainsKey(id))
            playerScores[id] = 0;

        UIManager.Instance?.RefreshScoreboard(playerScores);
    }

    // Add score
    public void AddPoints(int id, int points)
    {
        if (!playerScores.ContainsKey(id))
            playerScores[id] = 0;

        playerScores[id] += points;

        // Update UI Scoreboard
        UIManager.Instance?.RefreshScoreboard(playerScores);
    }

    public int GetScore(int id)
    {
        return playerScores.ContainsKey(id) ? playerScores[id] : 0;
    }
}
