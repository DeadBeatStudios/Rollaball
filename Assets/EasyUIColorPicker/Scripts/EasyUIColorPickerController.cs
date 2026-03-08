/* Copyright by Stefan Scholl
 * https://www.stefan-scholl.net/
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EasyUIColorPicker
{

    [System.Serializable]
    public class EasyUIColorPickerEvent : UnityEvent<Color>
    {
    }

    public class EasyUIColorPickerController : MonoBehaviour
    {

        #region API Property

        [HideInInspector]
        public Color color = Color.black;

        [HideInInspector]
        public Color baseColor = Color.black;

        private bool _interactable = true;
        public bool interactable // Be aware that this property can get called in editor context!
        {
            get
            {
                return _interactable;
            }

            set
            {
                _interactable = value;

                NonInteractablePanel.SetActive(!_interactable);
            }
        }

        #endregion API Property


        #region Inspector

        public EasyUIColorPickerEvent OnColorSelect;
        public EasyUIColorPickerEvent OnBaseColorSelect;

        [Space(10)]
        [Header("No need to change")]

        public Image BaseColorSelectedBlack;
        public Image BaseColorSelectedWhite;
        public Image MainColorSelectedBlack;
        public Image MainColorSelectedWhite;

        public Image[] BaseColorImages;
        public Image[] MainColorImages;

        public GameObject NonInteractablePanel;

        #endregion Inspector


        private Color[,] _mainColors;

        private void Init()
        {
            _mainColors = new Color[BaseColorImages.Length, MainColorImages.Length];

            for (int baseIndex = 0; baseIndex < BaseColorImages.Length; baseIndex++)
            {
                var baseColor = BaseColorImages[baseIndex].color;
                var baseRGB = baseColor == Color.black ? Vector3.one : new Vector3(baseColor.r, baseColor.g, baseColor.b); // Start with white when base color is black

                for (int mainIndex = 0; mainIndex < MainColorImages.Length; mainIndex++)
                {
                    var mainRGB = baseRGB * (1f / (float)MainColorImages.Length) * (mainIndex + (baseColor == Color.black ? 0 : 1)); // Base color black? Then start with black.
                    _mainColors[baseIndex, mainIndex] = new Color(mainRGB.x, mainRGB.y, mainRGB.z);
                }
            }

        }

        private void OnEnable()
        {
            if (_mainColors == null) Setup(Color.black);
        }

        #region API Methods
        /// <summary>
        /// Displays the color selector.
        /// </summary>
        /// <param name="initColor">Color to preselect.</param>
        /// <remarks>The nearest RGB color is preselected if <paramref name="initColor"/> doesn't match any color.</remarks>
        public void Show(Color initColor)
        {
            Setup(initColor);
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Closes the color selector.
        /// </summary>
        public void Close()
        {
            gameObject.SetActive(false);
        }

        private float ColorDistanceSquared(Color c1, Color c2)
        {
            return Mathf.Pow((c1.r - c2.r), 2) + Mathf.Pow((c1.g - c2.g), 2) + Mathf.Pow((c1.b - c2.b), 2);
        }

        /// <summary>
        /// Selects the color <paramref name="initColor"/> and initializes the Easy UI Color Picker if needed.
        /// </summary>
        /// <param name="initColor">Color to preselect.</param>
        /// <remarks>The nearest (RGB) color is used as Easy UI Color Picker doesn’t provide all available colors.</remarks>
        public void Setup(Color initColor)
        {
            if (_mainColors == null) Init();

            int selectBaseColorIndex = -1, selectMainColorIndex = -1, baseNear = 0, mainNear = 0;

            float distSquared = float.MaxValue;

            for (int baseIndex = 0; baseIndex < BaseColorImages.Length; baseIndex++)
            {
                for (int mainIndex = 0; mainIndex < MainColorImages.Length; mainIndex++)
                {
                    if (initColor == _mainColors[baseIndex, mainIndex])
                    {
                        selectBaseColorIndex = baseIndex;
                        selectMainColorIndex = mainIndex;

                        break;
                    }

                    var dist = ColorDistanceSquared(_mainColors[baseIndex, mainIndex], initColor);

                    if (dist < distSquared)
                    {
                        distSquared = dist;
                        baseNear = baseIndex;
                        mainNear = mainIndex;
                    }
                }
            }

            if (selectBaseColorIndex == -1)
            {
                selectBaseColorIndex = baseNear;
                selectMainColorIndex = mainNear;
            }

            color = _mainColors[selectBaseColorIndex, selectMainColorIndex];
            baseColor = BaseColorImages[selectBaseColorIndex].color;

            BaseColorSelect(selectBaseColorIndex, selectMainColorIndex);
        }

        #endregion API Methods

        public void BaseColorSelect(int index)
        {
            BaseColorSelect(index, null);
        }

        public void BaseColorSelect(int index, int? mainColorIndex)
        {
            var selectedImage = BaseColorImages[index];

            baseColor = selectedImage.color;

            Image selectedMarker;
            if (selectedImage.color != Color.black)
            {
                BaseColorSelectedWhite.enabled = false;
                BaseColorSelectedBlack.enabled = true;
                selectedMarker = BaseColorSelectedBlack;
            }
            else
            {
                BaseColorSelectedBlack.enabled = false;
                BaseColorSelectedWhite.enabled = true;
                selectedMarker = BaseColorSelectedWhite;
            }
            MainColorSelectedWhite.enabled = false;
            MainColorSelectedBlack.enabled = false;

            selectedMarker.transform.position = selectedImage.transform.position;

            for (int i = 0; i < MainColorImages.Length; i++)
            {
                MainColorImages[i].color = _mainColors[index, i];
            }

            if (mainColorIndex.HasValue) // Setup
            {
                MainColorSelect(mainColorIndex.Value, true);
            }
            else
            {
                if (OnBaseColorSelect != null)
                {
                    OnBaseColorSelect.Invoke(baseColor);
                }
            }
        }

        public void MainColorSelect(int index)
        {
            MainColorSelect(index, false);
        }

        public void MainColorSelect(int index, bool setup = false)
        {
            var selectedImage = MainColorImages[index];

            Image selectedMarker;
            if (index > 1) // Lighter shade. Black marker visible.
            {
                MainColorSelectedWhite.enabled = false;
                MainColorSelectedBlack.enabled = true;
                selectedMarker = MainColorSelectedBlack;
            }
            else
            {
                MainColorSelectedBlack.enabled = false;
                MainColorSelectedWhite.enabled = true;
                selectedMarker = MainColorSelectedWhite;
            }

            selectedMarker.transform.position = selectedImage.transform.position;

            color = selectedImage.color;

            if (!setup && OnColorSelect != null)
            {
                OnColorSelect.Invoke(color);
            }
        }
    }

}

/* Copyright by Stefan Scholl
 * https://www.stefan-scholl.net/
 */