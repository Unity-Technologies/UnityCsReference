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
using Object = System.Object;

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
        static int k_TickboxPosition = 227;

        Dictionary<string, ScalerProfileSettingInformation> m_ScalerProfiles = new Dictionary<string, ScalerProfileSettingInformation>();

        bool m_PreviousHierarchyMode;

        List<bool> m_FoldoutState = new List<bool>();
        private int m_SelectedProfileIndex = -1;
        // index represents default scalers + custom scalers per scaler profile.
        List<List<int>> m_IndexLists = new List<List<int>>();
        IAdaptivePerformanceSettings m_CurrentSettings;
        List<ReorderableList> m_scalerList = new List<ReorderableList>();
        List<List<AdaptivePerformanceScaler>> m_FieldObjects = new List<List<AdaptivePerformanceScaler>>();


        List<int> GetIndexListForProfile(AdaptivePerformanceScalerProfile profile)
        {
            var indexList = new List<int>();
            for (int j = 0; j < profile.DefaultScalerSettings.Count; j++)
            {
                indexList.Add(j);
            }

            for (int j = 0; j < profile.AddedScalers.Count; j++)
            {
                indexList.Add(profile.DefaultScalerSettings.Count + j);
            }

            return indexList;
        }

        void AddNewReorderableList(List<int> list)
        {
            var newReorderableListDefaultSettings = new ReorderableList(list, typeof(int), false, false, true, true);
            newReorderableListDefaultSettings.onAddDropdownCallback += OnNewCustomScalerCallback;
            newReorderableListDefaultSettings.onRemoveCallback += OnRemoveCustomScalerCallback;

            newReorderableListDefaultSettings.onCanRemoveCallback += OnCanRemoveCustomScalerCallback;
            newReorderableListDefaultSettings.drawNoneElementCallback += OnEmptyCustomScalerList;
            newReorderableListDefaultSettings.drawElementCallback = DrawScalerElementCallback;
            newReorderableListDefaultSettings.elementHeightCallback += ScalerElementHeightCallback;
            m_scalerList.Add(newReorderableListDefaultSettings);
        }

        /// <summary>
        /// Enables Settings Editor and generates the reorderable list to store all profiles in.
        /// </summary>
        public void OnEnable()
        {
            if (serializedObject == null || serializedObject.targetObject == null)
                return;
            m_FoldoutState.Clear();
            m_FieldObjects.Clear();
            m_scalerList.Clear();
            m_IndexLists.Clear();
            m_CurrentSettings = serializedObject.targetObject as IAdaptivePerformanceSettings;
            for (int i = 0; i < m_CurrentSettings.ScalerProfiles.Length; i++)
            {
                m_FoldoutState.Add(false);
                m_FieldObjects.Add(new List<AdaptivePerformanceScaler>());
                for (int j = 0; j < m_CurrentSettings.ScalerProfiles[i].AddedScalers.Count; j++)
                {
                    m_FieldObjects[i].Add(m_CurrentSettings.ScalerProfiles[i].AddedScalers[j]);
                }

                var indexList =  GetIndexListForProfile(m_CurrentSettings.ScalerProfiles[i]);
                m_IndexLists.Add(indexList);
                AddNewReorderableList(indexList);
            }

            if (m_scalerProfileList == null)
                m_scalerProfileList = serializedObject.FindProperty(k_ScalerProfileList);
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

        void OnEmptyCustomScalerList(Rect rect)
        {
            float midPoint = (rect.xMax - rect.xMin) / 2;
            Rect midRect = new Rect(rect.x + midPoint - 40, rect.y + 1, rect.width, rect.height);
            GUI.Label(midRect, "Added Custom Scalers Appear Here");
        }

        /// <summary>
        /// Displays the base scaler settings. Requires the serializedObject to be updated before and applied after as serialization is not taken care of.
        /// </summary>
        public void DisplayScalerSettings()
        {
            GUI.enabled = m_IndexerActiveProperty.boolValue && !EditorApplication.isPlayingOrWillChangePlaymode;
            m_ShowScalerSettings = EditorGUILayout.Foldout(m_ShowScalerSettings, k_ShowScalerProfiles, true);
            var currentSetting = m_CurrentSettings;
            if (m_ShowScalerSettings)
            {
                if (!m_IndexerActiveProperty.boolValue)
                {
                    EditorGUILayout.HelpBox(s_WarningIndexer, MessageType.Info);
                    EditorGUILayout.Space();
                }
                else
                {
                    for (int i = 0; i < currentSetting.ScalerProfiles.Length; i++)
                    {
                        GUIContent content = new GUIContent(currentSetting.ScalerProfiles[i].Name);
                        EditorGUI.indentLevel++;
                        EditorGUILayout.BeginHorizontal();
                        Rect foldoutRect = EditorGUILayout.GetControlRect();
                        var style = new GUIStyle(EditorStyles.foldout);
                        style.clipping = TextClipping.Ellipsis;
                        m_FoldoutState[i] = EditorGUI.Foldout(foldoutRect, m_FoldoutState[i], content, true, style);
                        GUILayout.FlexibleSpace();
                        GUIStyle menuButton = "WindowMenuButton";

                        if (EditorGUILayout.DropdownButton(GUIContent.none, FocusType.Passive, menuButton))
                        {
                            GenericMenu menu = new GenericMenu();
                            menu.AddItem(EditorGUIUtility.TrTextContent("Remove"), false, (tmp) =>
                            {
                                int index = (int)tmp;
                                if (currentSetting.ScalerProfiles.Length == 1)
                                {
                                    EditorUtility.DisplayDialog(s_WarningPopup, s_WarningPopupMessage, s_WarningPopupOption);
                                }
                                else
                                {
                                    currentSetting.DeleteScalerProfileAt(index);
                                    m_FoldoutState.RemoveAt(index);
                                    m_scalerList.RemoveAt(index);
                                    m_FieldObjects.RemoveAt(index);
                                    m_IndexLists.RemoveAt(index);
                                    UpdateAssetOnDiskAndInMemory();
                                }
                            }, i);
                            menu.ShowAsContext();
                        }
                        EditorGUILayout.EndHorizontal();
                        if (m_FoldoutState[i])
                        {
                            m_SelectedProfileIndex = i;
                            var reorderableListDefaultSettings = m_scalerList[i];
                            reorderableListDefaultSettings.list = m_IndexLists[i];

                            Rect controlRect = EditorGUILayout.GetControlRect(true, reorderableListDefaultSettings.GetHeight());
                            Rect indentedRect = new Rect(controlRect.x + 30f, controlRect.y, controlRect.width - 30f, controlRect.height);

                            reorderableListDefaultSettings.DoList(indentedRect);
                        }
                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUILayout.BeginVertical();
                var rect = EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                float midPoint = (rect.xMax - rect.xMin) / 2;
                Rect midRect = new Rect(rect.x + midPoint, rect.y, rect.width, rect.height);
                if(GUILayout.Button("Add New Scaler Profile", GUILayout.Width(160))) {
                    PopupWindow.Show(midRect, new EnterNamePopup(m_scalerProfileList, s => {
                        currentSetting.AddScalerProfileWithDefaultScalers(s);
                        m_FoldoutState.Add(false);
                        m_FieldObjects.Add(new List<AdaptivePerformanceScaler>());
                        var indexList = GetIndexListForProfile(currentSetting.ScalerProfiles[^1]);
                        m_IndexLists.Add(indexList);
                        AddNewReorderableList(indexList);
                        UpdateAssetOnDiskAndInMemory();
                    }));                    }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            GUI.enabled = true;
        }

        void DrawScalerElementCallback(Rect rect, int index, bool isactive, bool isfocused)
        {
            rect.y += 2;
            var settingsObject = serializedObject.targetObject as IAdaptivePerformanceSettings;
            var scalerProfile = settingsObject.ScalerProfiles[m_SelectedProfileIndex];

            ScalerProfileSettingInformation scalerProfileSettingInfo;
            if (!m_ScalerProfiles.TryGetValue(scalerProfile.Name, out scalerProfileSettingInfo))
            {
                scalerProfileSettingInfo = new ScalerProfileSettingInformation() { showScalerProfileSettings = false };
            }
            rect.width -= 6;
            rect.height = EditorGUIUtility.singleLineHeight;
            rect.x += 10;
            rect.width -= 10;

            if (index >= scalerProfile.DefaultScalerSettings.Count)
            {
                var newIndex = index - scalerProfile.DefaultScalerSettings.Count;
                {
                    var objectRowRect = new Rect(rect.x, rect.y, rect.width, rect.height);
                    objectRowRect.width = 200;
                    if (m_FieldObjects[m_SelectedProfileIndex][newIndex] != null && m_FieldObjects[m_SelectedProfileIndex][newIndex].Enabled)
                        objectRowRect.x += 20;
                    var newObject = (AdaptivePerformanceScaler)EditorGUI.ObjectField(objectRowRect, m_FieldObjects[m_SelectedProfileIndex][newIndex], typeof(AdaptivePerformanceScaler), true);
                    if (newObject)
                    {
                        bool isDuplicate = false;
                        for (int i = 0; i < scalerProfile.AddedScalers.Count; i++)
                        {
                            var addedScaler = scalerProfile.AddedScalers[i];
                            if (i != newIndex && addedScaler != null && ((addedScaler == newObject) ||
                                                                         (addedScaler.Name == newObject.name)))
                            {
                                isDuplicate = true;
                                break;
                            }
                        }

                        // User needs to click confirm to finalize the selection and get rid of the object field.
                        // This is a workaround for objectField could not distinguish drag and drop by event since the event is used and does not propagate to here.
                        //var button = GUI.Button(objectRowRect, new GUIContent("Confirm"));
                        if (!isDuplicate && newObject !=  m_FieldObjects[m_SelectedProfileIndex][newIndex])
                        {
                            var copyObject = Instantiate(newObject);
                            m_FieldObjects[m_SelectedProfileIndex][newIndex] = copyObject;
                            m_FieldObjects[m_SelectedProfileIndex][newIndex].hideFlags = HideFlags.HideInHierarchy;
                            m_FieldObjects[m_SelectedProfileIndex][newIndex].Name = newObject.name;
                            scalerProfile.AddedScalers[newIndex] = copyObject;
                            scalerProfile.AddedScalers[newIndex].DefaultSetting.name = newObject.name;
                            AssetDatabase.AddObjectToAsset(copyObject, serializedObject.targetObject);
                            UpdateAssetOnDiskAndInMemory();
                        }
                        else if(isDuplicate)
                        {
                            EditorUtility.DisplayDialog(s_WarningPopup, L10n.Tr("The Adaptive Performance Scaler named " + newObject.name + " already exists. Please rename and try again."), s_WarningPopupOption);
                            m_FieldObjects[m_SelectedProfileIndex][newIndex] = null;
                            UpdateAssetOnDiskAndInMemory();
                        }
                    }
                }

                if (scalerProfile.AddedScalers[newIndex] != null)
                {
                    rect = DrawScalerSetting(rect, scalerProfile.AddedScalers[newIndex].DefaultSetting, m_IndexerActiveProperty.boolValue && !EditorApplication.isPlayingOrWillChangePlaymode, scalerProfileSettingInfo, true);
                }
            }
            else
            {
                var scalerSetting = settingsObject.ScalerProfiles[m_SelectedProfileIndex].DefaultScalerSettings[index];
                var scalerName = scalerSetting.name;
                if (scalerName == "AdaptiveShadowCascades")
                {
                    return;
                }
                rect = DrawScalerSetting(rect, scalerSetting, m_IndexerActiveProperty.boolValue && !EditorApplication.isPlayingOrWillChangePlaymode, scalerProfileSettingInfo);
            }
            m_ScalerProfiles[settingsObject.ScalerProfiles[m_SelectedProfileIndex].Name] = scalerProfileSettingInfo;
        }

        void OnNewCustomScalerCallback(Rect buttonRect, ReorderableList list)
        {
            buttonRect.x -= 400;
            buttonRect.y -= 13;

            m_FieldObjects[m_SelectedProfileIndex].Add(null);
            m_CurrentSettings.ScalerProfiles[m_SelectedProfileIndex].AddedScalers.Add(null);
            m_IndexLists[m_SelectedProfileIndex].Add(m_IndexLists[m_SelectedProfileIndex].Count);
            UpdateAssetOnDiskAndInMemory();
        }

        void UpdateAssetOnDiskAndInMemory()
        {
            EditorUtility.SetDirty(serializedObject.targetObject);
            AssetDatabase.SaveAssetIfDirty(serializedObject.targetObject);
            serializedObject.Update();
        }

        void OnRemoveCustomScalerCallback(ReorderableList list)
        {
            var selectedIndex = list.index;
            var removeIndex = list.index;
            var defaultScalerCount = m_CurrentSettings.ScalerProfiles[m_SelectedProfileIndex].DefaultScalerSettings.Count;
            // Remove custom scaler only. Last element if no selection, or remove the selected item. Move the selection pointer to before the removed element.
            if (selectedIndex == -1)
            {
                removeIndex = m_CurrentSettings.ScalerProfiles[m_SelectedProfileIndex].AddedScalers.Count - 1;
            }
            else
            {
                selectedIndex = list.index - defaultScalerCount;
                if (selectedIndex < 0) return;
                removeIndex = selectedIndex;
            }

            DestroyImmediate(m_CurrentSettings.ScalerProfiles[m_SelectedProfileIndex].AddedScalers[removeIndex], true);
            m_CurrentSettings.ScalerProfiles[m_SelectedProfileIndex].AddedScalers.RemoveAt(removeIndex);
            m_FieldObjects[m_SelectedProfileIndex].RemoveAt(removeIndex);
            m_IndexLists[m_SelectedProfileIndex].RemoveAt(removeIndex + defaultScalerCount);

            // move pointer in the global index for 1 position up.
            list.index = Math.Clamp(removeIndex + defaultScalerCount - 1, 0, list.count - 1);
            UpdateAssetOnDiskAndInMemory();
        }

        bool OnCanRemoveCustomScalerCallback(ReorderableList list)
        {
            if (list.index == -1)
            {
                return m_CurrentSettings.ScalerProfiles[m_SelectedProfileIndex].AddedScalers.Count > 0;
            }

            int selectedIndex = list.index - m_CurrentSettings.ScalerProfiles[m_SelectedProfileIndex].DefaultScalerSettings.Count;
            return m_CurrentSettings.ScalerProfiles[m_SelectedProfileIndex].AddedScalers.Count > 0 && selectedIndex >= 0;
        }

        // Adaptive Framerate scaler should be automatically disabled in case of using vSync or when fps is conrolled by device GameMode
        bool DisabledAdaptiveFramerateScaler(string scalerName)
        {
            bool automode = IsAutoGameModeAvailable ? m_AutoGameModeEnabledProperty.boolValue : false;
            return (scalerName == s_AdaptiveFramerateMenu && (QualitySettings.vSyncCount > 0 || automode));
        }

        float ScalerElementHeightCallback(int index)
        {
            var settingsObject = serializedObject.targetObject as IAdaptivePerformanceSettings;
            var scalerProfile = settingsObject.ScalerProfiles[m_SelectedProfileIndex];
            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            ScalerProfileSettingInformation scalerProfileSettingInfo;
            m_ScalerProfiles.TryGetValue(scalerProfile.Name, out scalerProfileSettingInfo);

            if (index < scalerProfile.DefaultScalerSettings.Count)
            {
                var scalerSetting = scalerProfile.DefaultScalerSettings[index];

                var scalerName = scalerSetting.name;
                if (scalerName == "AdaptiveShadowCascades") // ap-obsolete-001 due to renaming the property
                    return height;

                if (scalerProfileSettingInfo != null)
                {
                    ScalerSettingInformation scalerSettingInfo;
                    scalerProfileSettingInfo.scalerSettingsInfos.TryGetValue(scalerName, out scalerSettingInfo);
                    if (scalerSettingInfo.showScalerSettings && scalerSetting.enabled)
                    {
                        height += k_NumberOfScalerProperties * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                    }

                    if (DisabledAdaptiveFramerateScaler(scalerName))
                    {
                        if (scalerSettingInfo.showScalerSettings && !scalerSetting.enabled) // if before was not executed due to scaler not enabled, but we need the height.
                        {
                            height += k_NumberOfScalerProperties * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                        }

                        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    }
                }
                return height;
            }
            else
            {
                var newIndex = index - scalerProfile.DefaultScalerSettings.Count;
                if (newIndex < scalerProfile.AddedScalers.Count)
                {
                    var addedScaler = scalerProfile.AddedScalers[newIndex];
                    if (addedScaler == null) return height;

                    var scalerSetting = addedScaler.DefaultSetting;
                    var scalerName = scalerSetting.name;

                    if (scalerProfileSettingInfo != null)
                    {
                        ScalerSettingInformation scalerSettingInfo;
                        scalerProfileSettingInfo.scalerSettingsInfos.TryGetValue(scalerName, out scalerSettingInfo);
                        if (scalerSettingInfo.showScalerSettings && scalerSetting.enabled)
                        {
                            height += k_NumberOfScalerProperties * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                        }
                    }
                    return height;
                }
                else
                {
                    return 0;
                }
            }
        }

        Rect DrawScalerSetting(Rect rect, AdaptivePerformanceScalerSettingsBase scalerSetting, bool renderNotDisabled, ScalerProfileSettingInformation scalerProfileSettingInfo, bool isCustomScaler = false)
        {
            string scalerName = scalerSetting.name;
            var isEnabled = renderNotDisabled && !EditorApplication.isPlayingOrWillChangePlaymode;

            if (DisabledAdaptiveFramerateScaler(scalerName))
            {
                isEnabled = false;
            }

            GUI.enabled = isEnabled;

            ScalerSettingInformation scalerSettingInfo;
            if (!scalerProfileSettingInfo.scalerSettingsInfos.TryGetValue(scalerName, out scalerSettingInfo))
            {
                scalerSettingInfo = new ScalerSettingInformation()
                {
                    showScalerSettings = false
                };
            }

            rect.x += k_TickboxPosition;
            var needsFoldout = scalerSetting.enabled;
            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUI.Toggle(rect, GUIContent.none, needsFoldout);
            if (EditorGUI.EndChangeCheck())
            {
                needsFoldout = newValue;
                if (newValue)
                    scalerSettingInfo.showScalerSettings = newValue;
            }
            scalerSetting.enabled = needsFoldout;
            rect.x -= k_TickboxPosition;

            if ((needsFoldout || !isEnabled || (isCustomScaler && needsFoldout)))
            {
                EditorGUI.BeginChangeCheck();
                var style = new  GUIStyle(EditorStyles.foldout);
                style.clipping = TextClipping.Ellipsis;
                var newShowScalerSettings = EditorGUI.Foldout(rect, scalerSettingInfo.showScalerSettings, isCustomScaler? new GUIContent("") : ReturnScalerGUIContent(scalerName), true, style);
                if (EditorGUI.EndChangeCheck())
                    scalerSettingInfo.showScalerSettings = newShowScalerSettings;
            }
            else if(!isCustomScaler)
            {
                var clipping = EditorStyles.label.clipping;
                EditorStyles.label.clipping = TextClipping.Ellipsis;
                EditorGUI.LabelField(rect, ReturnScalerGUIContent(scalerName));
                EditorStyles.label.clipping = clipping;
            }

            if ((needsFoldout || !isEnabled) && scalerSettingInfo.showScalerSettings)
            {
                rect.x += 10;
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                if (DisabledAdaptiveFramerateScaler(scalerName))
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

                var minBound = scalerSetting.minBound;
                var maxBound = scalerSetting.maxBound;

                EditorGUI.BeginChangeCheck();
                float newMinBound = EditorGUI.FloatField(rect, s_ScalerMinBound, minBound);
                if (EditorGUI.EndChangeCheck())
                {
                    minBound = Mathf.Clamp(newMinBound, 0, maxBound);
                }
                scalerSetting.minBound = minBound;

                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.BeginChangeCheck();
                float newMaxBound = EditorGUI.FloatField(rect, s_ScalerMaxBound, maxBound);
                if (EditorGUI.EndChangeCheck())
                {
                    maxBound = Mathf.Clamp(newMaxBound, minBound, 10000);
                }
                scalerSetting.maxBound = maxBound;

                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                var maxLevel = scalerSetting.maxLevel;
                EditorGUI.BeginChangeCheck();
                int newMaxLevel = EditorGUI.IntField(rect, s_ScalerMaxLevel, maxLevel);
                if (EditorGUI.EndChangeCheck())
                {
                    maxLevel = Mathf.Clamp(newMaxLevel, 1, 100);
                }
                scalerSetting.maxLevel = maxLevel;

                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                ScalerVisualImpact visualImpact = (ScalerVisualImpact)scalerSetting.visualImpact;
                EditorGUI.BeginChangeCheck();
                ScalerVisualImpact newVisualImpact = (ScalerVisualImpact)EditorGUI.EnumPopup(rect, s_ScalerVisualImpact, visualImpact);
                if (EditorGUI.EndChangeCheck())
                {
                    visualImpact = (ScalerVisualImpact)Mathf.Clamp((int)newVisualImpact, (int)ScalerVisualImpact.Low, (int)ScalerVisualImpact.High);
                }
                scalerSetting.visualImpact= visualImpact;

                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                ScalerTarget staticFlagMask = scalerSetting.target;
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
            scalerProfileSettingInfo.scalerSettingsInfos[scalerName] = scalerSettingInfo;
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
                    return new GUIContent(scalerName);
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
