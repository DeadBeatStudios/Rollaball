using System.Collections;
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn Settings")]
    [Tooltip("Assign one or more spawn points here")]
    public Transform[] spawnPoints;        // Multiple spawn options
    public float fallThreshold = -10f;     // Y height that triggers death
    public float respawnDelay = 1.5f;      // Delay before respawn

    private Rigidbody rb;
    private bool isRespawning = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // If no spawn points assigned, create one at start position
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            GameObject defaultSpawn = new GameObject("DefaultSpawnPoint");
            defaultSpawn.transform.position = transform.position;
            spawnPoints = new Transform[] { defaultSpawn.transform };
        }
    }

    private void Update()
    {
        // Detect death by falling below threshold
        if (!isRespawning && transform.position.y < fallThreshold)
        {
            HandleDeath(FlagPickup.FlagDropCause.FellOffMap);
        }
    }

    private void HandleDeath(FlagPickup.FlagDropCause cause = FlagPickup.FlagDropCause.Unknown, Transform killer = null)
    {
        if (isRespawning) return;

        StartCoroutine(RespawnSequence(cause, killer));
    }

    private IEnumerator RespawnSequence(FlagPickup.FlagDropCause cause, Transform killer)
    {
        isRespawning = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Debug.Log($"{gameObject.name} has died (cause: {cause}).");

        // 🔹 Find the flag
        FlagPickup flag = Object.FindAnyObjectByType<FlagPickup>();
        if (flag != null)
        {
            // ✅ Only the flag holder triggers a drop
            if (flag.IsHeldBy(transform))
            {
                flag.DropAndRespawn(cause, killer, transform.position);
                Debug.Log($"Flag dropped by {gameObject.name} due to {cause}");
            }
            else
            {
                Debug.Log($"{gameObject.name} died, but they weren’t holding the flag.");
            }
        }
        else
        {
            Debug.LogWarning("No FlagPickup found in the scene!");
        }

        yield return new WaitForSeconds(respawnDelay);
        Respawn();
    }

    private void Respawn()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // --- Pick random spawn point ---
        Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // --- Safe reposition ---
        Vector3 safePosition = spawn.position + Vector3.up * 0.5f;

        // Optional: snap onto terrain
        if (Physics.Raycast(safePosition, Vector3.down, out RaycastHit hit, 2f))
        {
            safePosition = hit.point + Vector3.up * 0.5f;
        }

        transform.position = safePosition;
        transform.rotation = spawn.rotation;

        Debug.Log($"{gameObject.name} respawned at {spawn.position}.");

        isRespawning = false;
    }
}
