// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.AdaptivePerformance.Editor;
using UnityEditor.AdaptivePerformance.Editor.Metadata;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.AdaptivePerformance.UI.Editor
{
    /// <summary>
    /// A toggle to enable adaptive performance in build profile window
    /// </summary>
    [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
    sealed class BuildProfileAdaptivePerformanceToggle : VisualElement
    {
        private Toggle m_EnableAdaptivePerformanceToggle;
        public BuildProfileAdaptivePerformanceProviderUI m_AdaptivePerformanceProviderUI;
        public static readonly string adaptivePerformanceLabelText = L10n.Tr("Adaptive Performance Settings");

        readonly string k_LabelText = L10n.Tr("Enable Adaptive Performance");
        const string k_BuildProfileAdaptivePerformanceUIUSS = "AdaptivePerformance/StyleSheets/BuildProfileAdaptivePerformanceUI/BuildProfileAdaptivePerformanceUI.uss";
        const string k_BuildProfileAdaptivePerformanceUIUXML = "AdaptivePerformance/UXML/BuildProfileAdaptivePerformanceUI/BuildProfileAdaptivePerformanceUI.uxml";
        BuildProfile m_BuildProfile;
        private VisualElement m_AdaptivePerformanceProviderElement;

        public BuildProfileAdaptivePerformanceToggle(BuildProfile profile)
        {
            AdaptivePerformancePackageMetadataStore.InitKnownPluginPackages();
            m_BuildProfile = profile;
            m_AdaptivePerformanceProviderUI = new BuildProfileAdaptivePerformanceProviderUI(m_BuildProfile);
            var buildProfileUI = EditorGUIUtility.LoadRequired(k_BuildProfileAdaptivePerformanceUIUXML) as VisualTreeAsset;
            var buildProfileUSS = EditorGUIUtility.LoadRequired(k_BuildProfileAdaptivePerformanceUIUSS) as StyleSheet;
            buildProfileUI.CloneTree(this);
            this.styleSheets.Add(buildProfileUSS);
            m_AdaptivePerformanceProviderElement = this.Q<VisualElement>("adaptivePerformance-provider-container");
            m_EnableAdaptivePerformanceToggle = this.Q<Toggle>("enable-adaptivePerformance-toggle");
            m_EnableAdaptivePerformanceToggle.label = k_LabelText;
            m_EnableAdaptivePerformanceToggle.value = m_BuildProfile.platformBuildProfile?.adaptivePerformanceEnabled ?? false;
            m_AdaptivePerformanceProviderElement.style.display = DisplayStyle.None;
            m_EnableAdaptivePerformanceToggle.RegisterValueChangedCallback(UpdataProvider);
            if (m_BuildProfile.platformBuildProfile?.adaptivePerformanceEnabled == true)
            {
                m_AdaptivePerformanceProviderElement.style.display = DisplayStyle.Flex;
                if (m_BuildProfile.GetComponent<AdaptivePerformanceGeneralSettings>() == null)
                {
                    InitializeSettingsAndUI();
                }
                else
                {
                    m_AdaptivePerformanceProviderUI.CreateUI();
                }
            }
            m_AdaptivePerformanceProviderElement.Add(m_AdaptivePerformanceProviderUI);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
        }

        public static void RemoveAllSettingsFromBuildProfile(BuildProfile profile)
        {
            var generalSetting = profile.GetComponent<AdaptivePerformanceGeneralSettings>();
            if ( generalSetting != null)
            {
                profile.RemoveComponent(generalSetting);
                ScriptableObject.DestroyImmediate(generalSetting, true);
            }
            var managerSetting = profile.GetComponent<AdaptivePerformanceManagerSettings>();
            if (managerSetting != null)
            {
                profile.RemoveComponent(managerSetting);
                foreach (var loader in managerSetting.loaders)
                {
                    if (loader == null) continue;
                    AssetDatabase.RemoveObjectFromAsset(loader);
                }
                managerSetting.loaders.Clear();
                ScriptableObject.DestroyImmediate(managerSetting, true);
            }
            var providerSettingContainer = profile.GetComponent<BuildProfileProviderContainer>();
            if (providerSettingContainer != null)
            {
                profile.RemoveComponent(providerSettingContainer);
                foreach (var providerSettings in providerSettingContainer.adaptivePerformanceProviderSettings)
                {
                    // The setting might have been destroyed by removing the package.
                    if (providerSettings == null)
                    {
                        continue;
                    }

                    foreach (var scaler in providerSettings.AddedScalerViaScan)
                    {
                        AssetDatabase.RemoveObjectFromAsset(scaler);
                        ScriptableObject.DestroyImmediate(scaler, true);
                    }

                    foreach (var profiles in providerSettings.ScalerProfiles)
                    {
                        foreach (var addedScaler in profiles.AddedScalers)
                        {
                            AssetDatabase.RemoveObjectFromAsset(addedScaler);
                            ScriptableObject.DestroyImmediate(addedScaler, true);
                        }
                    }
                    AssetDatabase.RemoveObjectFromAsset(providerSettings);
                    ScriptableObject.DestroyImmediate(providerSettings, true);
                }
                providerSettingContainer.adaptivePerformanceProviderSettings.Clear();
                ScriptableObject.DestroyImmediate(providerSettingContainer, true);
            }
            AssetDatabase.SaveAssetIfDirty(profile);
        }

        void UpdataProvider(ChangeEvent<bool> evt)
        {
            m_BuildProfile.platformBuildProfile.adaptivePerformanceEnabled = evt.newValue;
            EditorUtility.SetDirty(m_BuildProfile);
            if (evt.newValue == false)
            {
                m_AdaptivePerformanceProviderUI.UpdateMetadata();
                m_AdaptivePerformanceProviderUI.Clear();
                m_AdaptivePerformanceProviderElement.style.display = DisplayStyle.None;
            }
            else
            {
                InitializeSettingsAndUI();
            }
            EditorUtilities.EnableAPModule(evt.newValue);
        }

        public void InitializeSettingsAndUI()
        {
            EditorUtilities.CheckEnableFrameTimingState(m_BuildProfile);
            AddAdaptivePerformanceGeneralSettingsObject(m_BuildProfile);
            m_AdaptivePerformanceProviderUI.CreateUI();
            m_AdaptivePerformanceProviderUI.SelectDefaultProvider();
            m_AdaptivePerformanceProviderElement.style.display = DisplayStyle.Flex;
        }

        public static void AddAdaptivePerformanceGeneralSettingsObject(BuildProfile profile)
        {
            var generalSetting = profile.GetComponent<AdaptivePerformanceGeneralSettings>();
            if (generalSetting == null)
            {
                generalSetting = ScriptableObject.CreateInstance<AdaptivePerformanceGeneralSettings>();
                generalSetting.hideFlags = HideFlags.HideInInspector;
                profile.AddComponent(generalSetting);
            }
        }
    }
}
