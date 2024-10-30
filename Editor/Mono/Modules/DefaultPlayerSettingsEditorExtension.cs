// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEngine;
using TargetAttributes = UnityEditor.BuildTargetDiscovery.TargetAttributes;

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

        public virtual void ConfigurePlatformProfile(SerializedObject serializedProfile)
        {
        }

        public virtual bool CopyProjectSettingsPlayerSettingsToBuildProfile()
        {
            return false;
        }

        public virtual bool IsPlayerSettingsDataEqualToProjectSettings()
        {
            return true;
        }

        public virtual void OnActiveProfileChanged(BuildProfile previous, BuildProfile newProfile)
        {
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

        public virtual void RenderingSectionGUI() {}

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

        public virtual GraphicsJobMode AdjustGfxJobMode(GraphicsJobMode graphicsJobMode)
        {
            return graphicsJobMode;
        }

        public virtual bool SupportsMultithreadedRendering()
        {
            return false;
        }

        public virtual bool SupportsFrameTimingStatistics()
        {
            return false;
        }

        public virtual void SerializedObjectUpdated() {}

        protected SerializedProperty m_MTRendering;
        private static readonly GUIContent m_MTRenderingTooltip = EditorGUIUtility.TrTextContent("Multithreaded Rendering*");

        protected virtual GUIContent MultithreadedRenderingGUITooltip()
        {
            return m_MTRenderingTooltip;
        }

        public virtual void MultithreadedRenderingGUI(NamedBuildTarget namedBuildTarget)
        {
            // For platforms defined as IsMTRenderingDisabledByDefault in BuildTargetDiscovery::PreloadKnownBuildTargets (iPhone, tvOS, Android) the "Multithreaded Rendering" feature is controlled by PlayerSettings::m_MobileMTRenderingBaked (whose default is false)
            // For other platforms the "Multithreaded Rendering" feature is controlled by PlayerSettings::m_MTRendering. Default value is true (set during PlayerSettings::Reset)
            if (BuildTargetDiscovery.PlatformGroupHasFlag(namedBuildTarget.ToBuildTargetGroup(), TargetAttributes.IsMTRenderingDisabledByDefault))
            {
                var playerSettings = playerSettingsEditor.m_SerializedObject.targetObject as PlayerSettings;
                Debug.Assert(playerSettings != null);

                bool oldValue = playerSettings.GetMobileMTRenderingInternal_Instance(namedBuildTarget.TargetName);
                bool newValue = EditorGUILayout.Toggle(MultithreadedRenderingGUITooltip(), oldValue);
                if (oldValue != newValue)
                {
                    playerSettings.SetMobileMTRenderingInternal_Instance(namedBuildTarget.TargetName, newValue);
                    playerSettingsEditor.OnTargetObjectChangedDirectly();
                }
            }
            else EditorGUILayout.PropertyField(m_MTRendering, m_MTRenderingTooltip);
        }

        public virtual bool SupportsCustomLightmapEncoding()
        {
            return false;
        }

        public virtual bool SupportsCustomNormalMapEncoding()
        {
            return false;
        }

        public virtual bool ShouldShowVulkanSettings()
        {
            return false;
        }

        public virtual void VulkanSectionGUI() {}

        public virtual bool SupportsForcedSrgbBlit()
        {
            return false;
        }

        public virtual bool SupportsStaticSplashScreenBackgroundColor()
        {
            return false;
        }

        public virtual void AutoRotationSectionGUI() {}
    }
}
