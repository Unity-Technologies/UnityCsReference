// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditorInternal;
using UnityEditor.StyleSheets;
using UnityEngine;
using UnityEngine.AdaptivePerformance;

namespace UnityEditor.AdaptivePerformance.Editor
{
    /// <summary>
    /// This is a custom Editor base for Provider Settings. It displays provider general settings and you can use it to extend provider settings editors to display custom provider settings.
    /// </summary>
    public class ProviderSettingsEditor : UnityEditor.Editor
    {
        const string k_Logging = "m_Logging";
        const string k_AutoPerformanceModeEnabled = "m_AutomaticPerformanceModeEnabled";
        const string k_AutoGameModeEnabled = "m_AutomaticGameModeEnabled";
        const string k_EnableBoostOnStartup = "m_EnableBoostOnStartup";
        const string k_StatsLoggingFrequencyInFrames = "m_StatsLoggingFrequencyInFrames";
        const string k_IndexerSettings = "m_IndexerSettings";
        const string k_IndexerActive = "m_Active";
        const string k_IndexerThermalActionDelay = "m_ThermalActionDelay";
        const string k_IndexerPerformanceActionDelay = "m_PerformanceActionDelay";
        const string k_ScalerName = "m_Name";
        const string k_ScalerEnabled = "m_Enabled";
        const string k_ScalerScale = "m_Scale";
        const string k_ScalerVisualImpact = "m_VisualImpact";
        const string k_ScalerTarget = "m_Target";
        const string k_ScalerMaxLevel = "m_MaxLevel";
        const string k_ScalerMinBound = "m_MinBound";
        const string k_ScalerMaxBound = "m_MaxBound";
        const string k_ScalerProfileList = "m_scalerProfileList";

        static GUIContent s_LoggingLabel = EditorGUIUtility.TrTextContent(L10n.Tr("Logging"), L10n.Tr("Only active in development mode."));
        static GUIContent s_AutomaticPerformanceModeEnabledLabel = EditorGUIUtility.TrTextContent(L10n.Tr("Auto Performance Mode"), L10n.Tr("Auto Performance Mode controls performance by changing CPU and GPU levels."));
        static GUIContent s_AutomaticGameModeEnabledLabel = EditorGUIUtility.TrTextContent(L10n.Tr("Auto Game Mode"), L10n.Tr("Auto Game Mode controls performance by changing target FPS based on device GameMode settings."));
        static GUIContent s_EnableBoostOnStartupLabel = EditorGUIUtility.TrTextContent(L10n.Tr("Boost mode on startup"), L10n.Tr("Enables the CPU and GPU boost mode before engine startup to decrease startup time."));
        static GUIContent s_StatsLoggingFrequencyInFramesLabel = EditorGUIUtility.TrTextContent(L10n.Tr("Logging Frequency"), L10n.Tr("Changes the logging frequency."));
        static GUIContent s_IndexerActiveLabel = EditorGUIUtility.TrTextContent(L10n.Tr("Active"), L10n.Tr("Is indexer enabled."));
        static GUIContent s_IndexerThermalActionDelayLabel = EditorGUIUtility.TrTextContent(L10n.Tr("Thermal Action Delay"), L10n.Tr("Delay after any scaler is applied or unapplied because of thermal state."));
        static GUIContent s_IndexerPerformanceActionDelayLabel = EditorGUIUtility.TrTextContent(L10n.Tr("Performance Action Delay"), L10n.Tr("Delay after any scaler is applied or unapplied because of performance state."));

        static GUIContent s_ScalerScale = EditorGUIUtility.TrTextContent(L10n.Tr("Scale"), L10n.Tr("Scale to control the quality impact for the scaler. No quality change when 1, improved quality when >1, and lowered quality when <1"));
        static GUIContent s_ScalerVisualImpact = EditorGUIUtility.TrTextContent(L10n.Tr("Visual Impact"), L10n.Tr("Visual impact the scaler has on the application. The higher the more impact the scaler has on the visuals."));
        static GUIContent s_ScalerTarget = EditorGUIUtility.TrTextContent(L10n.Tr("Target"), L10n.Tr("Target for the scaler of the application bottleneck. The target selected has the most impact on the quality control of this scaler. Can only be overriden via API."));
        static GUIContent s_ScalerMaxLevel = EditorGUIUtility.TrTextContent(L10n.Tr("Max Level"), L10n.Tr("Maximum level for the scaler. This is tied to the implementation of the scaler to divide the levels into concrete steps."));
        static GUIContent s_ScalerMinBound = EditorGUIUtility.TrTextContent(L10n.Tr("Min Scale"), L10n.Tr("Minimum value for the scale boundary."));
        static GUIContent s_ScalerMaxBound = EditorGUIUtility.TrTextContent(L10n.Tr("Max Scale"), L10n.Tr("Maximum value for the scale boundary."));

        static GUIContent s_AdaptiveFramerate = EditorGUIUtility.TrTextContent(L10n.Tr("Framerate"), L10n.Tr("Adaptive Framerate enables you to automatically control the application's framerate by the defined minimum and maximum framerate. It uses Application.targetFramerate to control the framerate for your application."));
        static GUIContent s_AdaptiveResolution = EditorGUIUtility.TrTextContent(L10n.Tr("Resolution"), L10n.Tr("Adaptive Resolution enables you to automatically control the screen resolution of the application by the defined scale. It uses Dynamic Resolution (Vulkan only) and uses Resolution Scale of the Universal Render Pipeline as fallback if the project uses Universal Render Pipeline."));
        static GUIContent s_AdaptiveBatching = EditorGUIUtility.TrTextContent(L10n.Tr("Batching"), L10n.Tr("Adaptive Batching toggles dynamic batching based on the thermal and performance load."));
        static GUIContent s_AdaptiveLOD = EditorGUIUtility.TrTextContent(L10n.Tr("LOD"), L10n.Tr("Adaptive LOD changes the LOD bias based on the thermal and performance load."));
        static GUIContent s_AdaptiveLut = EditorGUIUtility.TrTextContent(L10n.Tr("LUT"), L10n.Tr("Requires Universal Render Pipeline. Adaptive LUT changes the LUT Bias of the Universal Render Pipeline based on the thermal and performance load."));
        static GUIContent s_AdaptiveMSAA = EditorGUIUtility.TrTextContent(L10n.Tr("MSAA"), L10n.Tr("Requires Universal Render Pipeline. Adaptive MSAA changes the Anti Aliasing Quality Bias of the Universal Render Pipeline based on the thermal and performance load."));
        static GUIContent s_AdaptiveShadowCascade = EditorGUIUtility.TrTextContent(L10n.Tr("Shadow Cascade"), L10n.Tr("Requires Universal Render Pipeline. Adaptive Shadow Cascade changes the Main Light Shadow Cascades Count Bias of the Universal Render Pipeline based on the thermal and performance load."));
        static GUIContent s_AdaptiveShadowDistance = EditorGUIUtility.TrTextContent(L10n.Tr("Shadow Distance"), L10n.Tr("Requires Universal Render Pipeline. Adaptive Shadow Distance changes the Max Shadow Distance Multiplier of the Universal Render Pipeline based on the thermal and performance load."));
        static GUIContent s_AdaptiveShadowmapResolution = EditorGUIUtility.TrTextContent(L10n.Tr("Shadowmap Resolution"), L10n.Tr("Requires Universal Render Pipeline. Adaptive Shadowmap Resolution changes the  Main Light Shadowmap Resolution Multiplier of the Universal Render Pipeline based on the thermal and performance load."));
        static GUIContent s_AdaptiveShadowQuality = EditorGUIUtility.TrTextContent(L10n.Tr("Shadow Quality"), L10n.Tr("Requires Universal Render Pipeline. Adaptive Shadow Quality changes the Shadow Quality Bias of the Universal Render Pipeline based on the thermal and performance load."));
        static GUIContent s_AdaptiveSorting = EditorGUIUtility.TrTextContent(L10n.Tr("Sorting"), L10n.Tr("Requires Universal Render Pipeline. Adaptive Sorting skips the front-to-back sorting of the Universal Render Pipeline based on the thermal and performance load."));
        static GUIContent s_AdaptiveTransparency = EditorGUIUtility.TrTextContent(L10n.Tr("Transparency"), L10n.Tr("Requires Universal Render Pipeline. Adaptive Transparency skips transparent objects render pass."));
        static GUIContent s_AdaptiveViewDistance = EditorGUIUtility.TrTextContent(L10n.Tr("View Distance"), L10n.Tr("Adaptive View Distance changes the view distance of the main camera. Requires the MainCamera tag on the Camera you want to assign."));
        static GUIContent s_AdaptivePhysics = EditorGUIUtility.TrTextContent(L10n.Tr("Physics"), L10n.Tr("Adaptive Physics changes the Time.fixedDeltaTime based on the thermal and performance load."));
        static GUIContent s_AdaptiveDecals = EditorGUIUtility.TrTextContent(L10n.Tr("Decals"), L10n.Tr("Adaptive Decal changes the maximum draw distance for all decals of the Universal Render Pipeline based on the thermal and performance load."));
        static GUIContent s_AdaptiveLayerCulling = EditorGUIUtility.TrTextContent(L10n.Tr("Layer Culling"), L10n.Tr("Adaptive Layer Culling changes the maximum draw distance for each layer based on the thermal and performance load. It scales the value provided by camera.layerCullDistances."));

        static string s_FramerateWarningVSync = L10n.Tr("Adaptive Framerate is only supported without VSync. Set VSync Count to \"Don't Sync\" in Quality settings.");
        static string s_FramerateWarningGameMode = L10n.Tr("Adaptive Framerate is only supported when \"Auto Game Mode\" is turned off.");
        static string s_WarningPopup = L10n.Tr("Warning");
        static string s_WarningPopupMessage = L10n.Tr("Adaptive Performance requires at least one profile to work properly");
        static string s_WarningPopupOption = L10n.Tr("Ok");
        static string s_AdaptiveFramerateMenu = L10n.Tr("Adaptive Framerate");
        static string s_WarningPlaymodePopup = L10n.Tr("Adaptive Performance settings cannot be changed when the Editor is in Play mode.");
        static string s_WarningIndexer = L10n.Tr("You have to enable Adaptive Performance Indexer to use Scaler.");
        static string s_WarningLegacyPackage = L10n.Tr(" Please consider update the legacy provider settings editor to support build profile UI properly. ");

        SerializedProperty m_LoggingProperty;
        SerializedProperty m_AutoPerformanceModeEnabledProperty;
        SerializedProperty m_AutoGameModeEnabledProperty;
        SerializedProperty m_EnableBoostOnStartupProperty;
        SerializedProperty m_StatsLoggingFrequencyInFramesProperty;
        SerializedProperty m_IndexerActiveProperty;
        SerializedProperty m_IndexerThermalActionDelayProperty;
        SerializedProperty m_IndexerPerformanceActionDelayProperty;
        SerializedProperty m_scalerProfileList;
        /// <summary>
        /// Whether to show targetGroupSelection tab when using the default base setting.
        /// User should use this property to conditionally define their UI if they choose to custom the
        /// provider setting UI for each platform and uses the targetGroupSelection tab.
        /// </summary>
        public virtual bool ShowTargetGroupSelection { get; set; } = true;
        /// <summary>
        /// String to show when the provider is not available on this platform.
        /// </summary>
        public virtual string UnsupportedInfo { get; set; } = L10n.Tr("Adaptive Performance Provider not available on this platform");

        /// <summary>
        /// Whether the runtime settings are collapsed or not.
        /// </summary>
        public bool m_ShowRuntimeSettings = true;
        /// <summary>
        /// Whether the development settings are collapsed or not.
        /// </summary>
        public bool m_ShowDevelopmentSettings = true;
        /// <summary>
        /// Whether the indexer settings are collapsed or not.
        /// </summary>
        public bool m_ShowIndexerSettings = true;
        /// <summary>
        /// Whether the scaler settings are collapsed or not.
        /// </summary>
        public bool m_ShowScalerSettings = true;

        /// <summary>
        /// Controls whether or not the 'EnableBoostOnStartup' option is available. Default value is <c>true</c>.
        /// </summary>
        protected virtual bool IsBoostAvailable { get; private set; } = true;
        /// <summary>
        /// Controls whether or not the 'AutomaticPerformanceModeEnabled' option is available. Default value is <c>true</c>.
        /// </summary>
        protected virtual bool IsAutoPerformanceModeAvailable { get; private set; } = true;
        /// <summary>
        /// Controls whether or not the 'AutomaticGameModeEnabled' option is available. Default value is <c>false</c>.
        /// </summary>
        protected virtual bool IsAutoGameModeAvailable { get; private set; } = false;
        /// <summary>
        /// Controls whether or not the 'Indexer/Thermal Action Delay' option is available. Default value is <c>false</c>.
        /// </summary>
        protected virtual bool IsThermalActionDelayAvailable { get; private set; } = true;

        static GUIContent k_ShowRuntimeSettings = EditorGUIUtility.TrTextContent(L10n.Tr("Runtime Settings"));
        static GUIContent k_ShowDevelopmentSettings = EditorGUIUtility.TrTextContent(L10n.Tr("Development Settings"));
        static GUIContent k_ShowIndexerSettings = EditorGUIUtility.TrTextContent(L10n.Tr("Indexer Settings"));
        static GUIContent k_ShowScalerSettings = EditorGUIUtility.TrTextContent(L10n.Tr("Scaler Settings"));
        static GUIContent k_ShowScalerProfiles = EditorGUIUtility.TrTextContent(L10n.Tr("Scaler Profiles"));

        struct ScalerSettingInformation
        {
            public bool showScalerSettings;
        }

        class ScalerProfileSettingInformation
        {
            public bool showScalerProfileSettings;
            public Dictionary<string, ScalerSettingInformation> scalerSettingsInfos = new Dictionary<string, ScalerSettingInformation>();
        }

        static int k_NumberOfScalerProperties = 5;
        static int k_TickboxPosition = 177;

        Dictionary<string, ScalerSettingInformation> m_Scalers = new Dictionary<string, ScalerSettingInformation>();
        Dictionary<string, ScalerProfileSettingInformation> m_ScalerProfiles = new Dictionary<string, ScalerProfileSettingInformation>();

        bool m_PreviousHierarchyMode;
        ReorderableList m_ReorderableList = null;

        /// <summary>
        /// Enables Settings Editor and generates the reorderable list to store all profiles in.
        /// </summary>
        public void OnEnable()
        {
            if (serializedObject == null || serializedObject.targetObject == null)
                return;

            if (m_scalerProfileList == null)
                m_scalerProfileList = serializedObject.FindProperty(k_ScalerProfileList);
            m_ReorderableList = new ReorderableList(serializedObject, m_scalerProfileList, false, true, true, true);
            m_ReorderableList.drawHeaderCallback = DrawHeaderCallback;
            m_ReorderableList.drawElementCallback = DrawElementCallback;
            m_ReorderableList.elementHeightCallback += ElementHeightCallback;
            m_ReorderableList.onRemoveCallback += OnRemoveCallback;
            m_ReorderableList.onAddDropdownCallback += OnNewCallback;
            m_ReorderableList.onCanRemoveCallback += OnCanRemoveCallback;
        }

        /// <summary>
        /// Starts the display block of the base settings. Needs to be called if DisplayBaseRuntimeSettings() or DisplayBaseDeveloperSettings() gets called. Needs to be concluded by a call to DisplayBaseSettingsEnd().
        /// Pass isLegacyAPI = false to hide the legacy warning banner and comply with new APIs.
        /// Default is true (for compatibility).
        /// </summary>
        /// <returns>
        /// False if the settings cannot be loaded. Otherwise true.
        /// </returns>
        public bool DisplayBaseSettingsBegin(bool isLegacyAPI = true)
        {
            if (serializedObject == null || serializedObject.targetObject == null)
                return false;

            serializedObject.Update();

            m_PreviousHierarchyMode = EditorGUIUtility.hierarchyMode;
            EditorGUIUtility.hierarchyMode = false;

            if (m_LoggingProperty == null)
                m_LoggingProperty = serializedObject.FindProperty(k_Logging);
            if (IsAutoPerformanceModeAvailable && m_AutoPerformanceModeEnabledProperty == null)
                m_AutoPerformanceModeEnabledProperty = serializedObject.FindProperty(k_AutoPerformanceModeEnabled);
            if (IsAutoGameModeAvailable && m_AutoGameModeEnabledProperty == null)
                m_AutoGameModeEnabledProperty = serializedObject.FindProperty(k_AutoGameModeEnabled);
            if (IsBoostAvailable && m_EnableBoostOnStartupProperty == null)
                m_EnableBoostOnStartupProperty = serializedObject.FindProperty(k_EnableBoostOnStartup);
            if (m_StatsLoggingFrequencyInFramesProperty == null)
                m_StatsLoggingFrequencyInFramesProperty = serializedObject.FindProperty(k_StatsLoggingFrequencyInFrames);
            var indexerSettings = serializedObject.FindProperty(k_IndexerSettings);
            Debug.Assert(indexerSettings != null);
            if (m_IndexerActiveProperty == null)
                m_IndexerActiveProperty = indexerSettings.FindPropertyRelative(k_IndexerActive);
            if (IsThermalActionDelayAvailable && m_IndexerThermalActionDelayProperty == null)
                m_IndexerThermalActionDelayProperty = indexerSettings.FindPropertyRelative(k_IndexerThermalActionDelay);
            if (m_IndexerPerformanceActionDelayProperty == null)
                m_IndexerPerformanceActionDelayProperty = indexerSettings.FindPropertyRelative(k_IndexerPerformanceActionDelay);

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            if (isLegacyAPI)
            {
                EditorGUILayout.HelpBox(s_WarningLegacyPackage, MessageType.Warning);
                EditorGUILayout.Space();
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox(s_WarningPlaymodePopup, MessageType.Info);
                EditorGUILayout.Space();
            }
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            return true;
        }

        /// <summary>
        /// Ends the display block of the base settings. Needs to be called if DisplayBaseSettingsBegin() is called.
        /// Pass isLegacyAPI = false to comply with new APIs in this class.
        /// Default is true (for compatibility).
        /// </summary>
        public void DisplayBaseSettingsEnd(bool isLegacyAPI = true)
        {
            if (serializedObject == null || serializedObject.targetObject == null)
                return;

            if (isLegacyAPI)
            {
                EditorGUILayout.EndBuildTargetSelectionGrouping(); // Start happens in provider Editor
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();

            EditorGUIUtility.hierarchyMode = m_PreviousHierarchyMode;

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Displays the base runtime settings. Requires DisplayBaseSettingsBegin() to be called before and DisplayBaseSettingsEnd() after as serialization is not taken care of.
        /// </summary>
        public void DisplayBaseRuntimeSettings()
        {
            m_ShowRuntimeSettings = EditorGUILayout.Foldout(m_ShowRuntimeSettings, k_ShowRuntimeSettings, true);
            if (m_ShowRuntimeSettings)
            {
                EditorGUI.indentLevel++;

                if (IsAutoPerformanceModeAvailable)
                    EditorGUILayout.PropertyField(m_AutoPerformanceModeEnabledProperty, s_AutomaticPerformanceModeEnabledLabel);

                if (IsAutoGameModeAvailable)
                    EditorGUILayout.PropertyField(m_AutoGameModeEnabledProperty, s_AutomaticGameModeEnabledLabel);

                if (IsBoostAvailable)
                    EditorGUILayout.PropertyField(m_EnableBoostOnStartupProperty, s_EnableBoostOnStartupLabel);

                DisplayBaseIndexerSettings();
                DisplayScalerSettings();
                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// Displays the base indexer settings. Requires the serializedObject to be updated before and applied after as serialization is not taken care of.
        /// </summary>
        public void DisplayBaseIndexerSettings()
        {
            m_ShowIndexerSettings = EditorGUILayout.Foldout(m_ShowIndexerSettings, k_ShowIndexerSettings, true);
            if (m_ShowIndexerSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_IndexerActiveProperty, s_IndexerActiveLabel);
                GUI.enabled = m_IndexerActiveProperty.boolValue && !EditorApplication.isPlayingOrWillChangePlaymode;
                if (IsThermalActionDelayAvailable)
                {
                    EditorGUILayout.PropertyField(m_IndexerThermalActionDelayProperty,
                        s_IndexerThermalActionDelayLabel);
                }

                EditorGUILayout.PropertyField(m_IndexerPerformanceActionDelayProperty, s_IndexerPerformanceActionDelayLabel);
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// Specify which platform the provider should be supported on.
        /// </summary>
        protected virtual BuildTargetGroup CurrentTargetGroup => BuildTargetGroup.Unknown;

        /// <summary>
        /// Display default common base settings for provider on specific target, which user could choose
        /// to override if they are using DisplayProviderSettings.
        /// </summary>
        protected virtual void DisplayTargetProviderSettings()
        {
            EditorGUIUtility.labelWidth = 180; // some property labels are cut-off
            DisplayBaseRuntimeSettings();
            EditorGUILayout.Space();
            DisplayBaseDeveloperSettings();
        }
        /// <summary>
        /// Default UI for showing provider settings on both project settings and build profile.
        /// </summary>
        protected void DisplayProviderSettings()
        {
            if (!DisplayBaseSettingsBegin(false))
                return;

            if (ShowTargetGroupSelection)
            {
                BuildTargetGroup selectedBuildTargetGroup = EditorGUILayout.BeginBuildTargetSelectionGrouping();
                if (selectedBuildTargetGroup == CurrentTargetGroup)
                {
                    DisplayTargetProviderSettings();
                }
                else
                {
                    EditorGUILayout.HelpBox(UnsupportedInfo, MessageType.Info);
                    EditorGUILayout.Space();
                }
            }
            else
            {
                DisplayTargetProviderSettings();
            }


            if(ShowTargetGroupSelection)
                EditorGUILayout.EndBuildTargetSelectionGrouping(); // Start happens in provider Editor
            DisplayBaseSettingsEnd(false);
        }

        /// <summary>
        /// Displays the base scaler settings. Requires the serializedObject to be updated before and applied after as serialization is not taken care of.
        /// </summary>
        public void DisplayScalerSettings()
        {
            GUI.enabled = m_IndexerActiveProperty.boolValue && !EditorApplication.isPlayingOrWillChangePlaymode;
            m_ShowScalerSettings = EditorGUILayout.Foldout(m_ShowScalerSettings, k_ShowScalerSettings, true);
            if (m_ShowScalerSettings)
            {
                if (!m_IndexerActiveProperty.boolValue)
                {
                    EditorGUILayout.HelpBox(s_WarningIndexer, MessageType.Info);
                    EditorGUILayout.Space();
                }
                else
                {
                    m_ReorderableList.DoLayoutList();
                }
            }
            GUI.enabled = true;
        }

        void DrawHeaderCallback(Rect rect)
        {
            rect.x += 10;
            EditorGUI.LabelField(rect, k_ShowScalerProfiles);
        }

        void DrawElementCallback(Rect rect, int index, bool isactive, bool isfocused)
        {
            SerializedProperty element = m_ReorderableList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;

            var name = element.FindPropertyRelative(k_ScalerName).stringValue;

            ScalerProfileSettingInformation scalerProfileSettingInfo;
            if (!m_ScalerProfiles.TryGetValue(name, out scalerProfileSettingInfo))
            {
                scalerProfileSettingInfo = new ScalerProfileSettingInformation()
                {
                    showScalerProfileSettings = false
                };
            }

            rect.width -= 6;
            rect.height = EditorGUIUtility.singleLineHeight;

            scalerProfileSettingInfo.showScalerProfileSettings = EditorGUI.Foldout(rect, scalerProfileSettingInfo.showScalerProfileSettings, new GUIContent($"{name}"), true);
            if (scalerProfileSettingInfo.showScalerProfileSettings)
            {
                AdaptivePerformanceScalerSettings settingsObject = new AdaptivePerformanceScalerSettings();
                Type settingsType = settingsObject.GetType();
                MemberInfo[] memberInfo = settingsType.GetProperties();
                rect.x += 10;
                rect.width -= 10;
                for (int i = 0; i < memberInfo.Length; i++)
                {
                    if (memberInfo[i].Name == "AdaptiveShadowCascades") // ap-obsolete-001 due to renaming the property
                        continue;
                    var scalerSetting = element.FindPropertyRelative($"m_{memberInfo[i].Name}");
                    rect = DrawScalerSetting(rect, scalerSetting, m_IndexerActiveProperty.boolValue && !EditorApplication.isPlayingOrWillChangePlaymode, scalerProfileSettingInfo);
                }
            }
            m_ScalerProfiles[name] = scalerProfileSettingInfo;
        }

        void OnNewCallback(Rect buttonRect, ReorderableList list)
        {
            buttonRect.x -= 400;
            buttonRect.y -= 13;
            PopupWindow.Show(buttonRect, new EnterNamePopup(m_scalerProfileList, s => {
                var index = list.serializedProperty.arraySize;
                list.serializedProperty.arraySize++;
                list.index = index;
                var element = list.serializedProperty.GetArrayElementAtIndex(index);
                element.FindPropertyRelative(k_ScalerName).stringValue = s;
                if (serializedObject.hasModifiedProperties)
                {
                    serializedObject.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                }
            }));
        }

        void OnRemoveCallback(ReorderableList list)
        {
            if (list.count <= 1)
            {
                EditorUtility.DisplayDialog(s_WarningPopup, s_WarningPopupMessage, s_WarningPopupOption);
            }
            else
            {
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
            }
        }

        bool OnCanRemoveCallback(ReorderableList list)
        {
            return list.count > 0;
        }

        // Adaptive Framerate scaler should be automatically disabled in case of using vSync or when fps is conrolled by device GameMode
        bool DisabledAdaptiveFramerateScaler(string scalerName)
        {
            bool automode = IsAutoGameModeAvailable ? m_AutoGameModeEnabledProperty.boolValue : false;
            return (scalerName == s_AdaptiveFramerateMenu && (QualitySettings.vSyncCount > 0 || automode));
        }

        float ElementHeightCallback(int index)
        {
            var name = m_ReorderableList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative(k_ScalerName).stringValue;

            var height = EditorGUIUtility.singleLineHeight;

            ScalerProfileSettingInformation scalerProfileSettingInfo;
            m_ScalerProfiles.TryGetValue(name, out scalerProfileSettingInfo);
            if (scalerProfileSettingInfo != null && scalerProfileSettingInfo.showScalerProfileSettings)
            {
                height += scalerProfileSettingInfo.scalerSettingsInfos.Count * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

                AdaptivePerformanceScalerSettings settingsObject = new AdaptivePerformanceScalerSettings();
                Type settingsType = settingsObject.GetType();
                MemberInfo[] memberInfo = settingsType.GetProperties();
                for (int i = 0; i < memberInfo.Length; i++)
                {
                    if (memberInfo[i].Name == "AdaptiveShadowCascades") // ap-obsolete-001 due to renaming the property
                        continue;

                    ScalerSettingInformation scalerSettingInfo;

                    var scalerSetting = m_ReorderableList.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative($"m_{memberInfo[i].Name}");
                    string scalerName = scalerSetting.FindPropertyRelative(k_ScalerName).stringValue;
                    scalerProfileSettingInfo.scalerSettingsInfos.TryGetValue(scalerName, out scalerSettingInfo);

                    if (scalerSettingInfo.showScalerSettings && scalerSetting.FindPropertyRelative(k_ScalerEnabled).boolValue)
                    {
                        height += k_NumberOfScalerProperties * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                    }
                    if (DisabledAdaptiveFramerateScaler(scalerName))
                    {
                        if (scalerSettingInfo.showScalerSettings && !scalerSetting.FindPropertyRelative(k_ScalerEnabled).boolValue) // if before was not executed due to scaler not enabled, but we need the height.
                        {
                            height += k_NumberOfScalerProperties * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                        }
                        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    }
                }
            }
            return height;
        }

        Rect DrawScalerSetting(Rect rect, SerializedProperty scalerSetting, bool renderNotDisabled, ScalerProfileSettingInformation scalerProfileSettingInfo)
        {
            string name = scalerSetting.FindPropertyRelative(k_ScalerName).stringValue;
            var isEnabled = renderNotDisabled && !EditorApplication.isPlayingOrWillChangePlaymode;

            if (DisabledAdaptiveFramerateScaler(name))
            {
                isEnabled = false;
            }

            GUI.enabled = isEnabled;
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            ScalerSettingInformation scalerSettingInfo;
            if (!scalerProfileSettingInfo.scalerSettingsInfos.TryGetValue(name, out scalerSettingInfo))
            {
                scalerSettingInfo = new ScalerSettingInformation()
                {
                    showScalerSettings = false
                };
            }

            rect.x += k_TickboxPosition;
            var needsFoldout = scalerSetting.FindPropertyRelative(k_ScalerEnabled).boolValue;
            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUI.Toggle(rect, GUIContent.none, needsFoldout);
            if (EditorGUI.EndChangeCheck())
            {
                needsFoldout = newValue;
                if (newValue)
                    scalerSettingInfo.showScalerSettings = newValue;
            }
            scalerSetting.FindPropertyRelative(k_ScalerEnabled).boolValue = needsFoldout;
            rect.x -= k_TickboxPosition;

            if (needsFoldout || !isEnabled)
            {
                EditorGUI.BeginChangeCheck();
                var newShowScalerSettings = EditorGUI.Foldout(rect, scalerSettingInfo.showScalerSettings, ReturnScalerGUIContent(name), true);
                if (EditorGUI.EndChangeCheck())
                    scalerSettingInfo.showScalerSettings = newShowScalerSettings;
            }
            else
                EditorGUI.LabelField(rect, ReturnScalerGUIContent(name));

            if ((needsFoldout || !isEnabled) && scalerSettingInfo.showScalerSettings)
            {
                rect.x += 10;
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                if (DisabledAdaptiveFramerateScaler(name))
                {
                    GUI.enabled = true;
                    rect.x += 10;
                    rect.width -= 10;
                    rect.height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    bool automode = IsAutoGameModeAvailable ? m_AutoGameModeEnabledProperty.boolValue : false;
                    var framerateWarning = (QualitySettings.vSyncCount > 0 && automode) ?
                        s_FramerateWarningVSync + "\n" + s_FramerateWarningGameMode :
                        (QualitySettings.vSyncCount > 0 ? s_FramerateWarningVSync : s_FramerateWarningGameMode);
                    EditorGUI.HelpBox(rect, framerateWarning, MessageType.Warning);
                    rect.height -= EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    rect.x -= 10;
                    rect.width += 10;
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    GUI.enabled = isEnabled;
                }

                var minBound = scalerSetting.FindPropertyRelative(k_ScalerMinBound).floatValue;
                var maxBound = scalerSetting.FindPropertyRelative(k_ScalerMaxBound).floatValue;

                EditorGUI.BeginChangeCheck();
                float newMinBound = EditorGUI.FloatField(rect, s_ScalerMinBound, minBound);
                if (EditorGUI.EndChangeCheck())
                {
                    minBound = Mathf.Clamp(newMinBound, 0, maxBound);
                }
                scalerSetting.FindPropertyRelative(k_ScalerMinBound).floatValue = minBound;

                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.BeginChangeCheck();
                float newMaxBound = EditorGUI.FloatField(rect, s_ScalerMaxBound, maxBound);
                if (EditorGUI.EndChangeCheck())
                {
                    maxBound = Mathf.Clamp(newMaxBound, minBound, 10000);
                }
                scalerSetting.FindPropertyRelative(k_ScalerMaxBound).floatValue = maxBound;

                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                var maxLevel = scalerSetting.FindPropertyRelative(k_ScalerMaxLevel).intValue;
                EditorGUI.BeginChangeCheck();
                int newMaxLevel = EditorGUI.IntField(rect, s_ScalerMaxLevel, maxLevel);
                if (EditorGUI.EndChangeCheck())
                {
                    maxLevel = Mathf.Clamp(newMaxLevel, 1, 100);
                }
                scalerSetting.FindPropertyRelative(k_ScalerMaxLevel).intValue = maxLevel;

                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                ScalerVisualImpact visualImpact = (ScalerVisualImpact)scalerSetting.FindPropertyRelative(k_ScalerVisualImpact).enumValueIndex;
                EditorGUI.BeginChangeCheck();
                ScalerVisualImpact newVisualImpact = (ScalerVisualImpact)EditorGUI.EnumPopup(rect, s_ScalerVisualImpact, visualImpact);
                if (EditorGUI.EndChangeCheck())
                {
                    visualImpact = (ScalerVisualImpact)Mathf.Clamp((int)newVisualImpact, (int)ScalerVisualImpact.Low, (int)ScalerVisualImpact.High);
                }
                scalerSetting.FindPropertyRelative(k_ScalerVisualImpact).enumValueIndex = (int)visualImpact;

                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                ScalerTarget staticFlagMask = (ScalerTarget)scalerSetting.FindPropertyRelative(k_ScalerTarget).intValue;
                GUIContent propDisplayNames = new GUIContent("");
                foreach (var enumValue in Enum.GetValues(typeof(ScalerTarget)))
                {
                    int checkBit = (int)staticFlagMask & (int)enumValue;
                    if (checkBit != 0)
                    {
                        propDisplayNames.text += propDisplayNames.text.Length != 0 ? " | " : "";
                        propDisplayNames.text += enumValue.ToString();
                    }
                }
                EditorGUI.LabelField(rect, s_ScalerTarget, propDisplayNames);

                rect.x -= 10;
            }
            scalerProfileSettingInfo.scalerSettingsInfos[name] = scalerSettingInfo;
            return rect;
        }

        GUIContent ReturnScalerGUIContent(string scalerName)
        {
            switch (scalerName)
            {
                case "Adaptive Framerate":
                    return s_AdaptiveFramerate;
                case "Adaptive Resolution":
                    return s_AdaptiveResolution;
                case "Adaptive Batching":
                    return s_AdaptiveBatching;
                case "Adaptive LOD":
                    return s_AdaptiveLOD;
                case "Adaptive Lut":
                    return s_AdaptiveLut;
                case "Adaptive MSAA":
                    return s_AdaptiveMSAA;
                case "Adaptive Shadow Cascade":
                    return s_AdaptiveShadowCascade;
                case "Adaptive Shadow Distance":
                    return s_AdaptiveShadowDistance;
                case "Adaptive Shadowmap Resolution":
                    return s_AdaptiveShadowmapResolution;
                case "Adaptive Shadow Quality":
                    return s_AdaptiveShadowQuality;
                case "Adaptive Sorting":
                    return s_AdaptiveSorting;
                case "Adaptive Transparency":
                    return s_AdaptiveTransparency;
                case "Adaptive View Distance":
                    return s_AdaptiveViewDistance;
                case "Adaptive Physics":
                    return s_AdaptivePhysics;
                case "Adaptive Decals":
                    return s_AdaptiveDecals;
                case "Adaptive Layer Culling":
                    return s_AdaptiveLayerCulling;

                default:
                    return new GUIContent("");
            }
        }

        /// <summary>
        /// Displays the base developer settings. Requires DisplayBaseSettingsBegin() to be called before and DisplayBaseSettingsEnd() after as serialization is not taken care of.
        /// </summary>
        public void DisplayBaseDeveloperSettings()
        {
            GUI.enabled = !EditorApplication.isPlayingOrWillChangePlaymode;
            m_ShowDevelopmentSettings = EditorGUILayout.Foldout(m_ShowDevelopmentSettings, k_ShowDevelopmentSettings, true);
            if (m_ShowDevelopmentSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_LoggingProperty, s_LoggingLabel);
                EditorGUILayout.PropertyField(m_StatsLoggingFrequencyInFramesProperty, s_StatsLoggingFrequencyInFramesLabel);
                EditorGUI.indentLevel--;
            }
            GUI.enabled = true;
        }
    }
}
