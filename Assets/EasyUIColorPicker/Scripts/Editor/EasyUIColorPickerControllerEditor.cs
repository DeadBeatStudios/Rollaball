using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace EasyUIColorPicker
{

    [CustomEditor(typeof(EasyUIColorPickerController))]
    public class EasyUIColorPickerControllerEditor : Editor
    {
        private GUIContent interactableLabel;

        private void Awake()
        {
            interactableLabel = new GUIContent("Interactable", "Can this color selector be interacted with?");
        }

        public override void OnInspectorGUI()
        {
            var picker = target as EasyUIColorPickerController;


            picker.interactable = EditorGUILayout.Toggle(interactableLabel, picker.interactable);

            base.OnInspectorGUI();
        }

    }

}
