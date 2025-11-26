using UnityEngine;
using System.Linq;

public class FlagPickup : MonoBehaviour
{
    public enum FlagDropCause { KilledByEnemy, SelfDestruct, FellOffMap, Unknown }

    [Header("Attachment Settings")]
    [SerializeField] private float attachHeightOffset = 1.5f;

    [Header("Respawn Settings")]
    [SerializeField] private float respawnHeightOffset = 0.5f;
    [SerializeField, Range(0f, 0.5f)] private float edgePaddingPercent = 0.05f;

    [Header("Terrain Spawn Validation")]
    [SerializeField, Range(0f, 90f)] private float maxSpawnSlope = 30f;
    [SerializeField] private int maxSpawnAttempts = 10;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private Transform holder;
    private bool isHeld = false;
    private Quaternion initialWorldRotation;

    private Bounds cachedBounds;

    private void Start()
    {
        initialWorldRotation = transform.rotation;

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        cachedBounds = GetGroundBounds();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isHeld) return;
        if (!other.CompareTag("Player") && !other.CompareTag("Enemy")) return;

        AttachToHolder(other.transform);
    }

    private void LateUpdate()
    {
        if (!isHeld || holder == null) return;

        // Holder died?
        PlayerRespawn pr = holder.GetComponentInParent<PlayerRespawn>();
        if (pr != null && pr.IsDead)
        {
            RespawnAtRandom();
            return;
        }

        // Update position
        transform.position = holder.position + Vector3.up * attachHeightOffset;
        transform.rotation = initialWorldRotation;
    }

    // Attach to a player/enemy
    public void AttachToHolder(Transform newHolder)
    {
        holder = newHolder;
        isHeld = true;

        if (TryGetComponent(out Collider col))
            col.enabled = false;

        transform.position = newHolder.position + Vector3.up * attachHeightOffset;
        transform.rotation = initialWorldRotation;

        if (showDebugLogs)
            Debug.Log($"🏁 Flag attached to {newHolder.name}");
    }

    // Drop from knockback impact to world
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

        Vector3 basePos = dropPos ?? prevHolder.position;
        float groundY = GetGroundHeight(basePos);
        Vector3 finalPos = new Vector3(basePos.x, groundY + respawnHeightOffset, basePos.z);

        transform.position = finalPos;
        transform.rotation = initialWorldRotation;

        if (showDebugLogs)
            Debug.Log($"🏁 Flag dropped on ground at {finalPos}");
    }

    // Only for scoring & death
    public void DropAndRespawn(FlagDropCause cause = FlagDropCause.Unknown)
    {
        RespawnAtRandom();
    }

    private void RespawnAtRandom()
    {
        isHeld = false;
        holder = null;

        if (TryGetComponent(out Collider col))
            col.enabled = true;

        Vector3 point = GetRandomPointInArena();
        transform.position = point + Vector3.up * respawnHeightOffset;
        transform.rotation = initialWorldRotation;

        if (showDebugLogs)
            Debug.Log($"🏁 Flag respawned randomly at {transform.position}");
    }

    // --- Ground helpers ---
    private Bounds GetGroundBounds()
    {
        // Priority: Terrain
        if (Terrain.activeTerrain != null)
        {
            Terrain t = Terrain.activeTerrain;
            Bounds b = t.terrainData.bounds;
            b.center += t.transform.position;
            return b;
        }

        // Fallback: all Ground-tagged objects
        var cols = GameObject.FindGameObjectsWithTag("Ground")
            .Select(g => g.GetComponent<Collider>())
            .Where(c => c != null)
            .ToArray();

        Bounds combined = cols[0].bounds;
        for (int i = 1; i < cols.Length; i++)
            combined.Encapsulate(cols[i].bounds);

        return combined;
    }

    private float GetGroundHeight(Vector3 world)
    {
        if (Terrain.activeTerrain != null)
            return Terrain.activeTerrain.SampleHeight(world) + Terrain.activeTerrain.transform.position.y;

        if (Physics.Raycast(world + Vector3.up * 10f, Vector3.down, out var hit, 50f, LayerMask.GetMask("Ground")))
            return hit.point.y;

        return 0f;
    }

    private Vector3 GetRandomPointInArena()
    {
        Bounds a = cachedBounds;
        float px = a.extents.x * edgePaddingPercent;
        float pz = a.extents.z * edgePaddingPercent;

        float rx = Random.Range(a.min.x + px, a.max.x - px);
        float rz = Random.Range(a.min.z + pz, a.max.z - pz);
        float ry = GetGroundHeight(new Vector3(rx, a.center.y, rz));

        return new Vector3(rx, ry, rz);
    }

    public bool IsHeldBy(Transform t)
        => isHeld && holder != null && (holder == t || holder.IsChildOf(t));

    public Transform CurrentHolder => holder;
    public bool IsHeld => isHeld;

    public int CurrentHolderID
    {
        get
        {
            if (holder == null) return -1;

            PlayerScore score = holder.GetComponent<PlayerScore>();
            return score != null ? score.ID : -1;
        }
    }

}
