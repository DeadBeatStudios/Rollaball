using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class RotateCameraHold : MonoBehaviour
{
    [Tooltip("Input action for holding Right Mouse Button")]
    public InputActionReference rotateHoldAction;

    private CinemachineRotationComposer rotationComposer;

    void Awake()
    {
        rotationComposer = GetComponent<CinemachineRotationComposer>();
    }

    void Update()
    {
        // Only allow camera rotation when RMB is held
        bool allowRotation = rotateHoldAction != null && rotateHoldAction.action.IsPressed();

        if (rotationComposer != null)
            rotationComposer.enabled = allowRotation;
    }
}
