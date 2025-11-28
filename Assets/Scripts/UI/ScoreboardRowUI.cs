using TMPro;
using UnityEngine;

public class ScoreboardRowUI : MonoBehaviour
{
    public TextMeshProUGUI playerText;
    public TextMeshProUGUI scoreText;

    // 💡 Modified: Accept string for name
    public void SetRow(string playerName, int score)
    {
        if (playerText != null)
            playerText.text = playerName;
        if (scoreText != null)
            scoreText.text = score.ToString();
    }

    // 💡 New: Header row setup
    public void SetHeader(string col1, string col2)
    {
        if (playerText != null)
            playerText.text = col1;
        if (scoreText != null)
            scoreText.text = col2;

        // Optional: Make header bold
        if (playerText != null)
            playerText.fontStyle = FontStyles.Bold;
        if (scoreText != null)
            scoreText.fontStyle = FontStyles.Bold;
    }

    // Keep old signature for backward compatibility
    public void SetRow(int id, int score)
    {
        SetRow($"Player {id}", score);
    }
}