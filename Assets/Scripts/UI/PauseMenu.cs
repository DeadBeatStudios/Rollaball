using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [Header("Pause Menu UI")]
    [SerializeField] private GameObject pauseMenuUI;

    private bool isPaused = false;
    private PlayerInput playerInput;
    private MusicCountdownUI musicCountdownUI;

    private void Awake()
    {
        playerInput = FindAnyObjectByType<PlayerInput>();
        musicCountdownUI = FindAnyObjectByType<MusicCountdownUI>();

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);
    }

    private void OnEnable()
    {
        if (playerInput != null)
            playerInput.actions["Pause"].performed += OnPausePerformed;
    }

    private void OnDisable()
    {
        if (playerInput != null)
            playerInput.actions["Pause"].performed -= OnPausePerformed;
    }

    private void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
            return;

        if (isPaused) Resume();
        else Pause();
    }

    private void Pause()
    {
        StartCoroutine(PauseSequence());
    }

    private IEnumerator PauseSequence()
    {
        Time.timeScale = 0f;
        isPaused = true;

        if (musicCountdownUI != null)
            musicCountdownUI.PauseTimer(true);

        yield return null; // Allow TMP/Canvas to rebuild

        pauseMenuUI.SetActive(true);

        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        if (musicCountdownUI != null)
            musicCountdownUI.PauseTimer(false);

        // Re-lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        isPaused = false;
        // SceneLoader.Instance.LoadScene("AshenPeaks");
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        // SceneLoader.Instance.LoadScene("MainMenu");
    }
}
