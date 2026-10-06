// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.AnimatedValues;

namespace UnityEditor
{
    internal class Collider3DEditorBase : ColliderEditorBase
    {
        protected SerializedProperty m_Material;
        protected SerializedProperty m_IsTrigger;
        protected SerializedProperty m_ProvidesContacts;

        private SerializedProperty m_LayerOverridePriority;
        private SerializedProperty m_IncludeLayers;
        private SerializedProperty m_ExcludeLayers;

        private readonly AnimBool m_ShowLayerOverrides = new AnimBool();
        private SavedBool m_ShowLayerOverridesFoldout;

        protected static class BaseStyles
        {
            public static readonly GUIContent materialContent = L10n.TextContent("Material", "Reference to the Physics Material that determines how this Collider interacts with others.", null, null);
            public static readonly GUIContent triggerContent = L10n.TextContent("Is Trigger", "If enabled, this Collider is used for triggering events and is ignored by the physics engine.", null, null);
            public static readonly GUIContent centerContent = L10n.TextContent("Center", "The position of the Collider in the GameObject's local space.", null, null);
            public static readonly GUIContent providesContacts = L10n.TextContent("Provides Contacts", "Whether or not this collider provides contacts without any MonoBehaviour listeners", null, null);

            public static readonly GUIContent layerOverridePriority = L10n.TextContent("Layer Override Priority", "When 2 colliders have conflicting overrides, the settings of the collider with the higher priority are taken.", null, null);
            public static readonly GUIContent includeLayers = L10n.TextContent("Include Layers", "Layers to include when producing collisions", null, null);
            public static readonly GUIContent excludeLayers = L10n.TextContent("Exclude Layers", "Layers to exclude when producing collisions", null, null);
        }

        public override void OnEnable()
        {
            base.OnEnable();
            m_Material = serializedObject.FindProperty("m_Material");
            m_IsTrigger = serializedObject.FindProperty("m_IsTrigger");
            m_ProvidesContacts = serializedObject.FindProperty("m_ProvidesContacts");
            m_ShowLayerOverrides.valueChanged.AddListener(Repaint);
            m_ShowLayerOverridesFoldout = new SavedBool($"{target.GetType() }.ShowLayerOverridesFoldout", false);
            m_ShowLayerOverrides.value = m_ShowLayerOverridesFoldout.value;

            m_LayerOverridePriority = serializedObject.FindProperty("m_LayerOverridePriority");
            m_IncludeLayers = serializedObject.FindProperty("m_IncludeLayers");
            m_ExcludeLayers = serializedObject.FindProperty("m_ExcludeLayers");

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_IsTrigger, BaseStyles.triggerContent);
            EditorGUILayout.PropertyField(m_Material, BaseStyles.materialContent);
            EditorGUILayout.PropertyField(m_ProvidesContacts, BaseStyles.providesContacts);
            ShowLayerOverridesProperties();

            serializedObject.ApplyModifiedProperties();
        }

        public override void OnDisable()
        {
            m_ShowLayerOverrides.valueChanged.RemoveListener(Repaint);
        }

        protected void ShowLayerOverridesProperties()
        {
            // Show Layer Overrides.
            m_ShowLayerOverridesFoldout.value = m_ShowLayerOverrides.target = EditorGUILayout.Foldout(m_ShowLayerOverrides.target, "Layer Overrides", true);
            if (EditorGUILayout.BeginFadeGroup(m_ShowLayerOverrides.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_LayerOverridePriority, BaseStyles.layerOverridePriority);
                EditorGUILayout.PropertyField(m_IncludeLayers, BaseStyles.includeLayers);
                EditorGUILayout.PropertyField(m_ExcludeLayers, BaseStyles.excludeLayers);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
        }
    }
}
