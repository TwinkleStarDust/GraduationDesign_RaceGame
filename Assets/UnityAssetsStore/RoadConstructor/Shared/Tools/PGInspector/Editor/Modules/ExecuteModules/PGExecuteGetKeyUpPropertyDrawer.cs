﻿// ---------------------------------------------------
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ---------------------------------------------------

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace PampelGames.Shared.Tools.PGInspector.Editor
{
    [CustomPropertyDrawer(typeof(PGExecuteGetKeyUp))]
    public class PGExecuteGetKeyUpPropertyDrawer : PropertyDrawer
    {
        private SerializedProperty keyCodeProperty;
        private readonly EnumField keyCode = new("Key");


        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();

            FindAndBindProperties(property);
            DrawExecute(property);

            container.Add(keyCode);
            return container;
        }

        private void FindAndBindProperties(SerializedProperty property)
        {
            keyCodeProperty = property.FindPropertyRelative(nameof(PGExecuteGetKeyUp.keyCode));
            keyCode.BindProperty(keyCodeProperty);
        }


        /********************************************************************************************************************************/

        private void DrawExecute(SerializedProperty property)
        {
        }
    }
}
#endif