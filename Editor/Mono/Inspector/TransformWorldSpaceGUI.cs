// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    // Fields of the Transform inspector when it displays values in world space.
    class TransformWorldSpaceGUI
    {
        readonly EditorGUI.NumberFieldValue[] m_Values = new EditorGUI.NumberFieldValue[3];

        TransformWorldSpaceGUIData m_Data;
        SerializedProperty m_LocalPosition;
        SerializedProperty m_LocalRotation;
        SerializedProperty m_LocalScale;
        
        public TransformWorldSpaceGUI(
            TransformWorldSpaceGUIData data,
            SerializedProperty localPosition,
            SerializedProperty localRotation,
            SerializedProperty localScale)
        {
            m_Data = data;
            m_LocalPosition = localPosition;
            m_LocalRotation = localRotation;
            m_LocalScale = localScale;
        }

        public void WorldPositionField(Rect rect, GUIContent label)
        {
            // The local position property drives the prefab override and animation styling, as it is what gets modified.
            int changedAxes = Vector3Field(rect, label, m_LocalPosition, m_Data.position, m_Data.positionMixedFlags);
            if (changedAxes != 0)
            {
                m_Data.ApplyPosition(m_Values, changedAxes, m_LocalPosition.isLiveModified);
                OnTargetsModified();
            }
        }

        public void WorldRotationField(Rect rect, GUIContent label)
        {
            int changedAxes = Vector3Field(rect, label, m_LocalRotation, m_Data.eulerRotation, m_Data.rotationMixedFlags);
            if (changedAxes != 0)
            {
                m_Data.ApplyRotation(m_Values, changedAxes, m_LocalRotation.isLiveModified);
                OnTargetsModified();
            }
        }

        // The world scale cannot be edited, as it cannot be represented exactly by a local scale
        // when a parent is scaled non-uniformly and rotated relative to the child.
        public void WorldScaleField(Rect rect, GUIContent label)
        {
            using (new EditorGUI.DisabledScope(true))
                Vector3Field(rect, label, m_LocalScale, m_Data.scale, m_Data.scaleMixedFlags);
        }

        // Returns a mask of the axes edited by the user, whose new values are in m_Values.
        int Vector3Field(Rect rect, GUIContent label, SerializedProperty property, Vector3Double value, int mixedFlags)
        {
            for (int axis = 0; axis < 3; axis++)
                m_Values[axis] = new EditorGUI.NumberFieldValue(value[axis]);

            label = EditorGUI.BeginProperty(rect, label, property);
            EditorGUI.showMixedValue = false;
            int changedAxes = EditorGUI.Vector3Field(rect, label, m_Values, mixedFlags);
            EditorGUI.EndProperty();

            return changedAxes;
        }

        void OnTargetsModified()
        {
            SerializedObject serializedObject = m_LocalPosition.serializedObject;
            serializedObject.Update();
            serializedObject.SetIsDifferentCacheDirty();
        }
    }
}
