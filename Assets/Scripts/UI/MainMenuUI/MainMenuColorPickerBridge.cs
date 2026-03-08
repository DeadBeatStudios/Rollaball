using UnityEngine;
using EasyUIColorPicker;

public class MainMenuColorPickerBridge : MonoBehaviour
{
    [Header("Picker Reference")]
    [SerializeField] private EasyUIColorPickerController colorPicker;

    [Header("Preview Reference")]
    [SerializeField] private PlayerAppearanceController previewAppearance;

    private void Start()
    {
        if (colorPicker == null)
        {
            Debug.LogError($"❌ {name}: No EasyUIColorPickerController assigned.");
            return;
        }

        if (previewAppearance == null)
        {
            Debug.LogError($"❌ {name}: No PlayerAppearanceController assigned for preview.");
            return;
        }

        // Sync picker UI to the currently stored player color
        colorPicker.Setup(PlayerProfile.SelectedColor);

        // Sync preview immediately too
        previewAppearance.ApplyColor(PlayerProfile.SelectedColor);

        // Listen for future color selections
        colorPicker.OnColorSelect.AddListener(HandleColorSelected);
    }

    private void OnDestroy()
    {
        if (colorPicker != null)
        {
            colorPicker.OnColorSelect.RemoveListener(HandleColorSelected);
        }
    }

    private void HandleColorSelected(Color selectedColor)
    {
        PlayerProfile.SelectedColor = selectedColor;
        previewAppearance.ApplyColor(selectedColor);
    }
}