using UnityEngine;
using TMPro;

public class MusicCountdownUI : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timeText;

    [Header("Game Over Trigger")]
    [SerializeField] private bool triggerGameOverOnFinish = true;

    private float musicLength = 0f;
    private float timeRemaining = 0f;
    private bool isPaused = false;
    private bool countdownActive = false;

    private void Awake()
    {
        // Auto-assign AudioSource
        if (musicSource == null)
            musicSource = FindAnyObjectByType<AudioSource>();

        // Auto-assign UI text
        if (timeText == null)
            timeText = GameObject.Find("MusicCountdownText")?.GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        if (musicSource == null)
        {
            Debug.LogError("MusicCountdownUI: No AudioSource assigned!");
            enabled = false;
            return;
        }

        if (timeText == null)
        {
            Debug.LogError("MusicCountdownUI: No UI Text assigned!");
            enabled = false;
            return;
        }

        if (musicSource.clip == null)
        {
            Debug.LogError("MusicCountdownUI: AudioSource has no clip assigned!");
            enabled = false;
            return;
        }

        musicLength = musicSource.clip.length;
        timeRemaining = musicLength;

        countdownActive = true;

        UpdateTimeDisplay();
    }

    private void Update()
    {
        if (!countdownActive || isPaused)
            return;

        // 🔥 STOP if the UI text was destroyed (prevents MissingReferenceException)
        if (timeText == null || !timeText)
            return;

        // Unscaled time keeps countdown running during paused Time.timeScale
        timeRemaining -= Time.unscaledDeltaTime;

        if (timeRemaining < 0f)
            timeRemaining = 0f;

        UpdateTimeDisplay();

        if (timeRemaining <= 0f)
        {
            countdownActive = false;
            OnCountdownFinished();
        }
    }

    private void UpdateTimeDisplay()
    {
        if (timeText == null || !timeText)
            return;

        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);

        timeText.text = $"{minutes:00}:{seconds:00}";
    }

    // Called by PauseMenu
    public void PauseTimer(bool paused)
    {
        isPaused = paused;

        if (paused)
        {
            if (musicSource != null && musicSource.isPlaying)
                musicSource.Pause();
        }
        else
        {
            if (musicSource != null && !musicSource.isPlaying && timeRemaining > 0f)
                musicSource.Play();
        }
    }

    private void OnCountdownFinished()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Stop();

        Debug.Log("⏳ Countdown finished — triggering Game Over...");

        if (triggerGameOverOnFinish && SimpleGameOverUI.Instance != null)
        {
            SimpleGameOverUI.Instance.ShowGameOver();
        }
    }
}
