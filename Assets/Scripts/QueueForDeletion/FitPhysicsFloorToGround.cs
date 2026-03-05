using UnityEngine;

[ExecuteAlways] // also runs in Edit Mode so you can see it fit live
public class FitPhysicsFloorToGround : MonoBehaviour
{
    [Header("Assign your visible Ground's MeshRenderer")]
    public MeshRenderer groundRenderer;

    [Header("This object's BoxCollider (on PhysicsFloor)")]
    public BoxCollider box;

    [Header("Tuning")]
    [Tooltip("How thick the physics floor is (meters).")]
    public float yThickness = 0.1f;
    [Tooltip("Inset the edges so spawns never sit on the exact edge.")]
    [Range(0f, 0.2f)] public float insetPercent = 0.02f; // 2% per side
    [Tooltip("Lift the floor up or down relative to the ground surface.")]
    public float topOffset = 0f;

    void OnValidate() { FitNow(); }
    void Update() { if (!Application.isPlaying) FitNow(); }

    public void FitNow()
    {
        if (groundRenderer == null || box == null) return;

        var b = groundRenderer.bounds;

        // Inset XZ to keep spawns inside edges
        float insetX = b.size.x * insetPercent;
        float insetZ = b.size.z * insetPercent;

        // Position this collider so its TOP sits at the ground's top surface
        float topY = b.max.y + topOffset;
        float centerY = topY - (yThickness * 0.5f);

        transform.position = new Vector3(b.center.x, centerY, b.center.z);
        transform.rotation = Quaternion.identity; // flat
        transform.localScale = Vector3.one;

        box.center = Vector3.zero;
        box.size = new Vector3(b.size.x - insetX * 2f, yThickness, b.size.z - insetZ * 2f);
    }
}
