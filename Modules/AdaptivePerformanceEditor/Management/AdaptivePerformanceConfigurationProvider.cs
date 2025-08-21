// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.AdaptivePerformance;
using UnityEditor.AdaptivePerformance.Editor.Metadata;

namespace UnityEditor.AdaptivePerformance.Editor
{
    internal class AdaptivePerformanceConfigurationProvider : SettingsProvider
    {
        Type m_BuildDataType = null;
        string m_BuildSettingsKey;
        UnityEditor.Editor m_CachedEditor;
        SerializedObject m_SettingsWrapper;

        public AdaptivePerformanceConfigurationProvider(string path, string buildSettingsKey, Type buildDataType, SettingsScope scopes = SettingsScope.Project) : base(path, scopes)
        {
            m_BuildDataType = buildDataType;
            m_BuildSettingsKey = buildSettingsKey;
            if (currentSettings == null)
            {
                Create();
            }
        }

        ScriptableObject currentSettings
        {
            get
            {
                ScriptableObject settings = null;
                EditorBuildSettings.TryGetConfigObject(m_BuildSettingsKey, out settings);
                if (settings == null)
                {
                    string searchText = String.Format("t:{0}", m_BuildDataType.Name);
                    string[] assets = AssetDatabase.FindAssets(searchText);
                    if (assets.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(assets[0]);
                        settings = AssetDatabase.LoadAssetAtPath(path, m_BuildDataType) as ScriptableObject;
                        if (settings != null)
                        {
                            EditorBuildSettings.AddConfigObject(m_BuildSettingsKey, settings, true);
                        }
                    }
                }
                return settings;
            }
        }

        void InitEditorData(ScriptableObject settings)
        {
            if (settings != null)
            {
                m_SettingsWrapper = new SerializedObject(settings);
                UnityEditor.Editor.CreateCachedEditor(settings, null, ref m_CachedEditor);
            }
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            InitEditorData(currentSettings);
        }

        public override void OnDeactivate()
        {
            m_CachedEditor = null;
            m_SettingsWrapper = null;
        }

        public override void OnGUI(string searchContext)
        {
            if (m_SettingsWrapper == null || m_SettingsWrapper.targetObject == null)
            {
                ScriptableObject settings = (currentSettings != null) ? currentSettings : Create();
                InitEditorData(settings);
            }

            if (m_SettingsWrapper != null  && m_SettingsWrapper.targetObject != null && m_CachedEditor != null)
            {
                m_SettingsWrapper.Update();
                m_CachedEditor.OnInspectorGUI();
                m_SettingsWrapper.ApplyModifiedProperties();
            }
        }

        ScriptableObject Create()
        {
            ScriptableObject settings = ScriptableObject.CreateInstance(m_BuildDataType) as ScriptableObject;
            if (settings != null)
            {
                var package = AdaptivePerformancePackageMetadataStore.GetPackageForSettingsTypeNamed(m_BuildDataType.FullName);
                package?.PopulateNewSettingsInstance(settings);

                string newAssetName = String.Format("{0}.asset", EditorUtilities.TypeNameToString(m_BuildDataType));
                string assetPath = EditorUtilities.GetAssetPathForComponents(EditorUtilities.s_DefaultSettingsPath);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    assetPath = Path.Combine(assetPath, newAssetName);
                    settings.hideFlags = HideFlags.HideInInspector;
                    AssetDatabase.CreateAsset(settings, assetPath);
                    EditorBuildSettings.AddConfigObject(m_BuildSettingsKey, settings, true);
                    return settings;
                }
            }
            return null;
        }
    }
}
