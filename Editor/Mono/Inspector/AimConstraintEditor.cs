// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Animations;

namespace UnityEditor
{
    [CustomEditor(typeof(AimConstraint))]
    [CanEditMultipleObjects]
    internal class AimConstraintEditor : ConstraintEditorBase
    {
        private SerializedProperty m_RotationAtRest;
        private SerializedProperty m_RotationOffset;
        private SerializedProperty m_AimVector;
        private SerializedProperty m_UpVector;
        private SerializedProperty m_WorldUpVector;
        private SerializedProperty m_WorldUpObject;
        private SerializedProperty m_WorldUpType;
        private SerializedProperty m_Weight;
        private SerializedProperty m_IsContraintActive;
        private SerializedProperty m_IsLocked;
        private SerializedProperty m_Sources;

        internal override SerializedProperty atRest { get { return m_RotationAtRest; } }
        internal override SerializedProperty offset { get { return m_RotationOffset; } }
        internal override SerializedProperty weight { get { return m_Weight; } }
        internal override SerializedProperty isContraintActive { get { return m_IsContraintActive; } }
        internal override SerializedProperty isLocked { get { return m_IsLocked; } }
        internal override SerializedProperty sources { get { return m_Sources; } }

        private class Styles : ConstraintStyleBase
        {
            GUIContent m_RotationAtRest = EditorGUIUtility.TrTextContent("Rotation At Rest", "The orientation of the constrained object when the weights of the sources add up to zero or when all the rotation axes are disabled.");
            GUIContent m_RotationOffset = EditorGUIUtility.TrTextContent("Rotation Offset", "The offset from the constrained orientation.");

            GUIContent m_RotationAxes = EditorGUIUtility.TrTextContent("Freeze Rotation Axes", "The axes along which the constraint is applied.");

            GUIContent m_AimVector = EditorGUIUtility.TrTextContent("Aim Vector", "Specifies which axis of the constrained object should aim at the target.");
            GUIContent m_UpVector = EditorGUIUtility.TrTextContent("Up Vector", "Specifies the direction of the up vector in local space.");
            GUIContent m_WorldUpVector = EditorGUIUtility.TrTextContent("World Up Vector", "Specifies the direction of the global up vector.");
            GUIContent m_WorldUpObject = EditorGUIUtility.TrTextContent("World Up Object", "The reference object when the World Up Type is either Object Up or Object Rotation Up.");
            GUIContent m_WorldUpType = EditorGUIUtility.TrTextContent("World Up Type", "Specifies how the world up vector should be computed.");
            GUIContent[] m_WorldUpTypes =
            {
                EditorGUIUtility.TrTextContent("Scene Up", "Use the Y axis as the world up vector."),
                EditorGUIUtility.TrTextContent("Object Up", "Use a vector that points to the reference object as the world up vector."),
                EditorGUIUtility.TrTextContent("Object Rotation Up", "Use a vector defined in the reference object's local space as the world up vector."),
                EditorGUIUtility.TrTextContent("Vector", "The world up vector is user defined."),
                EditorGUIUtility.TrTextContent("None", "The world up vector is ignored.")
            };
            public override GUIContent AtRest { get { return m_RotationAtRest; } }
            public override GUIContent Offset { get { return m_RotationOffset; } }
            public GUIContent FreezeAxes { get { return m_RotationAxes; } }
            public GUIContent AimVector { get { return m_AimVector; } }
            public GUIContent UpVector { get { return m_UpVector; } }
            public GUIContent WorldUpVector { get { return m_WorldUpVector; } }
            public GUIContent WorldUpObject { get { return m_WorldUpObject; } }
            public GUIContent WorldUpType { get { return m_WorldUpType; } }
            public GUIContent[] WorldUpTypes { get { return m_WorldUpTypes; } }
        }

        private static Styles s_Style = null;

        public void OnEnable()
        {
            if (s_Style == null)
                s_Style = new Styles();

            m_RotationAtRest = serializedObject.FindProperty("m_RotationAtRest");
            m_RotationOffset = serializedObject.FindProperty("m_RotationOffset");

            m_AimVector = serializedObject.FindProperty("m_AimVector");
            m_UpVector = serializedObject.FindProperty("m_UpVector");
            m_WorldUpVector = serializedObject.FindProperty("m_WorldUpVector");
            m_WorldUpObject = serializedObject.FindProperty("m_WorldUpObject");
            m_WorldUpType = serializedObject.FindProperty("m_UpType");

            m_Weight = serializedObject.FindProperty("m_Weight");
            m_IsContraintActive = serializedObject.FindProperty("m_IsContraintActive");
            m_IsLocked = serializedObject.FindProperty("m_IsLocked");
            m_Sources = serializedObject.FindProperty("m_Sources");

            OnEnable(s_Style);
        }

        internal override void OnValueAtRestChanged()
        {
            foreach (var t in targets)
                (t as AimConstraint).transform.SetLocalEulerAngles(atRest.vector3Value, RotationOrder.OrderZXY);
        }

        internal override void ShowOffset<T>(ConstraintStyleBase style)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(offset, style.Offset);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var t in targets)
                    (t as T).UserUpdateOffset();
            }
        }

        internal override void ShowCustomProperties()
        {
            EditorGUILayout.PropertyField(m_AimVector, s_Style.AimVector);
            EditorGUILayout.PropertyField(m_UpVector, s_Style.UpVector);
            EditorGUILayout.Popup(m_WorldUpType, s_Style.WorldUpTypes, s_Style.WorldUpType);

            var worldUpType = (AimConstraint.WorldUpType)m_WorldUpType.intValue;
            using (new EditorGUI.DisabledGroupScope(worldUpType != AimConstraint.WorldUpType.ObjectRotationUp && worldUpType != AimConstraint.WorldUpType.Vector))
            {
                EditorGUILayout.PropertyField(m_WorldUpVector, s_Style.WorldUpVector);
            }

            using (new EditorGUI.DisabledGroupScope(worldUpType != AimConstraint.WorldUpType.ObjectUp && worldUpType != AimConstraint.WorldUpType.ObjectRotationUp))
            {
                EditorGUILayout.PropertyField(m_WorldUpObject, s_Style.WorldUpObject);
            }
        }

        internal override void ShowFreezeAxesControl()
        {
            Rect drawRect = EditorGUILayout.GetControlRect(true, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector3, s_Style.FreezeAxes), EditorStyles.toggle);
            EditorGUI.MultiPropertyField(drawRect, s_Style.Axes, serializedObject.FindProperty("m_AffectRotationX"), s_Style.FreezeAxes);
        }

        public override void OnInspectorGUI()
        {
            if (s_Style == null)
                s_Style = new Styles();

            serializedObject.Update();

            ShowConstraintEditor<AimConstraint>(s_Style);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
