using UnityEngine;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    public TMP_InputField nameInputField;
    public string nextSceneName = "LevelSelect";

    public void StartGame()
    {
        string input = nameInputField.text.Trim();

        PlayerProfile.PlayerName =
            string.IsNullOrEmpty(input) ? "Player" : input;

        // Load Level Select using SceneLoader
        SceneLoader loader = FindAnyObjectByType<SceneLoader>();
        if (loader != null)
            loader.LoadScene(nextSceneName);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }
}
