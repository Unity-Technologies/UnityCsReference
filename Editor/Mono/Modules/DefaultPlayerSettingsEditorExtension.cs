// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.Modules
{
    internal abstract class DefaultPlayerSettingsEditorExtension : ISettingEditorExtension
    {
        protected PlayerSettingsEditor m_playerSettingsEditor;

        protected PlayerSettingsEditor playerSettingsEditor
        {
            get { return m_playerSettingsEditor; }
        }

        public virtual void OnEnable(PlayerSettingsEditor settingsEditor)
        {
            m_playerSettingsEditor = settingsEditor;

            m_MTRendering = playerSettingsEditor.FindPropertyAssert("m_MTRendering");
        }

        public virtual bool HasPublishSection()
        {
            return true;
        }

        public virtual void PublishSectionGUI(float h, float midWidth, float maxWidth) {}

        public virtual bool HasIdentificationGUI()
        {
            return false;
        }

        public virtual void IdentificationSectionGUI() {}

        public virtual void ConfigurationSectionGUI() {}

        public virtual bool SupportsOrientation()
        {
            return false;
        }

        public virtual bool CanShowUnitySplashScreen()
        {
            return false;
        }

        public virtual void SplashSectionGUI() {}

        public virtual bool UsesStandardIcons()
        {
            return true;
        }

        public virtual void IconSectionGUI() {}

        public virtual bool HasResolutionSection()
        {
            return false;
        }

        public virtual bool SupportsStaticBatching()
        {
            return true;
        }

        public virtual bool SupportsDynamicBatching()
        {
            return true;
        }

        public virtual void ResolutionSectionGUI(float h, float midWidth, float maxWidth) {}

        public virtual bool HasBundleIdentifier()
        {
            return true;
        }

        public virtual bool SupportsHighDynamicRangeDisplays()
        {
            return false;
        }

        public virtual bool SupportsGfxJobModes()
        {
            return false;
        }

        public string FixTargetOSVersion(string version)
        {
            var decimalIndex = version.IndexOf('.');
            if (decimalIndex < 0)
                return (version + ".0").Trim();
            else if (decimalIndex == version.Length - 1)
                return (version + "0").Trim();
            return version.Trim();
        }

        public virtual bool SupportsMultithreadedRendering()
        {
            return false;
        }

        protected SerializedProperty m_MTRendering;
        private static readonly GUIContent m_MTRenderingTooltip = EditorGUIUtility.TextContent("Multithreaded Rendering*");

        protected virtual GUIContent MultithreadedRenderingGUITooltip()
        {
            return m_MTRenderingTooltip;
        }

        public virtual void MultithreadedRenderingGUI(BuildTargetGroup targetGroup)
        {
            if (playerSettingsEditor.IsMobileTarget(targetGroup))
            {
                bool oldValue = PlayerSettings.GetMobileMTRendering(targetGroup);
                bool newValue = EditorGUILayout.Toggle(MultithreadedRenderingGUITooltip(), oldValue);
                if (oldValue != newValue)
                    PlayerSettings.SetMobileMTRendering(targetGroup, newValue);
            }
            else EditorGUILayout.PropertyField(m_MTRendering, m_MTRenderingTooltip);
        }

        public virtual bool SupportsCustomLightmapEncoding()
        {
            return false;
        }
    }
}
