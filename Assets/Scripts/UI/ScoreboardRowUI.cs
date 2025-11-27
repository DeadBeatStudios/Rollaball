using TMPro;
using UnityEngine;

public class ScoreboardRowUI : MonoBehaviour
{
    public TextMeshProUGUI playerText;
    public TextMeshProUGUI scoreText;

    public void SetRow(int id, int score)
    {
        if (playerText != null)
            playerText.text = $"Player {id}";

        if (scoreText != null)
            scoreText.text = $"{score}";
    }
}
