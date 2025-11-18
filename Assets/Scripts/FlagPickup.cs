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

    [Header("Respawn Settings")]
    [SerializeField] private float respawnHeightOffset = 0.5f;
    [SerializeField] private float edgePaddingPercent = 0.05f;

    [Header("Safety")]
    [SerializeField] private float autoDropY = -5f;

    private Transform holder;
    private bool collected = false;
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
        // Only players or enemies can pick up
        if (!other.CompareTag("Player") && !other.CompareTag("Enemy"))
            return;

        if (collected) return;

        // Attach to visual model if present
        Transform visualRoot = other.transform.Find("VisualModel");
        holder = visualRoot != null ? visualRoot : other.transform;

        collected = true;

        if (TryGetComponent(out Collider col))
            col.enabled = false;

        transform.SetParent(holder, true);
        transform.position = holder.position + Vector3.up * attachHeightOffset;
        transform.rotation = initialWorldRotation;

        Debug.Log($"🏁 Flag collected by: {holder.name}");
    }

    private void LateUpdate()
    {
        if (!collected) return;

        // Drop if holder is null or dead
        if (holder == null || IsHolderDead())
        {
            DropAndRespawn(FlagDropCause.Unknown);
            return;
        }

        // Follow holder
        transform.position = holder.position + Vector3.up * attachHeightOffset;
        transform.rotation = initialWorldRotation;

        // If holder falls off map
        if (holder.position.y < autoDropY)
        {
            DropAndRespawn(FlagDropCause.FellOffMap);
        }
    }

    private bool IsHolderDead()
    {
        // PLAYER CHECK
        var player = holder.GetComponentInParent<PlayerRespawn>();
        if (player != null)
            return player.IsDead;

        // ENEMY CHECK — enemy dies by disabling GameObject
        if (!holder.gameObject.activeInHierarchy)
            return true;

        return false;
    }

    public void DropAndRespawn(FlagDropCause cause, Transform killer = null, Vector3? deathPosition = null)
    {
        transform.SetParent(null);
        collected = false;

        if (TryGetComponent(out Collider col))
            col.enabled = true;

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        transform.rotation = initialWorldRotation;

        Vector3 spawnPos;

        // Determine if the holder was a player
        bool holderIsPlayer = holder != null && holder.CompareTag("Player");

        switch (cause)
        {
            case FlagDropCause.KilledByEnemy:
                if (killer != null)
                {
                    // Transfer instantly to killer
                    Transform killerVisual = killer.Find("VisualModel");
                    holder = killerVisual != null ? killerVisual : killer;

                    collected = true;

                    transform.SetParent(holder, true);
                    transform.position = holder.position + Vector3.up * attachHeightOffset;
                    transform.rotation = initialWorldRotation;

                    Debug.Log($"🏁 Flag transferred to {holder.name}");
                    return;
                }
                spawnPos = GetRandomSpawn();
                break;

            case FlagDropCause.FellOffMap:
                // Player falling off map -> ALWAYS use safe random spawn
                spawnPos = GetRandomSpawn();
                break;

            case FlagDropCause.SelfDestruct:
            case FlagDropCause.Unknown:
                // Debug kill or unknown -> ALWAYS safe ground spawn
                spawnPos = GetRandomSpawn();
                break;

            default:
                spawnPos = GetRandomSpawn();
                break;
        }

        transform.position = spawnPos + Vector3.up * respawnHeightOffset;
        Debug.Log($"Flag respawned at {transform.position} due to {cause}");
    }

    private Vector3 GetRandomSpawn()
    {
        Bounds floorBounds = GetPhysicsFloorBounds();
        float padX = floorBounds.extents.x * edgePaddingPercent;
        float padZ = floorBounds.extents.z * edgePaddingPercent;

        float rx = Random.Range(floorBounds.min.x + padX, floorBounds.max.x - padX);
        float rz = Random.Range(floorBounds.min.z + padZ, floorBounds.max.z - padZ);
        float rayStartY = floorBounds.max.y + 5f;

        if (Physics.Raycast(new Vector3(rx, rayStartY, rz), Vector3.down, out RaycastHit hit, 10f, LayerMask.GetMask("Ground")))
            return hit.point;

        return new Vector3(rx, floorBounds.max.y, rz);
    }

    private Bounds GetPhysicsFloorBounds()
    {
        GameObject floor = GameObject.Find("PhysicsFloor");
        if (floor != null && floor.TryGetComponent(out BoxCollider col))
            return col.bounds;

        return new Bounds(Vector3.zero, new Vector3(10f, 1f, 8f));
    }

    public bool IsHeldBy(Transform t) => holder != null && holder == t;
    public Transform CurrentHolder => holder;
    public bool IsHeld => holder != null;
}
