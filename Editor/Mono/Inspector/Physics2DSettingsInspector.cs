// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.AnimatedValues;

namespace UnityEditor
{
    [CustomEditor(typeof(Physics2DSettings))]
    internal class Physics2DSettingsInspector : ProjectSettingsBaseEditor
    {
        Vector2 m_LayerCollisionMatrixScrollPos;
        bool m_ShowLayerCollisionMatrix = true;

        static bool s_ShowGizmoSettings;
        readonly AnimBool m_GizmoSettingsFade = new AnimBool();
        SerializedProperty m_AlwaysShowColliders;
        SerializedProperty m_ShowColliderSleep;
        SerializedProperty m_ShowColliderContacts;
        SerializedProperty m_ShowColliderAABB;
        SerializedProperty m_ContactArrowScale;
        SerializedProperty m_ColliderAwakeColor;
        SerializedProperty m_ColliderAsleepColor;
        SerializedProperty m_ColliderContactColor;
        SerializedProperty m_ColliderAABBColor;

        public void OnEnable()
        {
            m_AlwaysShowColliders = serializedObject.FindProperty("m_AlwaysShowColliders");
            m_ShowColliderSleep = serializedObject.FindProperty("m_ShowColliderSleep");
            m_ShowColliderContacts = serializedObject.FindProperty("m_ShowColliderContacts");
            m_ShowColliderAABB = serializedObject.FindProperty("m_ShowColliderAABB");
            m_ContactArrowScale = serializedObject.FindProperty("m_ContactArrowScale");
            m_ColliderAwakeColor = serializedObject.FindProperty("m_ColliderAwakeColor");
            m_ColliderAsleepColor = serializedObject.FindProperty("m_ColliderAsleepColor");
            m_ColliderContactColor = serializedObject.FindProperty("m_ColliderContactColor");
            m_ColliderAABBColor = serializedObject.FindProperty("m_ColliderAABBColor");

            m_GizmoSettingsFade.value = s_ShowGizmoSettings;
            m_GizmoSettingsFade.valueChanged.AddListener(Repaint);
        }

        public void OnDisable()
        {
            m_GizmoSettingsFade.valueChanged.RemoveListener(Repaint);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            s_ShowGizmoSettings = EditorGUILayout.Foldout(s_ShowGizmoSettings, "Gizmos", true);
            m_GizmoSettingsFade.target = s_ShowGizmoSettings;
            if (m_GizmoSettingsFade.value)
            {
                serializedObject.Update();

                if (EditorGUILayout.BeginFadeGroup(m_GizmoSettingsFade.faded))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_AlwaysShowColliders);
                    EditorGUILayout.PropertyField(m_ShowColliderSleep);
                    EditorGUILayout.PropertyField(m_ColliderAwakeColor);
                    EditorGUILayout.PropertyField(m_ColliderAsleepColor);
                    EditorGUILayout.PropertyField(m_ShowColliderContacts);
                    EditorGUILayout.Slider(m_ContactArrowScale, 0.1f, 1.0f, m_ContactArrowScale.displayName);
                    EditorGUILayout.PropertyField(m_ColliderContactColor);
                    EditorGUILayout.PropertyField(m_ShowColliderAABB);
                    EditorGUILayout.PropertyField(m_ColliderAABBColor);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFadeGroup();

                serializedObject.ApplyModifiedProperties();
            }

            LayerMatrixGUI.DoGUI("Layer Collision Matrix", ref m_ShowLayerCollisionMatrix, ref m_LayerCollisionMatrixScrollPos, GetValue, SetValue);
        }

        static bool GetValue(int layerA, int layerB) { return !Physics2D.GetIgnoreLayerCollision(layerA, layerB); }
        static void SetValue(int layerA, int layerB, bool val) { Physics2D.IgnoreLayerCollision(layerA, layerB, !val); }
    }
}
