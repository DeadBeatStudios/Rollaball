using UnityEngine;

public class PlayerScore : MonoBehaviour
{
    public int playerID;  // Assigned at runtime using InstanceID

    private void Awake()
    {
        // Use InstanceID so ALL players/enemies show on scoreboard
        playerID = GetInstanceID();
    }

    private void Start()
    {
        // REGISTER USING ONLY ONE ARGUMENT
        GameManager.Instance.RegisterPlayer(playerID);
    }

    public void AddPoints(int points)
    {
        GameManager.Instance.AddPoints(playerID, points);
    }

    public int ID => playerID;
}
