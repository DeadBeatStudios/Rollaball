using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CinemachineOrbitalFollow))]
public class CameraZoom : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float zoomSpeed = 2f;
    public float minDistance = 2f;
    public float maxDistance = 10f;

    [Tooltip("Reference to your Input Action for zoom (usually Mouse Scroll)")]
    public InputActionReference zoomAction;

    private CinemachineOrbitalFollow orbitalFollow;

    void Awake()
    {
        orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
    }

    void Update()
    {
        if (zoomAction == null || orbitalFollow == null) return;

        float zoomInput = zoomAction.action.ReadValue<float>();
        if (Mathf.Abs(zoomInput) > 0.01f)
        {
            float newRadius = orbitalFollow.Radius - zoomInput * zoomSpeed * Time.deltaTime * 100f;
            orbitalFollow.Radius = Mathf.Clamp(newRadius, minDistance, maxDistance);
        }
    }
}
