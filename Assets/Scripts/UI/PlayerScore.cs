using UnityEngine;
using System.Collections.Generic;

public class PlayerScore : MonoBehaviour
{
    public int playerID;

    [Header("Player Info")]
    [SerializeField] private string playerName = "";
    [SerializeField] private bool isEnemy = false;

    // Name pool (used by spawner now — kept here only if you want)
    private static List<string> availableNames = new List<string>();
    private static List<string> usedNames = new List<string>();
    private static Dictionary<string, int> nameCounts = new Dictionary<string, int>();

    private void Awake()
    {
        // Only default to InstanceID if NOT an enemy (player identity can remain per object).
        if (!IsEnemy())
            playerID = GetInstanceID();

        // Initialize pool once (safe to leave)
        if (availableNames.Count == 0)
            ResetNamePool();
    }

    private static void ResetNamePool()
    {
        availableNames = new List<string>
        {
            "Adrian", "Ethan", "Benjamin", "Corey", "Dennis",
            "Connie", "Miranda", "Zoe", "Christy", "Mike"
        };
        usedNames.Clear();
        nameCounts.Clear();
    }

    private void Start()
    {
        if (IsEnemy())
        {
            // Enemy name/ID should be assigned by EnemySpawner (stable across respawns).
            // If not assigned, fall back to current GameObject name to avoid blanks.
            if (string.IsNullOrEmpty(playerName))
                playerName = gameObject.name;
        }
        else if (gameObject.CompareTag("Player"))
        {
            if (!string.IsNullOrEmpty(PlayerProfile.PlayerName))
                playerName = PlayerProfile.PlayerName;
            else
                playerName = "Player";
        }
        else
        {
            if (string.IsNullOrEmpty(playerName))
                playerName = gameObject.name;
        }

        GameManager.Instance.RegisterPlayer(playerID);
        GameManager.Instance.SetPlayerName(playerID, playerName);
    }

    private bool IsEnemy()
        => isEnemy || gameObject.name.ToLower().Contains("enemy");

    // Called by spawner to set stable identity for enemies
    public void SetIdentity(int id, string name)
    {
        playerID = id;
        playerName = name;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayer(playerID);
            GameManager.Instance.SetPlayerName(playerID, playerName);
        }
    }

    // Optional: if returning to menu
    public static void ResetEnemyNames()
    {
        ResetNamePool();
    }

    public void AddPoints(int points)
    {
        GameManager.Instance.AddPoints(playerID, points);
    }

    public void SetPlayerName(string newName)
    {
        playerName = newName;
        if (GameManager.Instance != null)
            GameManager.Instance.SetPlayerName(playerID, playerName);
    }

    public int ID => playerID;
    public string PlayerName => playerName;

    // (Old GenerateSmartEnemyName kept out intentionally — enemy naming is now spawner-driven)
}
