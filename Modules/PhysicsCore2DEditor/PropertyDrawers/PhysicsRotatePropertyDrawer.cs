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
            var root = new VisualElement();

            var directionProperty = property.FindPropertyRelative(nameof(PhysicsRotate.direction));
            var rotation = LimitPrecision(new PhysicsRotate { direction = directionProperty.vector2Value }.degrees);

            // The fields are locals captured by the callbacks below, never drawer fields.
            // One drawer instance creates the UI for every property of this type in a list, so drawer fields would only refer to the last one created.
            var rotationField = new FloatField(property.displayName) { tooltip = Tooltips.rotation, value = rotation };
            rotationField.AddToClassList(FloatField.alignedFieldUssClassName);

            var directionField = new Vector2Field { bindingPath = directionProperty.propertyPath };
            directionField.style.display = DisplayStyle.None;

            root.Add(rotationField);
            root.Add(directionField);

            rotationField.RegisterValueChangedCallback(UserRotationChanges);
            directionField.RegisterValueChangedCallback(DirectionChanged);

            return root;

            #region Locals

            void UserRotationChanges(ChangeEvent<float> evt)
            {
                // We need to support the direction property changing due to undo/redo.
                // In this case we need to stop the callback but without a way to suppress it, we need to unregister/register.
                if (directionField.UnregisterValueChangedCallback(DirectionChanged))
                {
                    directionField.value = PhysicsRotate.FromDegrees(evt.newValue).direction;
                    directionField.RegisterValueChangedCallback(DirectionChanged);
                }
            }

            void DirectionChanged(ChangeEvent<Vector2> evt)
            {
                // Fetch the rotation and ensure it's valid.
                var directionRotation = new PhysicsRotate { direction = evt.newValue };
                if (!directionRotation.isValid)
                    directionRotation = new PhysicsRotate();

                // Only update the rotation field if the direction rotation isn't equivalent.
                // NOTE: This stops the immediately update within the range of +/- PI radians.
                if (!Mathf.Approximately(PhysicsRotate.UnwindAngle(PhysicsMath.ToRadians(rotationField.value)), PhysicsRotate.UnwindAngle(directionRotation.radians)))
                    rotationField.value = LimitPrecision(directionRotation.degrees);
            }

            #endregion
        }

        #endregion

        #region IMGUI

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            var directionProperty = property.FindPropertyRelative(nameof(PhysicsRotate.direction));
            var degrees = LimitPrecision(new PhysicsRotate { direction = directionProperty.vector2Value }.degrees);

            var labelWithTooltip = new GUIContent(label.text, label.image, Tooltips.rotation);
            EditorGUI.BeginChangeCheck();
            var newDegrees = EditorGUI.FloatField(position, labelWithTooltip, degrees);
            if (EditorGUI.EndChangeCheck())
                directionProperty.vector2Value = PhysicsRotate.FromDegrees(newDegrees).direction;

            EditorGUI.EndProperty();
        }

        #endregion

        static float LimitPrecision(float value) => Mathf.Round(value * 100.0f) * 0.01f;
    }
}
