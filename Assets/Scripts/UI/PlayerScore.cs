using UnityEngine;

public class PlayerScore : MonoBehaviour
{
    public int playerID;

    [Header("Player Info")]
    [SerializeField] private string playerName = "";  // allows preview but overwritten on start
    [SerializeField] private bool isEnemy = false;    // 💡 Check this box for enemies in Inspector

    private void Awake()
    {
        // Use InstanceID for scoreboard indexing
        playerID = GetInstanceID();
    }

    private void Start()
    {
        // 🔥 CRITICAL: Determine if this is player or enemy
        if (isEnemy || gameObject.name.ToLower().Contains("enemy"))
        {
            // Generate enemy name
            GenerateEnemyName();
        }
        else if (gameObject.CompareTag("Player"))  // Make sure your player has "Player" tag
        {
            // Apply name selected in main menu ONLY to player
            if (!string.IsNullOrEmpty(PlayerProfile.PlayerName))
                playerName = PlayerProfile.PlayerName;
            else
                playerName = "Player";
        }
        else
        {
            // Fallback for other entities
            if (string.IsNullOrEmpty(playerName))
                playerName = gameObject.name;
        }

        // Register in GameManager
        GameManager.Instance.RegisterPlayer(playerID);
        GameManager.Instance.SetPlayerName(playerID, playerName);

        Debug.Log($"GameObject: {gameObject.name} , Tag: {gameObject.tag} , PlayerProfile Name: '{PlayerProfile.PlayerName}'");
    }

    private void GenerateEnemyName()
    {
        // 💡 Generate varied enemy names
        string[] enemyTypes = { "Adrian", "Ethan", "Benjamin", "Corey", "Connie", "Dennis" };
        string type = enemyTypes[Random.Range(0, enemyTypes.Length)];
        int number = Random.Range(100, 999);
        playerName = $"{type}_{number}";  // e.g., "Guard_247"
    }

    public void AddPoints(int points)
    {
        GameManager.Instance.AddPoints(playerID, points);
    }

    // 💡 Allow runtime name changes (useful for multiplayer later)
    public void SetPlayerName(string newName)
    {
        playerName = newName;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerName(playerID, playerName);
        }
    }

    public int ID => playerID;
    public string PlayerName => playerName;
}