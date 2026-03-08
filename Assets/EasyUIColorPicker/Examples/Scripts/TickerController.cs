using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EasyUIColorPicker;

namespace Examples_EasyUIColorPicker
{

    public class TickerController : MonoBehaviour
    {
        public ScrollRect ScrollView;
        public Text ScrollText;
        public InputField ScrollTextInput;
        public float Speed = 300;

        public Button TextColorButton;
        public Button BackgroundColorButton;

        public EasyUIColorPickerController TextColorPicker;
        public EasyUIColorPickerController BackgroundColorPicker;

        private RectTransform _textRect;
        private Transform _tickerTransform;

        private Image _textColorImage;
        private Image _backgroundColorImage;

        private void Start()
        {
            _textRect = ScrollText.GetComponent<RectTransform>();
            _tickerTransform = ScrollView.content.transform;

            _textColorImage = TextColorButton.GetComponent<Image>();
            _backgroundColorImage = BackgroundColorButton.GetComponent<Image>();

            ScrollText.text = ScrollTextInput.text;
            SetScrollerColors();
        }

        private void SetScrollerColors()
        {
            ScrollText.color = _textColorImage.color;

            ScrollView.GetComponent<Image>().color = _backgroundColorImage.color;
        }

        private void Update()
        {
            if (_tickerTransform.position.x + _textRect.rect.width < 0)
            {
                _tickerTransform.Translate(Vector3.right * (_tickerTransform.position.x * -1 + Screen.width));
            }

            _tickerTransform.Translate(Vector3.left * Time.deltaTime * Speed);
        }

        public void TextColorButtonClicked()
        {
            TextColorButton.interactable = false;
            TextColorPicker.Show(_textColorImage.color);
        }

        public void TextColorSelected(Color color)
        {
            _textColorImage.color = color;
            TextColorPicker.Close();
            TextColorButton.interactable = true;

            SetScrollerColors();
        }

        public void BackgroundColorButtonClicked()
        {
            BackgroundColorButton.interactable = false;
            BackgroundColorPicker.Show(_backgroundColorImage.color);
        }

        public void BackgroundColorSelected(Color color)
        {
            _backgroundColorImage.color = color;
            BackgroundColorPicker.Close();
            BackgroundColorButton.interactable = true;

            SetScrollerColors();
        }

        public void InputTextChanged(string text)
        {
            ScrollText.text = text;
        }
    }

}