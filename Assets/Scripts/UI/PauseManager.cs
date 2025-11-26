using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    [Header("Game Mode")]
    public bool isMultiplayer = false;
    // TRUE = countdown/music ignore pause

    public bool IsPaused { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void TogglePause()
    {
        if (isMultiplayer)
        {
            Debug.Log("Pause ignored (multiplayer mode).");
            return;
        }

        if (IsPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        if (isMultiplayer) return;

        IsPaused = true;
        Time.timeScale = 0f;
        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        if (isMultiplayer) return;

        IsPaused = false;
        Time.timeScale = 1f;
        Debug.Log("Game Resumed");
    }
}
