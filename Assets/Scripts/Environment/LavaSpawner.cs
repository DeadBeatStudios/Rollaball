using UnityEngine;

public class LavaSpawner : MonoBehaviour
{
    [Header("Lava Prefab")]
    [SerializeField] private GameObject lavaBallPrefab;

    [Header("Emission")]
    [SerializeField] private int lavaCount = 1;
    [SerializeField] private float spawnRadius = 0.3f;
    [SerializeField] private Vector2 lavaScaleRange = new Vector2(0.8f, 1.2f);

    [Header("Target")]
    [SerializeField] private Transform target;

    public void Emit()
    {
        for (int i = 0; i < lavaCount; i++)
        {
            Vector3 offset = Random.insideUnitSphere * spawnRadius;
            offset.y = 0f;

            GameObject lava = Instantiate(
                lavaBallPrefab,
                transform.position + offset,
                Quaternion.identity
            );

            float scale = Random.Range(lavaScaleRange.x, lavaScaleRange.y);
            lava.transform.localScale *= scale;

            LavaBall lavaBall = lava.GetComponent<LavaBall>();
            lavaBall.Initialize(target.position);
        }
    }

    private void Start()
    {
        Emit();
    }
}
