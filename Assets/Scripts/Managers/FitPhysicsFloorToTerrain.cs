using UnityEngine;

/// <summary>
/// Automatically resizes and aligns a PhysicsFloor object
/// to match the size and position of a specified Terrain.
/// Ideal for adapting BattleBallz-style floors to natural terrain maps.
/// </summary>
[ExecuteAlways] // Works in both Play Mode and Edit Mode
public class FitPhysicsFloorToTerrain : MonoBehaviour
{
    [Header("Assign the Terrain to follow")]
    [Tooltip("Drag your Terrain GameObject here.")]
    public Terrain targetTerrain;

    [Header("Offset Settings")]
    [Tooltip("Vertical offset to prevent z-fighting with terrain surface.")]
    public float heightOffset = 0.05f;

    [Tooltip("Scale multiplier for fine-tuning the PhysicsFloor size.")]
    public Vector3 scaleMultiplier = Vector3.one;

    private void Update()
    {
        if (targetTerrain == null)
            return;

        FitToTerrain();
    }

    private void FitToTerrain()
    {
        // Get terrain data and size
        TerrainData data = targetTerrain.terrainData;
        Vector3 terrainSize = data.size;
        Vector3 terrainPos = targetTerrain.transform.position;

        // Scale floor to match terrain X/Z
        Vector3 newScale = new Vector3(
            terrainSize.x * scaleMultiplier.x,
            scaleMultiplier.y, // keep Y scale adjustable
            terrainSize.z * scaleMultiplier.z
        );
        transform.localScale = newScale;

        // Center the floor over the terrain area
        Vector3 newPos = new Vector3(
            terrainPos.x + terrainSize.x / 2f,
            terrainPos.y + heightOffset,
            terrainPos.z + terrainSize.z / 2f
        );
        transform.position = newPos;
    }

#if UNITY_EDITOR
    // Optional: Draw bounds gizmo in the editor
    private void OnDrawGizmosSelected()
    {
        if (targetTerrain == null) return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
        Gizmos.DrawWireCube(transform.position, new Vector3(
            targetTerrain.terrainData.size.x,
            0.1f,
            targetTerrain.terrainData.size.z
        ));
    }
#endif
}
