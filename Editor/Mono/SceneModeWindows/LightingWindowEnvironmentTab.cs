// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Object = UnityEngine.Object;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Rendering;

namespace UnityEditor
{
    public abstract class LightingWindowEnvironmentSection
    {
        public virtual void OnEnable() {}
        public virtual void OnDisable() {}
        public virtual void OnInspectorGUI() {}
    }

    internal class LightingWindowEnvironmentTab
    {
        class Styles
        {
            public static readonly GUIContent OtherSettings = EditorGUIUtility.TrTextContent("Other Settings");
        }

        class DefaultEnvironmentSectionExtension : LightingWindowEnvironmentSection
        {
            Editor m_EnvironmentEditor;

            Editor environmentEditor
            {
                get
                {
                    if (m_EnvironmentEditor == null || m_EnvironmentEditor.target == null)
                    {
                        Editor.CreateCachedEditor(RenderSettings.GetRenderSettings(), typeof(LightingEditor), ref m_EnvironmentEditor);
                    }

                    return m_EnvironmentEditor;
                }
            }

            public override void OnInspectorGUI()
            {
                environmentEditor.OnInspectorGUI();
            }

            public override void OnDisable()
            {
                if (m_EnvironmentEditor != null)
                {
                    Object.DestroyImmediate(m_EnvironmentEditor);
                    m_EnvironmentEditor = null;
                }
            }
        }

        LightingWindowEnvironmentSection m_EnvironmentSection;
        Editor          m_FogEditor;
        Editor          m_OtherRenderingEditor;
        SavedBool       m_ShowOtherSettings;
        Object          m_RenderSettings = null;
        Vector2         m_ScrollPosition = Vector2.zero;

        Type m_SRP      = GraphicsSettings.currentRenderPipeline?.GetType();

        Object renderSettings
        {
            get
            {
                if (m_RenderSettings == null || m_RenderSettings != RenderSettings.GetRenderSettings())
                    m_RenderSettings = RenderSettings.GetRenderSettings();

                return m_RenderSettings;
            }
        }

        LightingWindowEnvironmentSection environmentEditor
        {
            get
            {
                var currentSRP = GraphicsSettings.currentRenderPipeline?.GetType();
                if (m_EnvironmentSection != null && m_SRP != currentSRP)
                {
                    m_SRP = currentSRP;
                    m_EnvironmentSection.OnDisable();
                    m_EnvironmentSection = null;
                }

                if (m_EnvironmentSection == null)
                {
                    Type extensionType = RenderPipelineEditorUtility.FetchFirstCompatibleTypeUsingScriptableRenderPipelineExtension<LightingWindowEnvironmentSection>();
                    if (extensionType == null)
                        extensionType = typeof(DefaultEnvironmentSectionExtension);
                    LightingWindowEnvironmentSection extension = (LightingWindowEnvironmentSection)Activator.CreateInstance(extensionType);
                    m_EnvironmentSection = extension;
                    m_EnvironmentSection.OnEnable();
                }

                return m_EnvironmentSection;
            }
        }

        Editor fogEditor
        {
            get
            {
                if (m_FogEditor == null || m_FogEditor.target == null || m_FogEditor.target != RenderSettings.GetRenderSettings())
                {
                    Editor.CreateCachedEditor(renderSettings, typeof(FogEditor), ref m_FogEditor);
                }

                return m_FogEditor;
            }
        }

        Editor otherRenderingEditor
        {
            get
            {
                if (m_OtherRenderingEditor == null || m_OtherRenderingEditor.target == null || m_OtherRenderingEditor.target != RenderSettings.GetRenderSettings())
                {
                    Editor.CreateCachedEditor(renderSettings, typeof(OtherRenderingEditor), ref m_OtherRenderingEditor);
                }

                return m_OtherRenderingEditor;
            }
        }

        public void OnEnable()
        {
            m_ShowOtherSettings = new SavedBool($"LightingWindow.ShowOtherSettings", true);
        }

        public void OnDisable()
        {
            ClearCachedProperties();
        }

        void ClearCachedProperties()
        {
            if (m_EnvironmentSection != null)
            {
                m_EnvironmentSection.OnDisable();
                m_EnvironmentSection = null;
            }
            if (m_FogEditor != null)
            {
                Object.DestroyImmediate(m_FogEditor);
                m_FogEditor = null;
            }
            if (m_OtherRenderingEditor != null)
            {
                Object.DestroyImmediate(m_OtherRenderingEditor);
                m_OtherRenderingEditor = null;
            }
        }

        public void OnGUI()
        {
            EditorGUIUtility.hierarchyMode = true;

            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            if (!SupportedRenderingFeatures.active.overridesEnvironmentLighting)
                environmentEditor.OnInspectorGUI();

            OtherSettingsGUI();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();
        }

        void OtherSettingsGUI()
        {
            if (SupportedRenderingFeatures.active.overridesFog && SupportedRenderingFeatures.active.overridesOtherLightingSettings)
                return;

            m_ShowOtherSettings.value = EditorGUILayout.FoldoutTitlebar(m_ShowOtherSettings.value, Styles.OtherSettings, true);

            if (m_ShowOtherSettings.value)
            {
                EditorGUI.indentLevel++;

                if (!SupportedRenderingFeatures.active.overridesFog)
                    fogEditor.OnInspectorGUI();

                if (!SupportedRenderingFeatures.active.overridesOtherLightingSettings)
                    otherRenderingEditor.OnInspectorGUI();

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
        }
    }
} // namespace
