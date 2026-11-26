// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.U2D.Physics.Editor
{
    [CustomPropertyDrawer(typeof(PhysicsRotate))]
    sealed class PhysicsRotatePropertyDrawer : PropertyDrawer
    {
        static class Tooltips
        {
            public const string rotation = "A rotation is always stored in the range of +/- 180 degrees (or PI radians) as rotation is stored as a unit vector direction.";
        }

        #region UITK

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // The rotation is stored as a direction but shown in degrees, so the field is linked to the property rather than bound to it.
            // The field is delayed, so a rotation is written only when editing ends, and is then shown as stored, in the range of +/- 180 degrees.
            var rotationField = new FloatField(property.displayName) { tooltip = Tooltips.rotation, isDelayed = true };
            rotationField.AddToClassList(FloatField.alignedFieldUssClassName);
            SerializedPropertyProxy.Link(rotationField, property, ReadDegrees, WriteDegrees);

            return rotationField;
        }

        #endregion

        #region IMGUI

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            var labelWithTooltip = new GUIContent(label.text, label.image, Tooltips.rotation);
            EditorGUI.BeginChangeCheck();
            var newDegrees = EditorGUI.FloatField(position, labelWithTooltip, ReadDegrees(property));
            if (EditorGUI.EndChangeCheck())
                WriteDegrees(property, newDegrees);

            EditorGUI.EndProperty();
        }

        #endregion

        // Read the stored direction as degrees, rounded for display, treating an invalid direction as no rotation.
        private static float ReadDegrees(SerializedProperty property)
        {
            var rotate = new PhysicsRotate { direction = property.FindPropertyRelative(nameof(PhysicsRotate.direction)).vector2Value };
            if (!rotate.isValid)
                rotate = new PhysicsRotate();

            return LimitPrecision(rotate.degrees);
        }

        // Store degrees as the direction they describe.
        private static void WriteDegrees(SerializedProperty property, float degrees)
            => property.FindPropertyRelative(nameof(PhysicsRotate.direction)).vector2Value = PhysicsRotate.FromDegrees(degrees).direction;

        private static float LimitPrecision(float value) => Mathf.Round(value * 100.0f) * 0.01f;
    }
}
