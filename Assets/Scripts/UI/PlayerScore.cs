using UnityEngine;

public class PlayerScore : MonoBehaviour
{
    public int playerID;  // Assigned at runtime using InstanceID

    [Header("Player Info")]
    [SerializeField] private string playerName = "";  // 💡 New: Set in Inspector or generate

    private void Awake()
    {
        // Use InstanceID so ALL players/enemies show on scoreboard
        playerID = GetInstanceID();

        // Auto-generate name if not set
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = gameObject.name;  // Use GameObject name as fallback
        }
    }

    private void Start()
    {
        // REGISTER USING ONLY ONE ARGUMENT
        GameManager.Instance.RegisterPlayer(playerID);
        GameManager.Instance.SetPlayerName(playerID, playerName);  // 💡 New: Register name
    }

    public void AddPoints(int points)
    {
        GameManager.Instance.AddPoints(playerID, points);
    }

    public int ID => playerID;
    public string PlayerName => playerName;  // 💡 New getter
}