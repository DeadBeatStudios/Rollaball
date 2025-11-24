using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleGameOverUI : MonoBehaviour
{
    public static SimpleGameOverUI Instance;

    [Header("UI Elements")]
    public GameObject gameOverPanel;

    private void Awake()
    {
        Instance = this;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    public void ShowGameOver()
    {
        if (gameOverPanel == null)
        {
            Debug.LogError("GameOver Panel is missing!");
            return;
        }

        gameOverPanel.SetActive(true);

        // Re-enable cursor for selection
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
