using System.Collections;
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn Settings")]
    [Tooltip("Assign one or more spawn points here")]
    public Transform[] spawnPoints;
    public float fallThreshold = -10f;
    public float respawnDelay = 1.5f;

    [Header("Death Effects")]
    [Tooltip("Spawns chunk explosion & swaps player model")]
    [SerializeField] private ChunkExplosionSpawner explosionSpawner;

    private Rigidbody rb;
    private bool isRespawning = false;

    // NEW — allows Flag to detect Holder death reliably
    public bool IsDead { get; private set; } = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            GameObject defaultSpawn = new GameObject("DefaultSpawnPoint");
            defaultSpawn.transform.position = transform.position;
            spawnPoints = new Transform[] { defaultSpawn.transform };
        }
    }

    private void Update()
    {
        if (!isRespawning && transform.position.y < fallThreshold)
        {
            HandleDeath(FlagPickup.FlagDropCause.FellOffMap);
        }
    }

    public void HandleDeath(FlagPickup.FlagDropCause cause = FlagPickup.FlagDropCause.Unknown, Transform killer = null)
    {
        if (isRespawning) return;

        IsDead = true;   // <-- CRITICAL FIX

        if (explosionSpawner != null)
            explosionSpawner.SpawnChunkExplosion();

        StartCoroutine(RespawnSequence(cause, killer));
    }

    private IEnumerator RespawnSequence(FlagPickup.FlagDropCause cause, Transform killer)
    {
        isRespawning = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Debug.Log($"{gameObject.name} has died (cause: {cause}).");

        yield return new WaitForSeconds(respawnDelay);
        Respawn();
    }

    private void Respawn()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Vector3 safePosition = spawn.position + Vector3.up * 0.5f;

        if (Physics.Raycast(safePosition, Vector3.down, out RaycastHit hit, 2f))
        {
            safePosition = hit.point + Vector3.up * 0.5f;
        }

        transform.position = safePosition;
        transform.rotation = spawn.rotation;

        if (explosionSpawner != null)
            explosionSpawner.RestorePlayerModel();

        IsDead = false;   // <-- Reset after respawn

        Debug.Log($"{gameObject.name} respawned at {spawn.position}.");

        isRespawning = false;
    }
}
