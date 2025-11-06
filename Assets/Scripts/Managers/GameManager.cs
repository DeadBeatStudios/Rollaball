using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private Dictionary<int, int> playerScores = new Dictionary<int, int>();

    public int highScore { get; private set; } = 0;
    public int highScorePlayerID { get; private set; } = -1;

    private void Awake()
    {
        // Singleton pattern ensures only one GameManager exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 🔹 Load high score from PlayerPrefs when game starts
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        highScorePlayerID = PlayerPrefs.GetInt("HighScorePlayerID", -1);
    }

    public void RegisterPlayer(int playerID)
    {
        if (!playerScores.ContainsKey(playerID))
        {
            playerScores.Add(playerID, 0);
        }
    }

    // ✅ Keep only this version of AddPoints
    public void AddPoints(int playerID, int points)
    {
        if (!playerScores.ContainsKey(playerID))
            return;

        playerScores[playerID] += points;
        int currentScore = playerScores[playerID];

        // Update high score
        if (currentScore > highScore)
        {
            highScore = currentScore;
            highScorePlayerID = playerID;

            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.SetInt("HighScorePlayerID", highScorePlayerID);
            PlayerPrefs.Save();
        }

        // 🔹 Update UI
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateScoreUI(currentScore, highScore);
    }

    public int GetPlayerScore(int playerID)
    {
        return playerScores.ContainsKey(playerID) ? playerScores[playerID] : 0;
    }
}