using UnityEngine;

public class PlayerScore : MonoBehaviour
{
    public int playerID;
    private void Start()
    {
        // Register this player with the GameManager
        GameManager.Instance.RegisterPlayer(playerID);
    }

    public void AddPoints(int points)
    {
        GameManager.Instance.AddPoints(playerID, points);
    }
}