using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class RollingSound : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource rollingAudio;

    [Header("Settings")]
    [SerializeField] private float speedThreshold = 0.5f;      // minimum speed to start sound
    [SerializeField] private float maxSpeedForVolume = 12f;    // speed at which volume = 1
    [SerializeField] private string enabledSceneName = "Fire_AshenPeaksLevel";

    private Rigidbody rb;
    private bool sceneEnabled = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // If no AudioSource manually assigned, try to grab one on this GameObject
        if (rollingAudio == null)
        {
            rollingAudio = GetComponent<AudioSource>();
        }
    }

    private void Start()
    {
        // Only active on this scene
        sceneEnabled = SceneManager.GetActiveScene().name == enabledSceneName;

        if (!sceneEnabled && rollingAudio != null)
        {
            rollingAudio.Stop();
        }
    }

    private void Update()
    {
        if (!sceneEnabled || rollingAudio == null)
            return;

        // Use mostly horizontal speed so jumps don't spam audio
        Vector3 vel = rb.linearVelocity;
        float vertical = vel.y;
        vel.y = 0f;

        float speed = vel.magnitude;
        bool roughlyGrounded = Mathf.Abs(vertical) < 0.2f;

        if (speed > speedThreshold && roughlyGrounded)
        {
            if (!rollingAudio.isPlaying)
                rollingAudio.Play();

            // Normalize 0..1 based on speed
            float t = Mathf.Clamp01(speed / maxSpeedForVolume);

            // Volume & pitch scale with speed
            rollingAudio.volume = Mathf.Lerp(0.3f, 1f, t);
            rollingAudio.pitch = Mathf.Lerp(0.9f, 1.2f, t);
        }
        else
        {
            if (rollingAudio.isPlaying)
                rollingAudio.Stop();
        }
    }
}
