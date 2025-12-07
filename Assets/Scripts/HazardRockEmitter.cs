using UnityEngine;

public class HazardRockEmitter : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 1.0f;
    [SerializeField] private Vector2 rockScaleRange = new Vector2(5f, 10f);

    [Header("Scatter Area")]
    [SerializeField] private Vector3 scatterSize = new Vector3(80f, 0f, 80f);

    private GameObject hazardRockPrefab;
    private float timer = 0f;

    private void Awake()
    {
        hazardRockPrefab = Resources.Load<GameObject>("Hazard_Rock_Fractured");

        if (hazardRockPrefab == null)
            Debug.LogError("❌ HazardRockEmitter: Could not find Hazard_Rock_Fractured prefab in Resources folder!");
    }

    private void Update()
    {
        if (hazardRockPrefab == null) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnHazardRock();
        }
    }

    private void SpawnHazardRock()
    {
        // Choose a point randomly within the scatter zone
        Vector3 randomOffset = new Vector3(
            Random.Range(-scatterSize.x / 2f, scatterSize.x / 2f),
            0f,
            Random.Range(-scatterSize.z / 2f, scatterSize.z / 2f)
        );

        Vector3 spawnPos = transform.position + randomOffset;

        GameObject rock = Instantiate(hazardRockPrefab, spawnPos, Quaternion.identity);

        // Random size
        float scale = Random.Range(rockScaleRange.x, rockScaleRange.y);
        rock.transform.localScale = Vector3.one * scale;
    }
}
