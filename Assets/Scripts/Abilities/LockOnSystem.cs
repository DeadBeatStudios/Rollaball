using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

/// <summary>
/// Lock-on targeting system compatible with Unity 6 CinemachineCamera.
/// Handles targeting, visual highlight, and camera snapping/damping override.
/// </summary>
public class LockOnSystem : MonoBehaviour
{
    [Header("Lock-On Settings")]
    [SerializeField] private float lockOnRange = 25f;
    [SerializeField] private float lockOnAngle = 60f;
    [SerializeField] private LayerMask targetLayers;

    [Header("Camera Tracking")]
    public bool enableCameraTracking = true;

    [Tooltip("0 = no snap, 1 = very aggressive snap")]
    [SerializeField, Range(0f, 1f)]
    private float lockOnSnapStrength = 0.8f;

    [Tooltip("Multiplier for camera damping while locked on (0.1 = very snappy).")]
    [SerializeField, Range(0.05f, 2f)]
    private float lockOnDampingMultiplier = 0.4f;

    [Header("References")]
    [SerializeField] private CinemachineCamera cineCamera;  // Unity 6 camera
    [SerializeField] private Transform cameraTransform;     // main camera for angle

    [Header("Visual Feedback")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float highlightIntensity = 2f;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    // Internal state
    private Transform currentTarget;

    private Renderer targetRenderer;
    private Material[] originalMaterials;
    private Material[] highlightMaterials;

    // Cached camera components and values
    private Transform originalLookAtTarget;
    private bool isCameraModified = false;

    private CinemachineRotationComposer rotationComposer; // 🔥 CACHED
    private Vector2 originalDamping; // 🔥 FIX: Vector2, not Vector3

    // 🔥 NEW: Input axis controller (disable during lock-on)
    private CinemachineInputAxisController axisController;

    // 🔥 NEW: Orbital follow (might need to disable during lock-on)
    private CinemachineOrbitalFollow orbitalFollow;

    public bool IsLocked => currentTarget != null;
    public Transform CurrentTarget => currentTarget;
    public Vector3 TargetPosition => currentTarget != null ? currentTarget.position : Vector3.zero;
    public Vector3 DirectionToTarget => currentTarget != null ? (currentTarget.position - transform.position).normalized : Vector3.zero;

    private void Start()
    {
        // Validate CinemachineCamera
        if (cineCamera == null)
        {
            Debug.LogError("LockOnSystem: CinemachineCamera is NOT assigned.", this);
            return;
        }

        // Validate or find main camera transform
        if (cameraTransform == null)
        {
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
            else
                Debug.LogError("LockOnSystem: No camera found for angle calculations!", this);
        }

        // 🔥 IMPROVEMENT: Cache RotationComposer and validate
        rotationComposer = cineCamera.GetComponent<CinemachineRotationComposer>();
        if (rotationComposer != null)
        {
            // 🔥 FIX: Damping is Vector2 in Unity 6 (X, Y only - no Z)
            originalDamping = rotationComposer.Damping;

            if (showDebug)
                Debug.Log($"LockOnSystem: Cached original damping {originalDamping}");
        }
        else
        {
            Debug.LogWarning("LockOnSystem: CinemachineRotationComposer not found. Camera damping control will be disabled.", this);
            // System still works for targeting, just won't adjust damping
        }

        // 🔥 NEW: Cache OrbitalFollow (might need to disable during lock-on)
        orbitalFollow = cineCamera.GetComponent<CinemachineOrbitalFollow>();
        if (orbitalFollow == null)
        {
            Debug.LogWarning("LockOnSystem: CinemachineOrbitalFollow not found. Position control might conflict with lock-on.", this);
        }

        // Cache original LookAt target
        originalLookAtTarget = cineCamera.Target.LookAtTarget;
    }

    private void Update()
    {
        if (IsLocked && !IsTargetStillValid(currentTarget))
        {
            UnlockTarget();
        }
    }

    private void LateUpdate()
    {
        // 🔥 NEW: Manual camera rotation toward locked target
        if (IsLocked && enableCameraTracking && currentTarget != null && cameraTransform != null)
        {
            ApplyManualCameraTracking();
        }
    }

    // ============================================================
    // INPUT
    // ============================================================

    public void OnLockOn(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            ToggleLockOn();
    }

    private void ToggleLockOn()
    {
        if (IsLocked)
            UnlockTarget();
        else
            TryLockOntoTarget();
    }

    // ============================================================
    // TARGETING
    // ============================================================

    private void TryLockOntoTarget()
    {
        Transform best = FindBestTarget();
        if (best == null)
        {
            if (showDebug)
                Debug.Log("LockOnSystem: No valid targets in range.");
            return;
        }

        LockOntoTarget(best);
    }

    private Transform FindBestTarget()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, lockOnRange, targetLayers);

        Transform best = null;
        float bestScore = float.MaxValue;

        foreach (Collider col in cols)
        {
            // 🔥 IMPROVEMENT: Component-based self-exclusion
            if (col.GetComponent<PlayerController>() != null)
            {
                if (showDebug)
                    Debug.Log($"LockOnSystem: Skipping controlled player: {col.name}");
                continue;
            }

            if (!col.CompareTag("Enemy") && !col.CompareTag("Player"))
                continue;

            Transform t = col.transform;

            // Angle check (in front of camera)
            if (cameraTransform == null)
                continue;

            Vector3 flatCamForward = cameraTransform.forward;
            flatCamForward.y = 0;
            flatCamForward.Normalize();

            Vector3 dirToTarget = (t.position - cameraTransform.position);
            dirToTarget.y = 0;
            dirToTarget.Normalize();

            float angle = Vector3.Angle(flatCamForward, dirToTarget);
            if (angle > lockOnAngle)
            {
                if (showDebug)
                    Debug.Log($"LockOnSystem: Target {t.name} outside angle: {angle}°");
                continue;
            }

            // Line of sight check
            if (!HasLineOfSight(t))
            {
                if (showDebug)
                    Debug.Log($"LockOnSystem: Target {t.name} blocked by obstacle");
                continue;
            }

            // Hybrid scoring: 60% distance + 40% angle
            float distance = Vector3.Distance(transform.position, t.position);
            float score = (distance / lockOnRange) * 0.6f + (angle / lockOnAngle) * 0.4f;

            if (score < bestScore)
            {
                bestScore = score;
                best = t;
            }
        }

        return best;
    }

    private bool HasLineOfSight(Transform target)
    {
        Vector3 dir = target.position - transform.position;

        if (Physics.Raycast(transform.position, dir.normalized, out RaycastHit hit, dir.magnitude))
        {
            if (hit.transform != target && !hit.transform.IsChildOf(target))
                return false;
        }

        return true;
    }

    private bool IsTargetStillValid(Transform t)
    {
        if (t == null)
            return false;

        if (!t.gameObject.activeInHierarchy)
            return false;

        if (Vector3.Distance(transform.position, t.position) > lockOnRange)
        {
            if (showDebug)
                Debug.Log("LockOnSystem: Target out of range");
            return false;
        }

        if (!HasLineOfSight(t))
        {
            if (showDebug)
                Debug.Log("LockOnSystem: Line of sight lost");
            return false;
        }

        return true;
    }

    // ============================================================
    // LOCK / UNLOCK
    // ============================================================

    private void LockOntoTarget(Transform t)
    {
        // 🔥 IMPROVEMENT: Always unlock before locking (prevents multiple locks)
        if (IsLocked)
            UnlockTarget();

        currentTarget = t;

        ApplyHighlight(t);
        ApplyCameraLockOn();

        if (showDebug)
            Debug.Log($"LockOnSystem: ✅ Locked onto {t.name}");
    }

    private void UnlockTarget()
    {
        if (!IsLocked)
            return;

        RemoveHighlight();
        RemoveCameraLockOn();

        if (showDebug)
            Debug.Log($"LockOnSystem: 🔓 Unlocked from {currentTarget.name}");

        currentTarget = null;
    }

    // ============================================================
    // CAMERA CONTROL — Unity 6 COMPLIANT
    // ============================================================

    /// <summary>
    /// Manually rotate camera toward locked target (bypasses Cinemachine rotation)
    /// </summary>
    private void ApplyManualCameraTracking()
    {
        if (currentTarget == null || cameraTransform == null)
            return;

        // Calculate direction to target
        Vector3 direction = currentTarget.position - cameraTransform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Apply rotation with smoothing based on snap strength
        float rotationSpeed = Mathf.Lerp(2f, 15f, lockOnSnapStrength);

        cameraTransform.rotation = Quaternion.Slerp(
            cameraTransform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }

    private void ApplyCameraLockOn()
    {
        if (!enableCameraTracking || cineCamera == null || currentTarget == null)
            return;

        // 🔥 NOTE: We're NOT setting LookAtTarget anymore
        // Manual rotation in LateUpdate() handles camera tracking
        // This method now only disables input and adjusts damping

        // 🔥 NEW: Disable manual camera input during lock-on
        if (axisController != null)
        {
            axisController.enabled = false;

            if (showDebug)
                Debug.Log("LockOnSystem: 📹 Manual camera input disabled (locked-on)");
        }

        // 🔥 NEW: Optionally disable OrbitalFollow during lock-on (if it fights manual rotation)
        // Uncomment these lines if camera doesn't rotate properly:
        // if (orbitalFollow != null)
        // {
        //     orbitalFollow.enabled = false;
        //     if (showDebug)
        //         Debug.Log("LockOnSystem: 📹 OrbitalFollow disabled during lock-on");
        // }

        // 🔥 IMPROVEMENT: Apply snappy damping (only if composer exists)
        if (rotationComposer != null)
        {
            // 🔥 FIX: Vector2, not Vector3
            Vector2 modifiedDamping = new Vector2(
                Mathf.Lerp(originalDamping.x, originalDamping.x * lockOnDampingMultiplier, lockOnSnapStrength),
                Mathf.Lerp(originalDamping.y, originalDamping.y * lockOnDampingMultiplier, lockOnSnapStrength)
            );

            rotationComposer.Damping = modifiedDamping;

            if (showDebug)
                Debug.Log($"LockOnSystem: 📹 Camera damping set to {modifiedDamping} (snap: {lockOnSnapStrength}, mult: {lockOnDampingMultiplier})");
        }

        isCameraModified = true;
    }

    private void RemoveCameraLockOn()
    {
        if (!enableCameraTracking || !isCameraModified || cineCamera == null)
            return;

        // 🔥 NOTE: No LookAtTarget to clear (we use manual rotation)

        // 🔥 NEW: Re-enable manual camera input
        if (axisController != null)
        {
            axisController.enabled = true;

            if (showDebug)
                Debug.Log("LockOnSystem: 📹 Manual camera input re-enabled");
        }

        // 🔥 NEW: Re-enable OrbitalFollow if it was disabled
        // Uncomment if you uncommented the disable code above:
        // if (orbitalFollow != null)
        // {
        //     orbitalFollow.enabled = true;
        //     if (showDebug)
        //         Debug.Log("LockOnSystem: 📹 OrbitalFollow re-enabled");
        // }

        // 🔥 IMPROVEMENT: Restore original damping (only if composer exists)
        if (rotationComposer != null)
        {
            rotationComposer.Damping = originalDamping;

            if (showDebug)
                Debug.Log($"LockOnSystem: 📹 Camera damping restored to {originalDamping}");
        }

        isCameraModified = false;
    }

    // ============================================================
    // VISUAL HIGHLIGHT
    // ============================================================

    private void ApplyHighlight(Transform target)
    {
        targetRenderer = target.GetComponentInChildren<Renderer>();
        if (targetRenderer == null)
        {
            Debug.LogWarning($"LockOnSystem: No Renderer found on {target.name}", this);
            return;
        }

        // 🔥 IMPROVEMENT: Store original materials
        originalMaterials = targetRenderer.materials;
        highlightMaterials = new Material[originalMaterials.Length];

        // 🔥 IMPROVEMENT: Respect Inspector color with full alpha
        Color emiss = new Color(
            highlightColor.r,
            highlightColor.g,
            highlightColor.b,
            1f // Force full alpha for emission
        ) * highlightIntensity;

        for (int i = 0; i < originalMaterials.Length; i++)
        {
            highlightMaterials[i] = new Material(originalMaterials[i]);
            highlightMaterials[i].EnableKeyword("_EMISSION");
            highlightMaterials[i].SetColor("_EmissionColor", emiss);
            highlightMaterials[i].globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        targetRenderer.materials = highlightMaterials;

        if (showDebug)
            Debug.Log($"LockOnSystem: Applied highlight {highlightColor} (intensity: {highlightIntensity})");
    }

    private void RemoveHighlight()
    {
        if (targetRenderer != null && originalMaterials != null)
        {
            targetRenderer.materials = originalMaterials;

            // 🔥 IMPROVEMENT: Properly destroy duplicated materials
            if (highlightMaterials != null)
            {
                foreach (var m in highlightMaterials)
                {
                    if (m != null)
                        Destroy(m);
                }
            }
        }

        targetRenderer = null;
        originalMaterials = null;
        highlightMaterials = null;
    }

    private void OnDisable()
    {
        UnlockTarget();
    }

    private void OnDestroy()
    {
        UnlockTarget();
    }

    private void OnDrawGizmosSelected()
    {
        // Draw lock-on range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lockOnRange);

        // Draw camera angle cone
        if (cameraTransform != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 forward = cameraTransform.forward;
            forward.y = 0;
            forward.Normalize();

            Quaternion leftRot = Quaternion.Euler(0, -lockOnAngle, 0);
            Quaternion rightRot = Quaternion.Euler(0, lockOnAngle, 0);

            Vector3 leftBoundary = leftRot * forward * lockOnRange;
            Vector3 rightBoundary = rightRot * forward * lockOnRange;

            Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
            Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        }

        // Draw line to current target
        if (IsLocked && Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
            Gizmos.DrawWireSphere(currentTarget.position, 1f);
        }
    }
}