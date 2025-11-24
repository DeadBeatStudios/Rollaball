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

    private float musicLength = 0f;       // Full duration of the music
    private float timeRemaining = 0f;     // Time left in the countdown
    private bool isPaused = false;        // Pause status
    private bool countdownActive = false; // Prevent logic before audio is ready

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

        // Initialize values
        musicLength = musicSource.clip.length;
        timeRemaining = musicLength;

        // Start UI immediately, audio may start shortly after
        countdownActive = true;

        UpdateTimeDisplay();
    }

    private void Update()
    {
        if (!countdownActive || isPaused)
            return;

        // Use unscaled delta time so countdown continues even if timescale changes
        timeRemaining -= Time.unscaledDeltaTime;

        // Clamp
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
        if (timeText == null)
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
            // Pause audio safely
            if (musicSource != null && musicSource.isPlaying)
                musicSource.Pause();
        }
        else
        {
            // Resume audio
            if (musicSource != null && !musicSource.isPlaying && timeRemaining > 0f)
                musicSource.Play();
        }
    }

    private void OnCountdownFinished()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Stop();

        if (triggerGameOverOnFinish)
        {
            Debug.Log("⏳ Countdown finished — triggering Game Over...");
            // Hook into your future GameOverManager or UI transition here.
        }
    }
}
