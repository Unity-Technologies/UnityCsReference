// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.LowLevelPhysics2D;
using UnityEngine.UIElements;

namespace UnityEditor.LowLevelPhysics2D
{
    [CustomPropertyDrawer(typeof(PhysicsRotate))]
    sealed class PhysicsRotatePropertyDrawer : PropertyDrawer
    {
        static class Tooltips
        {
            public const string rotation = "A rotation is always stored in the range of +/- 180 degrees (or PI radians) as rotation is stored as a unit vector direction.";
        }

        private FloatField m_RotationField;
        private Vector2Field m_DirectionField;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            var directionProperty = property.FindPropertyRelative(nameof(PhysicsRotate.direction));
            var rotation = LimitPrecision(PhysicsMath.ToDegrees(new PhysicsRotate { direction = directionProperty.vector2Value }.angle));

            m_RotationField = new FloatField(property.displayName) { tooltip = Tooltips.rotation, value = rotation };
            m_RotationField.AddToClassList(FloatField.alignedFieldUssClassName);

            m_DirectionField = new Vector2Field { bindingPath = directionProperty.propertyPath };
            m_DirectionField.style.display = DisplayStyle.None;

            root.Add(m_RotationField);
            root.Add(m_DirectionField);

            m_RotationField.RegisterValueChangedCallback(UserRotationChanges);
            m_DirectionField.RegisterValueChangedCallback(DirectionChanged);

            return root;
        }

        void UserRotationChanges(ChangeEvent<float> evt)
        {
            // We need to support the direction property changing due to undo/redo.
            // In this case we need to stop the callback but without a way to suppress it, we need to unregister/register.
            if (m_DirectionField.UnregisterValueChangedCallback(DirectionChanged))
            {
                m_DirectionField.value = new PhysicsRotate(PhysicsMath.ToRadians(evt.newValue)).direction;
                m_DirectionField.RegisterValueChangedCallback(DirectionChanged);
            }
        }

        void DirectionChanged(ChangeEvent<Vector2> evt)
        {
            // Fetch the rotation and ensure it's valid.
            var rotation = new PhysicsRotate { direction = evt.newValue };
            if (!rotation.isValid)
                rotation = new PhysicsRotate();

            // Only update the rotation field if the direction rotation isn't equivalent.
            // NOTE: This stops the immediately update within the range of +/- PI radians.
            if (!Mathf.Approximately(PhysicsRotate.UnwindAngle(PhysicsMath.ToRadians(m_RotationField.value)), PhysicsRotate.UnwindAngle(rotation.angle)))
                m_RotationField.value = LimitPrecision(PhysicsMath.ToDegrees(rotation.angle));
        }

        static float LimitPrecision(float value) => Mathf.Round(value * 100.0f) * 0.01f;
    }
}
