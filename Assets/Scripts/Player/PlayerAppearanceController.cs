using UnityEngine;

public class PlayerAppearanceController : MonoBehaviour
{
    [Header("Visual References")]
    [SerializeField] private Renderer visualRenderer;

    private Material[] runtimeMaterials;
    private Color appliedColor = Color.white;

    public Color AppliedColor => appliedColor;

    private void Awake()
    {
        if (visualRenderer == null)
        {
            visualRenderer = GetComponentInChildren<Renderer>(true);

            if (visualRenderer == null)
            {
                Debug.LogError($"❌ {name}: No Renderer found.");
                return;
            }
        }

        runtimeMaterials = visualRenderer.materials;

        ApplyProfileColor();
    }

    public void ApplyProfileColor()
    {
        ApplyColor(PlayerProfile.SelectedColor);
    }

    public void ApplyColor(Color newColor)
    {
        if (runtimeMaterials == null || runtimeMaterials.Length == 0)
            return;

        appliedColor = newColor;

        foreach (Material mat in runtimeMaterials)
        {
            if (mat == null) continue;

            mat.color = appliedColor;
        }
    }
}