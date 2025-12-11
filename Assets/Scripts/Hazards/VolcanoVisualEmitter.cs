using UnityEngine;

public class VolcanoVisualEmitter : MonoBehaviour
{
    [Header("Emission Settings")]
    [SerializeField] private float spawnInterval = 0.75f;
    [SerializeField] private Vector2 rockScaleRange = new Vector2(3f, 6f);

    [Header("Scatter Settings")]
    [SerializeField] private float spawnRadius = 1.5f;       // how wide around the emitter they appear
    [SerializeField] private float scatterAngle = 45f;       // cone spread angle
    [SerializeField] private float launchForce = 7f;         // total impulse of the visual launch

    private GameObject visualRockPrefab;
    private float timer = 0f;

    private void Awake()
    {
        // Load correct prefab from Resources folder
        visualRockPrefab = Resources.Load<GameObject>("VolcanoRock_Visual");

        if (visualRockPrefab == null)
            Debug.LogError("❌ VolcanoVisualEmitter: Could not find VolcanoRock_Visual prefab in Resources folder!");
    }

    private void Update()
    {
        if (visualRockPrefab == null) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnVisualRock();
        }
    }

    private void SpawnVisualRock()
    {
        // 🔥 Randomized spawn location around emitter
        Vector3 offset = Random.insideUnitSphere * spawnRadius;
        offset.y = Mathf.Abs(offset.y) * 0.35f; // keep mostly above the ground

        Vector3 spawnPos = transform.position + offset;

        GameObject rock = Instantiate(visualRockPrefab, spawnPos, Random.rotation);

        // Random scale
        float scale = Random.Range(rockScaleRange.x, rockScaleRange.y);
        rock.transform.localScale = Vector3.one * scale;

        // 🌋 Randomized launch direction in a cone
        Vector3 baseUp = Vector3.up;

        // Random angle within scatter cone
        Vector3 randomDir = Quaternion.AngleAxis(
            Random.Range(-scatterAngle, scatterAngle),
            Random.insideUnitSphere
        ) * baseUp;

        randomDir.Normalize();

        // Add some outward direction
        if (rock.TryGetComponent(out Rigidbody rb))
        {
            rb.AddForce(randomDir * launchForce, ForceMode.Impulse);
        }
    }
}
