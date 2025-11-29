using UnityEngine;

public class PlayerScore : MonoBehaviour
{
    public int playerID;

    [Header("Player Info")]
    [SerializeField] private string playerName = "";  // allows preview but overwritten on start

    private void Awake()
    {
        // Use InstanceID for scoreboard indexing
        playerID = GetInstanceID();
    }

    private void Start()
    {
        // Apply name selected in main menu
        if (!string.IsNullOrEmpty(PlayerProfile.PlayerName))
            playerName = PlayerProfile.PlayerName;
        else
            playerName = "Player";

        // Register player in GameManager
        GameManager.Instance.RegisterPlayer(playerID);
        GameManager.Instance.SetPlayerName(playerID, playerName);
    }

    public void AddPoints(int points)
    {
        GameManager.Instance.AddPoints(playerID, points);
    }

    public int ID => playerID;
    public string PlayerName => playerName;
}
