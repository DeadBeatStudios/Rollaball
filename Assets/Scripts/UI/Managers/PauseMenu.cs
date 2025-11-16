using UnityEngine;
using UnityEngine.InputSystem;   // REQUIRED for new Input System

public class PauseMenu : MonoBehaviour
{
    [Header("Menu")]
    public GameObject pauseMenuUI;

    [Header("Scene Loading")]
    public SceneLoader sceneLoader;

    private bool isPaused = false;

    // Input System callback
    public void OnPause(InputAction.CallbackContext context)
    {
        // Only respond when the button is "performed" (pressed)
        if (!context.performed)
            return;

        if (isPaused)
            Resume();
        else
            Pause();
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        sceneLoader.LoadScene("MainMenu");
    }
}
