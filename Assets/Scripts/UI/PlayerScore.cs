using UnityEngine;

public class PlayerScore : MonoBehaviour
{
    public int ID { get; private set; }

    private void Awake()
    {
        // Unique per entity using InstanceID
        ID = GetInstanceID();
    }

    private void Start()
    {
        // Register in global manager
        GameManager.Instance.RegisterPlayer(ID);
    }

    public void AddPoints(int points)
    {
        GameManager.Instance.AddPoints(ID, points);
    }
}
