using UnityEngine;

public class ColorPaletteButton : MonoBehaviour
{
    [Header("Palette Color")]
    [SerializeField] private Color buttonColor = Color.white;

    [Header("Preview Reference")]
    [SerializeField] private PlayerAppearanceController previewAppearance;

    public void SelectColor()
    {
        PlayerProfile.SelectedColor = buttonColor;

        if (previewAppearance != null)
        {
            previewAppearance.ApplyColor(buttonColor);
        }
        else
        {
            Debug.LogWarning($"⚠️ {name}: No PlayerAppearanceController assigned.");
        }
    }
}