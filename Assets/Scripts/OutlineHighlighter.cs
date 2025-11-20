using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class OutlineHighlighter : MonoBehaviour
{
    [Header("Outline Setup")]
    [Tooltip("Material using SG_Outline_Unlit. Assigned as an extra material when highlighting.")]
    [SerializeField] private Material outlineMaterial;

    private Renderer targetRenderer;
    private Material[] originalMaterials;
    private bool isOutlined;

    private void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        originalMaterials = targetRenderer.materials;
    }

    public void EnableOutline()
    {
        if (isOutlined || outlineMaterial == null)
            return;

        // Copy current materials and append outline mat
        var current = targetRenderer.materials;
        var newMats = new Material[current.Length + 1];
        for (int i = 0; i < current.Length; i++)
            newMats[i] = current[i];

        newMats[newMats.Length - 1] = outlineMaterial;
        targetRenderer.materials = newMats;

        isOutlined = true;
    }

    public void DisableOutline()
    {
        if (!isOutlined)
            return;

        // Restore originals
        targetRenderer.materials = originalMaterials;
        isOutlined = false;
    }
}
