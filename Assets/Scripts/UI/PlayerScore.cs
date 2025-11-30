using UnityEngine;
using System.Collections.Generic;

public class PlayerScore : MonoBehaviour
{
    public int playerID;

    [Header("Player Info")]
    [SerializeField] private string playerName = "";
    [SerializeField] private bool isEnemy = false;

    // 💡 Static = shared across ALL PlayerScore instances
    private static List<string> availableNames = new List<string>();
    private static List<string> usedNames = new List<string>();
    private static Dictionary<string, int> nameCounts = new Dictionary<string, int>();

    private void Awake()
    {
        playerID = GetInstanceID();

        // 💡 Initialize name pool (only once, first enemy does this)
        if (availableNames.Count == 0)
        {
            ResetNamePool();
        }
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
        if (isEnemy || gameObject.name.ToLower().Contains("enemy"))
        {
            GenerateSmartEnemyName();
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

    private void GenerateSmartEnemyName()
    {
        string baseName;

        // 🔥 CRITICAL: If we still have unused names, pick one
        if (availableNames.Count > 0)
        {
            // Pick random unused name
            int index = Random.Range(0, availableNames.Count);
            baseName = availableNames[index];

            // Move it from available to used
            availableNames.RemoveAt(index);
            usedNames.Add(baseName);

            // First use - no number needed!
            playerName = baseName;
        }
        else
        {
            // All names used - pick from used names and add number
            baseName = usedNames[Random.Range(0, usedNames.Count)];

            // Track how many times this name has been reused
            if (!nameCounts.ContainsKey(baseName))
                nameCounts[baseName] = 1;

            nameCounts[baseName]++;

            // Add number for duplicates
            playerName = $"{baseName}_{nameCounts[baseName]}";
        }
    }

    // 💡 Call this when returning to menu or resetting game
    public static void ResetEnemyNames()
    {
        ResetNamePool();
    }

    // Rest of your existing code...
    public void AddPoints(int points)
    {
        GameManager.Instance.AddPoints(playerID, points);
    }

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