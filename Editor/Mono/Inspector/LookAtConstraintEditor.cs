// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Animations;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor
{
    [CustomEditor(typeof(LookAtConstraint))]
    [CanEditMultipleObjects]
    internal class LookAtConstraintEditor : ConstraintEditorBase
    {
        private SerializedProperty m_RotationAtRest;
        private SerializedProperty m_RotationOffset;
        private SerializedProperty m_WorldUpObject;
        private SerializedProperty m_Roll;
        private SerializedProperty m_UseUpObject;
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
            GUIContent m_RotationAtRest = L10n.TextContent("Rotation At Rest", "The orientation of the constrained object when the weights of the sources add up to zero or when all the rotation axes are disabled.", null, null);
            GUIContent m_RotationOffset = L10n.TextContent("Rotation Offset", "The offset from the constrained orientation.", null, null);
            GUIContent m_WorldUpObject = L10n.TextContent("World Up Object", "The reference object when the World Up Type is either Object Up or Object Rotation Up.", null, null);
            GUIContent m_Roll = L10n.TextContent("Roll", "Specifies the roll angle in degrees.", null, null);
            GUIContent m_UseUpObject = L10n.TextContent("Use Up Object", "Specifies how the world up vector should be computed. Either use the World Up Object or the Roll value", null, null);

            public override GUIContent AtRest { get { return m_RotationAtRest; } }
            public override GUIContent Offset { get { return m_RotationOffset; } }
            public GUIContent WorldUpObject { get { return m_WorldUpObject; } }
            public GUIContent Roll { get { return m_Roll; } }
            public GUIContent UseUpObject { get { return m_UseUpObject; } }
        }

        [NoAutoStaticsCleanup]
        private static Styles s_Style = null;

        public void OnEnable()
        {
            if (s_Style == null)
                s_Style = new Styles();

            m_RotationAtRest = serializedObject.FindProperty("m_RotationAtRest");
            m_RotationOffset = serializedObject.FindProperty("m_RotationOffset");

            m_UseUpObject = serializedObject.FindProperty("m_UseUpObject");
            m_WorldUpObject = serializedObject.FindProperty("m_WorldUpObject");
            m_Roll = serializedObject.FindProperty("m_Roll");

            m_Weight = serializedObject.FindProperty("m_Weight");
            m_IsContraintActive = serializedObject.FindProperty("m_Active");
            m_IsLocked = serializedObject.FindProperty("m_IsLocked");
            m_Sources = serializedObject.FindProperty("m_Sources");

            OnEnable(s_Style);
        }

        internal override void OnValueAtRestChanged()
        {
            foreach (var t in targets)
            {
                (t as LookAtConstraint).transform.SetLocalEulerAngles(atRest.vector3Value, RotationOrder.OrderZXY);
                EditorUtility.SetDirty(target);
            }
        }

        internal override void ShowCustomProperties()
        {
            EditorGUILayout.PropertyField(m_UseUpObject, s_Style.UseUpObject);

            using (new EditorGUI.DisabledGroupScope(m_UseUpObject.boolValue))
            {
                EditorGUILayout.PropertyField(m_Roll, s_Style.Roll);
            }

            using (new EditorGUI.DisabledGroupScope(!m_UseUpObject.boolValue))
            {
                EditorGUILayout.PropertyField(m_WorldUpObject, s_Style.WorldUpObject);
            }
        }

        internal override void ShowFreezeAxesControl()
        {
        }

        public override void OnInspectorGUI()
        {
            if (s_Style == null)
                s_Style = new Styles();

            serializedObject.Update();

            ShowConstraintEditor<LookAtConstraint>(s_Style);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
