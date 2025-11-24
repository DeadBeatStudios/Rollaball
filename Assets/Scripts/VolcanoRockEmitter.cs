using UnityEngine;

public class VolcanoRockEmitter : MonoBehaviour
{
    [Header("Rock Prefab (Auto-loaded)")]
    [SerializeField] private GameObject rockPrefab;

    [Header("Rock Spawning")]
    [SerializeField] private float spawnInterval = 2f;

    [Header("Launch Force")]
    [SerializeField] private float spawnForce = 5f;
    [SerializeField] private Vector3 randomPush = new Vector3(1f, 0f, 1f);

    [Header("Rock Size Randomization")]
    [SerializeField] private float minRockSize = 5f;
    [SerializeField] private float maxRockSize = 10f;

    private void Awake()
    {
        // 🔥 Auto-load prefab from Resources root
        if (rockPrefab == null)
        {
            rockPrefab = Resources.Load<GameObject>("VolcanoRock");

            if (rockPrefab == null)
                Debug.LogError("VolcanoRockEmitter: Could not load VolcanoRock prefab! " +
                               "Place VolcanoRock.prefab in Assets/Prefabs/Resources/");
        }
    }

    private void Start()
    {
        InvokeRepeating(nameof(SpawnRock), spawnInterval, spawnInterval);
    }

    private void SpawnRock()
    {
        if (rockPrefab == null)
            return;

        GameObject rock = Instantiate(rockPrefab, transform.position, Quaternion.identity);

        // 🔥 Random scale
        float scale = Random.Range(minRockSize, maxRockSize);
        rock.transform.localScale = Vector3.one * scale;

        // 🔥 Physics force
        if (rock.TryGetComponent(out Rigidbody rb))
        {
            rb.mass = scale;

            Vector3 push =
                transform.up * spawnForce +
                new Vector3(
                    Random.Range(-randomPush.x, randomPush.x),
                    0f,
                    Random.Range(-randomPush.z, randomPush.z)
                );

            rb.AddForce(push, ForceMode.Impulse);
        }
    }
}
