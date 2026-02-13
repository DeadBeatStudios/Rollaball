using System.Collections;
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn Settings")]
    [Tooltip("Assign one or more spawn points here")]
    public Transform[] spawnPoints;

    [Tooltip("Y position below which the player dies")]
    public float fallThreshold = -10f;

    public float respawnDelay = 1.5f;

    [Header("Death Effects")]
    [SerializeField] private ChunkExplosionSpawner explosionSpawner;

    private Rigidbody rb;
    private bool isRespawning = false;

    /// <summary>
    /// Public death state so other systems (AI, Flag listeners, etc.) can react.
    /// </summary>
    public bool IsDead { get; private set; }

    // --------------------------------------------------------------
    //  SETUP
    // --------------------------------------------------------------

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

    // --------------------------------------------------------------
    //  FALL DEATH (LOCAL CHECK ONLY)
    // --------------------------------------------------------------

    private void Update()
    {
        if (!IsDead && transform.position.y < fallThreshold)
        {
            Kill(DeathEvents.DeathCause.FellOffMap);
        }
    }

    // --------------------------------------------------------------
    //  PUBLIC DEATH API (AUTHORITATIVE)
    // --------------------------------------------------------------

    /// <summary>
    /// Canonical death entry point (local).
    /// Prefer using DeathEvents.ReportDeath(...) from external systems.
    /// </summary>
    public void Kill(
        DeathEvents.DeathCause cause,
        GameObject instigator = null)
    {
        if (IsDead || isRespawning)
            return;

        IsDead = true;

        if (explosionSpawner != null)
            explosionSpawner.SpawnChunkExplosion();

        StartCoroutine(RespawnSequence(cause, instigator));
    }

    // --------------------------------------------------------------
    //  RESPAWN FLOW
    // --------------------------------------------------------------

    private IEnumerator RespawnSequence(
        DeathEvents.DeathCause cause,
        GameObject instigator)
    {
        isRespawning = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Debug.Log($"{name} died (cause: {cause})");

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
            safePosition = hit.point + Vector3.up * 0.5f;

        transform.position = safePosition;
        transform.rotation = spawn.rotation;

        if (explosionSpawner != null)
            explosionSpawner.RestorePlayerModel();

        IsDead = false;
        isRespawning = false;

        Debug.Log($"{name} respawned at {spawn.position}");
    }
}
