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

        private class Styles : IConstraintStyle
        {
            GUIContent m_Activate = EditorGUIUtility.TextContent("Activate|Activate the constraint at the current offset from the sources.");
            GUIContent m_Zero = EditorGUIUtility.TextContent("Zero|Activate the constraint at zero offset from the sources.");

            GUIContent m_RotationAtRest = EditorGUIUtility.TextContent("Rotation At Rest|The orientation of the constrained object when the weights of the sources add up to zero or when all the rotation axes are disabled.");
            GUIContent m_RotationOffset = EditorGUIUtility.TextContent("Rotation Offset|The offset from the constrained orientation.");

            GUIContent m_Sources = EditorGUIUtility.TextContent("Sources");

            GUIContent m_Weight = EditorGUIUtility.TextContent("Weight");

            GUIContent m_RotationAxes = EditorGUIUtility.TextContent("Freeze Rotation Axes|The axes along which the constraint is applied.");

            GUIContent m_IsActive = EditorGUIUtility.TextContent("Is Active");
            GUIContent m_IsLocked = EditorGUIUtility.TextContent("Lock|When set, evaluate with the current offset. When not set, update the offset based on the current transform.");

            GUIContent[] m_Axes =
            {
                EditorGUIUtility.TextContent("X"),
                EditorGUIUtility.TextContent("Y"),
                EditorGUIUtility.TextContent("Z")
            };

            GUIContent m_ConstraintSettings = EditorGUIUtility.TextContent("Constraint Settings");

            GUIContent m_AimVector = EditorGUIUtility.TextContent("Aim Vector|Specifies which axis of the constrained object should aim at the target.");
            GUIContent m_UpVector = EditorGUIUtility.TextContent("Up Vector|Specifies the direction of the up vector in local space.");
            GUIContent m_WorldUpVector = EditorGUIUtility.TextContent("World Up Vector|Specifies the direction of the global up vector.");
            GUIContent m_WorldUpObject = EditorGUIUtility.TextContent("World Up Object|The reference object when the World Up Type is either Object Up or Object Rotation Up.");
            GUIContent m_WorldUpType = EditorGUIUtility.TextContent("World Up Type|Specifies how the world up vector should be computed.");
            GUIContent[] m_WorldUpTypes =
            {
                EditorGUIUtility.TextContent("Scene Up|Use the Y axis as the world up vector."),
                EditorGUIUtility.TextContent("Object Up|Use a vector that points to the reference object as the world up vector."),
                EditorGUIUtility.TextContent("Object Rotation Up|Use a vector defined in the reference object's local space as the world up vector."),
                EditorGUIUtility.TextContent("Vector|The world up vector is user defined."),
                EditorGUIUtility.TextContent("None|The world up vector is ignored.")
            };

            public GUIContent Activate { get { return m_Activate; } }
            public GUIContent Zero { get { return m_Zero; } }
            public GUIContent AtRest { get { return m_RotationAtRest; } }
            public GUIContent Offset { get { return m_RotationOffset; } }
            public GUIContent Sources { get { return m_Sources; } }
            public GUIContent Weight { get { return m_Weight; } }
            public GUIContent FreezeAxes { get { return m_RotationAxes; } }
            public GUIContent IsActive { get { return m_IsActive; } }
            public GUIContent IsLocked { get { return m_IsLocked; } }
            public GUIContent[] Axes { get { return m_Axes; } }
            public GUIContent ConstraintSettings { get { return m_ConstraintSettings; } }
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

        internal override void ShowOffset<T>(IConstraintStyle style)
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
