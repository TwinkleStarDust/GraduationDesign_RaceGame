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
    [CustomPropertyDrawer(typeof(PGStopGetKeyDown))]
    public class PGStopGetKeyDownPropertyDrawer : PropertyDrawer
    {
        private SerializedProperty keyCodeProperty;
        private readonly EnumField keyCode = new("Key");


        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();

            FindAndBindProperties(property);
            DrawStop(property);

            container.Add(keyCode);
            return container;
        }

        private void FindAndBindProperties(SerializedProperty property)
        {
            keyCodeProperty = property.FindPropertyRelative(nameof(PGStopGetKeyDown.keyCode));
            keyCode.BindProperty(keyCodeProperty);
        }


        /********************************************************************************************************************************/

        private void DrawStop(SerializedProperty property)
        {
        }
    }
}
#endif