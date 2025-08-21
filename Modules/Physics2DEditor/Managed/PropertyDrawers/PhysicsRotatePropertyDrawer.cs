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
        private FloatField m_AngleField;
        private Vector2Field m_DirectionField;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            var foldout = new Foldout { text = property.displayName, viewDataKey = typeof(PhysicsRotatePropertyDrawer).ToString() };
            root.Add(foldout);

            var directionProperty = property.FindPropertyRelative(nameof(PhysicsRotate.direction));
            var angle = LimitPrecision(PhysicsMath.ToDegrees(new PhysicsRotate { direction = directionProperty.vector2Value }.angle));
            m_AngleField = new FloatField("Angle") { value = angle };
            m_AngleField.AddToClassList(FloatField.alignedFieldUssClassName);
            m_DirectionField = new Vector2Field { label = "Direction", enabledSelf = false, bindingPath = directionProperty.propertyPath };
            m_DirectionField.AddToClassList(Vector2Field.alignedFieldUssClassName);
            foldout.Add(m_AngleField);
            foldout.Add(m_DirectionField);
            
            m_AngleField.RegisterValueChangedCallback(UserAngleChanges);
            m_DirectionField.RegisterValueChangedCallback(DirectionChanged);

            return root;
        }

        void UserAngleChanges(ChangeEvent<float> evt)
        {
            // We need to support the direction property changing due to undo/redo.
            // In this case we need to stop the callback but without a way to surpress it, we need to unregister/register.
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

            m_AngleField.value = LimitPrecision(PhysicsMath.ToDegrees(rotation.angle));
        }

        static float LimitPrecision(float value) => Mathf.Round(value * 100.0f) * 0.01f;
    }
}
