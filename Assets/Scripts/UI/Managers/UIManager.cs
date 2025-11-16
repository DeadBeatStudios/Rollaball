using UnityEngine;
using TMPro;   // Make sure this is included

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI References")]
    [SerializeField] private TMP_Text scoreText;      // assigned in Inspector
    [SerializeField] private TMP_Text highScoreText;  // assigned in Inspector

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Initialize from GameManager values
        if (scoreText != null)
            scoreText.text = "Score: 0";

        if (highScoreText != null)
        {
            int savedHigh = PlayerPrefs.GetInt("HighScore", 0);
            highScoreText.text = "High Score: " + savedHigh;
        }
    }

    // Called whenever points are added
    public void UpdateScoreUI(int currentScore, int highScore)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {currentScore}";

        if (highScoreText != null)
            highScoreText.text = $"High Score: {highScore}";
    }
}
