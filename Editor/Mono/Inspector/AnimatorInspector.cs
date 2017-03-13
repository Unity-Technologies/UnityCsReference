// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.AnimatedValues;

namespace UnityEditor
{
    [CustomEditor(typeof(Animator))]
    [CanEditMultipleObjects]
    internal class AnimatorInspector : Editor
    {
        SerializedProperty m_Avatar;
        SerializedProperty m_ApplyRootMotion;
        SerializedProperty m_CullingMode;
        SerializedProperty m_UpdateMode;
        SerializedProperty m_WarningMessage;

        AnimBool m_ShowWarningMessage = new AnimBool();
        bool m_IsRootPositionOrRotationControlledByCurves;

        private bool IsWarningMessageEmpty { get { return m_WarningMessage != null && m_WarningMessage.stringValue.Length > 0; } }
        private string WarningMessage { get { return m_WarningMessage != null ? m_WarningMessage.stringValue : ""; } }

        class Styles
        {
            public GUIContent applyRootMotion = new GUIContent(EditorGUIUtility.TextContent("Apply Root Motion"));
            public GUIContent updateMode = new GUIContent(EditorGUIUtility.TextContent("Update Mode"));
            public GUIContent cullingMode = new GUIContent(EditorGUIUtility.TextContent("Culling Mode"));

            public Styles()
            {
                applyRootMotion.tooltip = "Automatically move the object using the root motion from the animations";
                updateMode.tooltip = "Controls when and how often the Animator is updated";
                cullingMode.tooltip = "Controls what is updated when the object has been culled";
            }
        }
        static Styles styles;

        private void Init()
        {
            if (styles == null)
            {
                styles = new Styles();
            }
            InitShowOptions();
        }

        private void InitShowOptions()
        {
            m_ShowWarningMessage.value = IsWarningMessageEmpty;

            m_ShowWarningMessage.valueChanged.AddListener(Repaint);
        }

        private void UpdateShowOptions()
        {
            m_ShowWarningMessage.target = IsWarningMessageEmpty;
        }

        void OnEnable()
        {
            m_Avatar = serializedObject.FindProperty("m_Avatar");
            m_ApplyRootMotion = serializedObject.FindProperty("m_ApplyRootMotion");
            m_CullingMode = serializedObject.FindProperty("m_CullingMode");
            m_UpdateMode = serializedObject.FindProperty("m_UpdateMode");
            m_WarningMessage = serializedObject.FindProperty("m_WarningMessage");


            Init();
        }

        public override void OnInspectorGUI()
        {
            bool isEditingMultipleObjects = targets.Length > 1;

            bool cullingModeChanged = false;
            bool updateModeChanged = false;

            Animator firstAnimator = target as Animator;

            serializedObject.UpdateIfRequiredOrScript();

            UpdateShowOptions();

            EditorGUI.BeginChangeCheck();
            var controller  = EditorGUILayout.ObjectField("Controller", firstAnimator.runtimeAnimatorController, typeof(RuntimeAnimatorController), false) as RuntimeAnimatorController;
            if (EditorGUI.EndChangeCheck())
            {
                foreach (Animator animator in targets)
                {
                    Undo.RecordObject(animator, "Changed AnimatorController");
                    animator.runtimeAnimatorController = controller;
                }
                AnimationWindowUtility.ControllerChanged();
            }

            EditorGUILayout.PropertyField(m_Avatar);
            if (firstAnimator.supportsOnAnimatorMove && !isEditingMultipleObjects)
            {
                EditorGUILayout.LabelField("Apply Root Motion", "Handled by Script");
            }
            else
            {
                EditorGUILayout.PropertyField(m_ApplyRootMotion, styles.applyRootMotion);

                // This might change between layout & repaint so we have local cached value to only update on layout
                if (Event.current.type == EventType.Layout)
                    m_IsRootPositionOrRotationControlledByCurves = firstAnimator.isRootPositionOrRotationControlledByCurves;

                if (!m_ApplyRootMotion.boolValue && m_IsRootPositionOrRotationControlledByCurves)
                {
                    EditorGUILayout.HelpBox("Root position or rotation are controlled by curves", MessageType.Info, true);
                }
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_UpdateMode, styles.updateMode);
            updateModeChanged =  EditorGUI.EndChangeCheck();


            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CullingMode, styles.cullingMode);
            cullingModeChanged =  EditorGUI.EndChangeCheck();


            if (!isEditingMultipleObjects)
                EditorGUILayout.HelpBox(firstAnimator.GetStats(), MessageType.Info, true);

            if (EditorGUILayout.BeginFadeGroup(m_ShowWarningMessage.faded))
            {
                EditorGUILayout.HelpBox(WarningMessage, MessageType.Warning, true);
            }
            EditorGUILayout.EndFadeGroup();


            serializedObject.ApplyModifiedProperties();

            foreach (Animator animator in targets)
            {
                if (cullingModeChanged)
                    animator.OnCullingModeChanged();

                if (updateModeChanged)
                    animator.OnUpdateModeChanged();
            }
        }
    }
}
