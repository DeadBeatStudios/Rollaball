using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class DebugController : MonoBehaviour
{
    [Header("Unity Events Hooked to DebugManager")]
    public UnityEvent OnSpawnChaser;
    public UnityEvent OnSpawnDefender;
    public UnityEvent OnSpawnFlagChaser;
    public UnityEvent OnResetEnemies;
    public UnityEvent OnResetFlag;
    public UnityEvent OnTeleportPlayer;
    public UnityEvent OnToggleAI;

    // Called by PlayerInput automatically
    public void SpawnChaser(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            OnSpawnChaser?.Invoke();
    }

    public void SpawnDefender(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            OnSpawnDefender?.Invoke();
    }

    public void SpawnFlagChaser(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            OnSpawnFlagChaser?.Invoke();
    }

    public void ResetEnemies(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            OnResetEnemies?.Invoke();
    }

    public void ResetFlag(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            OnResetFlag?.Invoke();
    }

    public void TeleportPlayer(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            OnTeleportPlayer?.Invoke();
    }

    public void ToggleAI(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            OnToggleAI?.Invoke();
    }
}
