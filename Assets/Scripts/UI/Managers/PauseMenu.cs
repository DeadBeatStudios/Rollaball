using UnityEngine;
using UnityEngine.InputSystem;   // REQUIRED for new Input System

public class PauseMenu : MonoBehaviour
{
    [Header("Menu")]
    public GameObject pauseMenuUI;

    [Header("Scene Loading")]
    public SceneLoader sceneLoader;

    [Header("Music Timer Reference")]
    [SerializeField] private MusicCountdownUI musicCountdown;  // auto-assigned if null

    private bool isPaused = false;

    private void Awake()
    {
        // Auto-assign the MusicCountdownUI if not set
        if (musicCountdown == null)
        {
            musicCountdown = FindAnyObjectByType<MusicCountdownUI>();
        }
    }

    // Input System callback (Esc / Start button)
    public void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (isPaused)
            Resume();
        else
        {
            // 🔥 Pause audio BEFORE UI opens (instant freeze)
            if (musicCountdown != null)
                musicCountdown.PauseTimer(true);

            Pause();
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        // 🔥 Resume timer + music
        if (musicCountdown != null)
            musicCountdown.PauseTimer(false);
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        // (Music is already paused instantly above)
    }

    public void QuitToMenu()
    {
        // Restore timescale before changing scenes
        Time.timeScale = 1f;

        sceneLoader.LoadScene("MainMenu");
    }
}
