using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EasyUIColorPicker;

namespace Examples_EasyUIColorPicker
{

    public class BallColor : MonoBehaviour
    {
        public EasyUIColorPickerController EasyUIColorPicker;

        private Material _mat;
        private void Start()
        {
            _mat = GetComponent<Renderer>().material;

            EasyUIColorPicker.OnColorSelect.AddListener(ColorSelected); // Setting the callback in code.
            EasyUIColorPicker.OnBaseColorSelect.AddListener(BaseColorSelected); // Setting the callback in code.

            EasyUIColorPicker.Setup(_mat.color);
        }

        public void ColorSelected(Color color)
        {
            _mat.color = color;
            SetOthersInteractable(true);
        }


        public void BaseColorSelected(Color color)
        {
            SetOthersInteractable(false);
        }

        private void SetOthersInteractable(bool interactable)
        {
            var others = FindObjectsOfType<EasyUIColorPickerController>();
            foreach (var other in others)
            {
                if (other == EasyUIColorPicker)
                {
                    continue;
                }

                other.interactable = interactable;
            }
        }

    }

}