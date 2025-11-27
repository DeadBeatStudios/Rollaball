using UnityEngine;
using TMPro;

public class ScoreboardRowUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text NameText;
    public TMP_Text ScoreText;

    /// <summary>
    /// Updates a single scoreboard row.
    /// </summary>
    public void SetRow(int id, int score)
    {
        if (NameText != null)
            NameText.text = $"ID {id}";

        if (ScoreText != null)
            ScoreText.text = score.ToString();
    }
}
