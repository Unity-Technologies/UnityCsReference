// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.U2D.Physics.Editor
{
    [CustomPropertyDrawer(typeof(PhysicsMask))]
    sealed class PhysicsMaskPropertyDrawer : PropertyDrawer
    {
        // Shared single-line mask field used by the mask drawers: 64-bit layers use LayerMaskField64,
        // 32-bit layers use the native LayerMaskField. Reads/writes bitMaskProperty.ulongValue.
        internal static void DrawMaskField(Rect position, GUIContent label, SerializedProperty bitMaskProperty, bool showAsPhysicsMask)
        {
            if (showAsPhysicsMask || PhysicsWorld.usePhysicsLayers)
            {
                LayerMaskField64.Draw(position, label, bitMaskProperty, showAsPhysicsMask);
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                var newMask = EditorGUI.LayerMaskField(position, (uint)bitMaskProperty.ulongValue, label);
                if (EditorGUI.EndChangeCheck())
                    bitMaskProperty.ulongValue = newMask;
            }
        }

        #region UITK

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            const string bitMaskTypeName = nameof(PhysicsMask.bitMask);
            var bitMaskProperty = property.FindPropertyRelative(bitMaskTypeName);
            var bitMaskBindingPath = bitMaskTypeName;

            // Find if we need to show as a basic 64-bit mask.
            var showAsPhysicsMask = ShowAsPhysicsMask(property);

            // 64-bit mask.
            if (showAsPhysicsMask || PhysicsWorld.usePhysicsLayers)
            {
                var layerNames = new List<String>(capacity: 64);
                var layerMasks = new List<UInt64>(capacity: 64);

                // Are we showing as a physics mask?
                if (showAsPhysicsMask)
                {
                    // Yes, so set layer names and masks for each bit.
                    PhysicsLayers.GetBitNamesAndMasks(layerNames, layerMasks);
                }
                else
                {
                    // No, so get the layer names and masks.
                    PhysicsLayers.GetLayerNamesAndMasks(layerNames, layerMasks);
                }

                var categories = new Mask64Field(property.displayName, layerNames, bitMaskProperty.ulongValue)
                {
                    bindingPath = bitMaskBindingPath,
                    choicesMasks = layerMasks
                };
                categories.AddToClassList(Mask64Field.alignedFieldUssClassName);
                root.Add(categories);

                return root;
            }

            // 32-bit mask.
            {
                var categories = new LayerMaskField32(property.displayName) { bindingPath = bitMaskBindingPath };
                root.Add(categories);

                return root;
            }
        }

        #endregion

        #region IMGUI

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            var bitMaskProperty = property.FindPropertyRelative(nameof(PhysicsMask.bitMask));
            var showAsPhysicsMask = ShowAsPhysicsMask(property);

            DrawMaskField(position, label, bitMaskProperty, showAsPhysicsMask);

            EditorGUI.EndProperty();
        }

        #endregion

        // Resolves the field the same way Unity does when it picks a drawer, so a field nested in a struct is found on that struct.
        // For an array or List element the field is the collection itself, which is where the attribute sits.
        internal static bool ShowAsPhysicsMask(SerializedProperty property)
        {
            var field = ScriptAttributeUtility.GetFieldInfoFromProperty(property, out _);
            return field != null && field.IsDefined(typeof(PhysicsMask.ShowAsPhysicsMaskAttribute), true);
        }
    }
}
