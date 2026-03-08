using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EasyUIColorPicker;

namespace Examples_EasyUIColorPicker
{

    public class CubeColor : MonoBehaviour
    {
        public GameObject Cube;
        public Button ChangeColorButton;
        public EasyUIColorPickerController EasyUIColorPicker;

        private Material _cubeMaterial;

        private void Start()
        {
            _cubeMaterial = Cube.GetComponent<Renderer>().material;
        }

        public void ShowColorPicker()
        {
            ChangeColorButton.interactable = false;
            EasyUIColorPicker.Show(_cubeMaterial.color); // Tries to find a matching color or a color near enough. EasyUIColorPicker doesn't show the whole RGB color space.
        }

        // Callback used for event "On Color Select (Color)". See Inspector for "EasyUIColorPicker" in the scene.
        public void ColorSelected(Color color)
        {
            _cubeMaterial.color = color;

            EasyUIColorPicker.Close();
            ChangeColorButton.interactable = true;
        }

        // Callback used for event "On Base Color Select (Color)". See Inspector for "EasyUIColorPicker" in the scene.
        public void BaseColorSelected(Color color)
        {
            Debug.Log("Base color: #" + ColorUtility.ToHtmlStringRGB(color));
        }
    }

}