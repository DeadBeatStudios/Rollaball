using UnityEngine;

public class FlagPickup : MonoBehaviour
{
    public enum FlagDropCause
    {
        KilledByEnemy,
        SelfDestruct,
        FellOffMap,
        Unknown
    }

    [Header("Attachment Settings")]
    [SerializeField] private float attachHeightOffset = 1.5f;

    [Header("Respawn Zones")]
    [Tooltip("Drag in the FlagSpawner objects where the flag is allowed to respawn.")]
    public Transform[] respawnSpawners;

    [Tooltip("Height added above the spawner when respawning.")]
    public float respawnHeightOffset = 0.5f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    // Internal State
    private Transform holder;
    private bool isHeld = false;
    private Quaternion initialWorldRotation;

    private void Start()
    {
        initialWorldRotation = transform.rotation;

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isHeld)
            return;

        if (!other.CompareTag("Player") && !other.CompareTag("Enemy"))
            return;

        AttachToHolder(other.transform);
    }

    private void LateUpdate()
    {
        if (!isHeld || holder == null)
            return;

        // Follow holder only (death handling is event-driven via FlagDeathListener)
        transform.position = holder.position + Vector3.up * attachHeightOffset;
        transform.rotation = initialWorldRotation;
    }

    // -------------------------------------------------
    // ATTACH / DROP
    // -------------------------------------------------

    public void AttachToHolder(Transform newHolder)
    {
        if (newHolder == null)
            return;

        holder = newHolder;
        isHeld = true;

        if (TryGetComponent(out Collider col))
            col.enabled = false;

        transform.SetParent(null);
        transform.position = newHolder.position + Vector3.up * attachHeightOffset;
        transform.rotation = initialWorldRotation;

        if (showDebugLogs)
            Debug.Log($"Flag attached to {newHolder.name}");
    }

    public void DropToWorld(
        FlagDropCause cause = FlagDropCause.Unknown,
        Transform killer = null,
        Vector3? dropPos = null)
    {
        isHeld = false;
        Transform prevHolder = holder;
        holder = null;

        if (TryGetComponent(out Collider col))
            col.enabled = true;

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Vector3 basePos = dropPos ?? (prevHolder != null ? prevHolder.position : transform.position);
        Vector3 finalPos = basePos + Vector3.up * respawnHeightOffset;

        transform.position = finalPos;
        transform.rotation = initialWorldRotation;

        if (showDebugLogs)
            Debug.Log($"Flag dropped at {finalPos} (cause: {cause})");
    }

    /// <summary>
    /// Used by scoring and instant respawn events.
    /// </summary>
    public void DropAndRespawn()
    {
        RespawnAtRandomSpawner();
    }

    // -------------------------------------------------
    // RESPAWN ZONES
    // -------------------------------------------------

    public void RespawnAtSpawner(int index)
    {
        if (respawnSpawners == null || respawnSpawners.Length == 0)
        {
            Debug.LogError("FlagPickup: No respawn spawners assigned.");
            return;
        }

        index = Mathf.Clamp(index, 0, respawnSpawners.Length - 1);
        Transform sp = respawnSpawners[index];

        if (sp == null)
        {
            Debug.LogError($"FlagPickup: Spawner at index {index} is null.");
            return;
        }

        isHeld = false;
        holder = null;

        if (TryGetComponent(out Collider col))
            col.enabled = true;

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        transform.position = sp.position + Vector3.up * respawnHeightOffset;
        transform.rotation = initialWorldRotation;

        if (showDebugLogs)
            Debug.Log($"Flag respawned at spawner: {sp.name}");
    }

    public void RespawnAtRandomSpawner()
    {
        if (respawnSpawners == null || respawnSpawners.Length == 0)
        {
            Debug.LogError("FlagPickup: No respawn spawners assigned.");
            return;
        }

        int index = Random.Range(0, respawnSpawners.Length);
        RespawnAtSpawner(index);
    }

    // -------------------------------------------------
    // PUBLIC API
    // -------------------------------------------------

    public bool IsHeldBy(Transform t)
        => isHeld && holder != null && (holder == t || holder.IsChildOf(t));

    public Transform CurrentHolder => holder;
    public bool IsHeld => isHeld;

    /// <summary>
    /// Called by FlagDeathListener when the carrier dies.
    /// Keeps FlagPickup decoupled from PlayerRespawn / Enemy systems.
    /// </summary>
    public void OnCarrierDied(DeathEvents.DeathCause cause, GameObject instigator = null)
    {
        RespawnAtRandomSpawner();

        if (showDebugLogs)
            Debug.Log($"FlagPickup: Carrier died, flag respawned. Cause={cause}");
    }

    // -------------------------------------------------
    // FORCE RESET FLAG
    // -------------------------------------------------

    /// <summary>
    /// Forces the flag to reset immediately:
    /// - Detaches holder
    /// - Re-enables collider
    /// - Clears physics
    /// - Respawns at a random spawner
    /// </summary>
    public void ForceResetFlag()
    {
        // Reset held state
        isHeld = false;
        holder = null;

        // Restore collider
        if (TryGetComponent(out Collider col))
            col.enabled = true;

        // Reset physics
        if (TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Respawn the flag safely
        RespawnAtRandomSpawner();

        if (showDebugLogs)
            Debug.Log("FlagPickup: FORCE RESET");
    }
}
