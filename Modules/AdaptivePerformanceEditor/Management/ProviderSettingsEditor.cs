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
using Unity.Scripting.LifecycleManagement;
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

        static readonly GUIContent s_LoggingLabel = EditorGUIUtility.TrTextContent(L10n.Tr("Logging", null), L10n.Tr("Only active in development mode.", null));
        static readonly GUIContent s_AutomaticPerformanceModeEnabledLabel = EditorGUIUtility.TrTextContent(L10n.Tr("Auto Performance Mode", null), L10n.Tr("Auto Performance Mode controls performance by changing CPU and GPU levels.", null));
        static readonly GUIContent s_AutomaticGameModeEnabledLabel = EditorGUIUtility.TrTextContent(L10n.Tr("Auto Game Mode", null), L10n.Tr("Auto Game Mode controls performance by changing target FPS based on device GameMode settings.", null));
        static readonly GUIContent s_EnableBoostOnStartupLabel = EditorGUIUtility.TrTextContent(L10n.Tr("Boost mode on startup", null), L10n.Tr("Enables the CPU and GPU boost mode before engine startup to decrease startup time.", null));
        static readonly GUIContent s_StatsLoggingFrequencyInFramesLabel = EditorGUIUtility.TrTextContent(L10n.Tr("Logging Frequency", null), L10n.Tr("Changes the logging frequency.", null));
        static readonly GUIContent s_IndexerActiveLabel = EditorGUIUtility.TrTextContent(L10n.Tr("Active", null), L10n.Tr("Is indexer enabled.", null));
        static readonly GUIContent s_IndexerThermalActionDelayLabel = EditorGUIUtility.TrTextContent(L10n.Tr("Thermal Action Delay", null), L10n.Tr("Delay after any scaler is applied or unapplied because of thermal state.", null));
        static readonly GUIContent s_IndexerPerformanceActionDelayLabel = EditorGUIUtility.TrTextContent(L10n.Tr("Performance Action Delay", null), L10n.Tr("Delay after any scaler is applied or unapplied because of performance state.", null));
        static readonly string[] k_ModeOptions = new string[] {AdaptivePerformanceNormalModeProvider.kModeName, AdaptivePerformanceBatteryModeProvider.kModeName};
        static readonly GUIContent s_ModeLabel = EditorGUIUtility.TrTextContent(L10n.Tr("Operation Mode", null));
        static readonly GUIContent s_ScalerScale = EditorGUIUtility.TrTextContent(L10n.Tr("Scale", null), L10n.Tr("Scale to control the quality impact for the scaler. No quality change when 1, improved quality when >1, and lowered quality when <1", null));
        static readonly GUIContent s_ScalerVisualImpact = EditorGUIUtility.TrTextContent(L10n.Tr("Visual Impact", null), L10n.Tr("Visual impact the scaler has on the application. The higher the more impact the scaler has on the visuals.", null));
        static readonly GUIContent s_ScalerTarget = EditorGUIUtility.TrTextContent(L10n.Tr("Target", null), L10n.Tr("Target for the scaler of the application bottleneck. The target selected has the most impact on the quality control of this scaler. Can only be overriden via API.", null));
        static readonly GUIContent s_ScalerMaxLevel = EditorGUIUtility.TrTextContent(L10n.Tr("Max Level", null), L10n.Tr("Maximum level for the scaler. This is tied to the implementation of the scaler to divide the levels into concrete steps.", null));
        static readonly GUIContent s_ScalerMinBound = EditorGUIUtility.TrTextContent(L10n.Tr("Min Scale", null), L10n.Tr("Minimum value for the scale boundary.", null));
        static readonly GUIContent s_ScalerMaxBound = EditorGUIUtility.TrTextContent(L10n.Tr("Max Scale", null), L10n.Tr("Maximum value for the scale boundary.", null));

        static readonly GUIContent s_AdaptiveFramerate = EditorGUIUtility.TrTextContent(L10n.Tr("Framerate", null), L10n.Tr("Adaptive Framerate enables you to automatically control the application's framerate by the defined minimum and maximum framerate. It uses Application.targetFramerate to control the framerate for your application.", null));
        static readonly GUIContent s_AdaptiveResolution = EditorGUIUtility.TrTextContent(L10n.Tr("Resolution", null), L10n.Tr("Adaptive Resolution enables you to automatically control the screen resolution of the application by the defined scale. It uses Dynamic Resolution (Vulkan only) and uses Resolution Scale of the Universal Render Pipeline as fallback if the project uses Universal Render Pipeline.", null));
        static readonly GUIContent s_AdaptiveLOD = EditorGUIUtility.TrTextContent(L10n.Tr("LOD", null), L10n.Tr("Adaptive LOD changes the LOD bias based on the thermal and performance load.", null));
        static readonly GUIContent s_AdaptiveLut = EditorGUIUtility.TrTextContent(L10n.Tr("LUT", null), L10n.Tr("Requires Universal Render Pipeline. Adaptive LUT changes the LUT Bias of the Universal Render Pipeline based on the thermal and performance load.", null));
        static readonly GUIContent s_AdaptiveMSAA = EditorGUIUtility.TrTextContent(L10n.Tr("MSAA", null), L10n.Tr("Requires Universal Render Pipeline. Adaptive MSAA changes the Anti Aliasing Quality Bias of the Universal Render Pipeline based on the thermal and performance load.", null));
        static readonly GUIContent s_AdaptiveShadowCascade = EditorGUIUtility.TrTextContent(L10n.Tr("Shadow Cascade", null), L10n.Tr("Requires Universal Render Pipeline. Adaptive Shadow Cascade changes the Main Light Shadow Cascades Count Bias of the Universal Render Pipeline based on the thermal and performance load.", null));
        static readonly GUIContent s_AdaptiveShadowDistance = EditorGUIUtility.TrTextContent(L10n.Tr("Shadow Distance", null), L10n.Tr("Requires Universal Render Pipeline. Adaptive Shadow Distance changes the Max Shadow Distance Multiplier of the Universal Render Pipeline based on the thermal and performance load.", null));
        static readonly GUIContent s_AdaptiveShadowmapResolution = EditorGUIUtility.TrTextContent(L10n.Tr("Shadowmap Resolution", null), L10n.Tr("Requires Universal Render Pipeline. Adaptive Shadowmap Resolution changes the  Main Light Shadowmap Resolution Multiplier of the Universal Render Pipeline based on the thermal and performance load.", null));
        static readonly GUIContent s_AdaptiveShadowQuality = EditorGUIUtility.TrTextContent(L10n.Tr("Shadow Quality", null), L10n.Tr("Requires Universal Render Pipeline. Adaptive Shadow Quality changes the Shadow Quality Bias of the Universal Render Pipeline based on the thermal and performance load.", null));
        static readonly GUIContent s_AdaptiveSorting = EditorGUIUtility.TrTextContent(L10n.Tr("Sorting", null), L10n.Tr("Requires Universal Render Pipeline. Adaptive Sorting skips the front-to-back sorting of the Universal Render Pipeline based on the thermal and performance load.", null));
        static readonly GUIContent s_AdaptiveTransparency = EditorGUIUtility.TrTextContent(L10n.Tr("Transparency", null), L10n.Tr("Requires Universal Render Pipeline. Adaptive Transparency skips transparent objects render pass.", null));
        static readonly GUIContent s_AdaptiveViewDistance = EditorGUIUtility.TrTextContent(L10n.Tr("View Distance", null), L10n.Tr("Adaptive View Distance changes the view distance of the main camera. Requires the MainCamera tag on the Camera you want to assign.", null));
        static readonly GUIContent s_AdaptivePhysics = EditorGUIUtility.TrTextContent(L10n.Tr("Physics", null), L10n.Tr("Adaptive Physics changes the Time.fixedDeltaTime based on the thermal and performance load.", null));
        static readonly GUIContent s_AdaptiveDecals = EditorGUIUtility.TrTextContent(L10n.Tr("Decals", null), L10n.Tr("Adaptive Decal changes the maximum draw distance for all decals of the Universal Render Pipeline based on the thermal and performance load.", null));
        static readonly GUIContent s_AdaptiveLayerCulling = EditorGUIUtility.TrTextContent(L10n.Tr("Layer Culling", null), L10n.Tr("Adaptive Layer Culling changes the maximum draw distance for each layer based on the thermal and performance load. It scales the value provided by camera.layerCullDistances.", null));
        
        static readonly GUIContent s_AdaptiveOnDemandRendering = EditorGUIUtility.TrTextContent(L10n.Tr("On Demand Rendering", null), L10n.Tr("Adaptive On Demand Rendering skips rendering frames while the application is idle in Battery Mode by driving Rendering.OnDemandRendering.renderFrameInterval.", null));

        // Tab strip used in place of the per-scaler Operation Mode dropdown.
        // Visual order is defined by s_OperationModeTabOrder; the labels are derived
        // lazily from the enum names via ObjectNames.NicifyVariableName so they stay
        // in sync if OperationMode gains, loses, or renames members.
        static readonly OperationMode[] s_OperationModeTabOrder = new[]
        {
            OperationMode.NormalMode,
            OperationMode.BatteryMode,
        };
        [NoAutoStaticsCleanup]
        static GUIContent[] s_OperationModeTabs;
        [NoAutoStaticsCleanup]
        static GUIStyle s_TabFirstStyle;
        [NoAutoStaticsCleanup]
        static GUIStyle s_TabLastStyle;
        const int k_TabButtonHeight = 22;
        // Width of the foldout arrow on a custom-scaler row. 
        const int k_FoldoutArrowWidth = 15;
        // Fixed x-offset (from the row's left edge) for the per-scaler Enabled checkbox
        // on the header row. 
        const int k_TickboxPosition = 227;


        static readonly string s_FramerateWarningVSync = L10n.Tr("Adaptive Framerate is only supported without VSync. Set VSync Count to \"Don't Sync\" in Quality settings.", null);
        static readonly string s_FramerateWarningGameMode = L10n.Tr("Adaptive Framerate is only supported when \"Auto Game Mode\" is turned off.", null);
        static readonly string s_WarningPopup = L10n.Tr("Warning", null);
        static readonly string s_WarningPopupMessage = L10n.Tr("Adaptive Performance requires at least one profile to work properly", null);
        static readonly string s_WarningPopupOption = L10n.Tr("Ok", null);
        static readonly string s_AdaptiveFramerateMenu = L10n.Tr("Adaptive Framerate", null);
        static readonly string s_WarningPlaymodePopup = L10n.Tr("Adaptive Performance settings cannot be changed when the Editor is in Play mode.", null);
        static readonly string s_WarningIndexer = L10n.Tr("You have to enable Adaptive Performance Indexer to use Scaler.", null);
        static readonly string s_WarningLegacyPackage = L10n.Tr(" Please consider update the legacy provider settings editor to support build profile UI properly. ", null);

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
        public virtual string UnsupportedInfo { get; set; } = L10n.Tr("Adaptive Performance Provider not available on this platform", null);

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

        static readonly GUIContent k_ShowRuntimeSettings = EditorGUIUtility.TrTextContent(L10n.Tr("Runtime Settings", null));
        static readonly GUIContent k_ShowDevelopmentSettings = EditorGUIUtility.TrTextContent(L10n.Tr("Development Settings", null));
        static readonly GUIContent k_ShowIndexerSettings = EditorGUIUtility.TrTextContent(L10n.Tr("Indexer Settings", null));
        static readonly GUIContent k_ShowScalerSettings = EditorGUIUtility.TrTextContent(L10n.Tr("Scaler Settings", null));
        static readonly GUIContent k_ShowScalerProfiles = EditorGUIUtility.TrTextContent(L10n.Tr("Scaler Profiles", null));

        struct ScalerSettingInformation
        {
            public bool showScalerSettings;
        }

        class ScalerProfileSettingInformation
        {
            public bool showScalerProfileSettings;
            // Which mode's settings the user is currently viewing/editing for this
            // profile. Selected via the per-profile Operation Mode tab strip drawn in
            // DisplayScalerSettings. Editor-only session state — NOT serialized, and
            // independent of the global IndexerOperationMode that drives runtime routing.
            // Explicit default — without this, viewedMode would land on BatteryMode
            // because BatteryMode = 0 in the enum declaration order.
            public OperationMode viewedMode = OperationMode.NormalMode;
            public Dictionary<string, ScalerSettingInformation> scalerSettingsInfos = new Dictionary<string, ScalerSettingInformation>();
        }

        // Number of property rows displayed inside the expanded panel of each scaler:
        // 1. minBound, 2. maxBound, 3. maxLevel, 4. visualImpact, 5. target
        // (The Enabled toggle lives on the header row; the operation-mode tab strip is
        // per-profile, drawn once in DisplayScalerSettings.)
        [NoAutoStaticsCleanup]
        static int k_NumberOfScalerProperties = 5;

        Dictionary<string, ScalerProfileSettingInformation> m_ScalerProfiles = new Dictionary<string, ScalerProfileSettingInformation>();

        bool m_PreviousHierarchyMode;
        bool m_HasNonSerializedChanges;

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
            // Suppress the list's own "RL Background" box so the outer EditorStyles.frameBox
            // (drawn in DisplayScalerSettings) is the single visual border, matching the
            // BeginPlatformGrouping look — tabs cut into the frame's top border on selection.
            newReorderableListDefaultSettings.showDefaultBackground = false;
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
            if (target == null || serializedObject == null || serializedObject.targetObject == null)
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
            if (target == null || serializedObject == null || serializedObject.targetObject == null)
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
            if (target == null || serializedObject == null || serializedObject.targetObject == null)
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
                AssetDatabase.SaveAssetIfDirty(serializedObject.targetObject);
            }

            if (m_HasNonSerializedChanges)
            {
                EditorUtility.SetDirty(serializedObject.targetObject);
                m_HasNonSerializedChanges = false;
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
                EditorGUI.BeginChangeCheck();
                var newOperationMode = (OperationMode)EditorGUILayout.EnumPopup(s_ModeLabel, m_CurrentSettings.IndexerOperationMode);
                if (EditorGUI.EndChangeCheck())
                {
                    m_CurrentSettings.IndexerOperationMode = newOperationMode;
                    MarkNonSerializedChange();
                }
                if (m_CurrentSettings.IndexerOperationMode == OperationMode.NormalMode)
                {
                    m_CurrentSettings.ActiveModeProvider = m_CurrentSettings.NormalModeProvider;
                }
                else if (m_CurrentSettings.IndexerOperationMode == OperationMode.BatteryMode)
                {
                    m_CurrentSettings.ActiveModeProvider = m_CurrentSettings.BatteryModeProvider;
                }

                if (m_CurrentSettings.IndexerOperationMode == OperationMode.BatteryMode)
                {
                    EditorGUI.BeginChangeCheck();
                    // Clamp the minimum here: IntField ignores the field's [Min] attribute, so
                    // without this the user could type a negative value directly.
                    int newIdleTimeThreshold = Mathf.Max(1, EditorGUILayout.IntField("Idle Time Threshold", m_CurrentSettings.BatteryModeProvider.IdleTimeThresholdInSeconds));
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_CurrentSettings.BatteryModeProvider.IdleTimeThresholdInSeconds = newIdleTimeThreshold;
                        MarkNonSerializedChange();
                    }

                    EditorGUI.BeginChangeCheck();
                    float newSavingTarget = EditorGUILayout.Slider("Save Target", m_CurrentSettings.BatteryModeProvider.SavingTarget, 0f, 0.8f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_CurrentSettings.BatteryModeProvider.SavingTarget = newSavingTarget;
                        MarkNonSerializedChange();
                    }
                }

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
                                    MarkNonSerializedChange();
                                }
                            }, i);
                            menu.ShowAsContext();
                        }
                        EditorGUILayout.EndHorizontal();
                        if (m_FoldoutState[i])
                        {
                            m_SelectedProfileIndex = i;

                            // Look up (lazily create) the per-profile editor state. The
                            // same container is read by DrawScalerSetting via the
                            // scalerProfileSettingInfo argument, so the tab selection set
                            // here is what each scaler row in this profile picks up.
                            var profileName = currentSetting.ScalerProfiles[i].Name;
                            if (!m_ScalerProfiles.TryGetValue(profileName, out var profileInfo))
                            {
                                profileInfo = new ScalerProfileSettingInformation();
                                m_ScalerProfiles[profileName] = profileInfo;
                            }

                            // Operation Mode tab strip + scaler list, framed together the
                            // same way BeginPlatformGrouping renders the platform selector
                            // tabs at the top of this inspector. The tabs sit flush against
                            // the top edge of the frame box and span its full width; the
                            // list draws inside the frame below them.
                            if (s_TabFirstStyle == null)
                            {
                                s_TabFirstStyle = "Tab first";
                                s_TabLastStyle = "Tab last";
                            }
                            if (s_OperationModeTabs == null)
                            {
                                s_OperationModeTabs = new GUIContent[s_OperationModeTabOrder.Length];
                                for (int t = 0; t < s_OperationModeTabOrder.Length; t++)
                                    s_OperationModeTabs[t] = EditorGUIUtility.TrTextContent(L10n.Tr(ObjectNames.NicifyVariableName(s_OperationModeTabOrder[t].ToString()), null));
                            }

                            // Matches BeginPlatformGrouping styling: a frameBox wraps the
                            // tabs + list, the tab strip sits flush against the frame's top
                            // edge, and the selected tab's style cuts through the frame's
                            // top border at its position. The ReorderableList has its own
                            // box drawing suppressed (showDefaultBackground = false in
                            // AddNewReorderableList) so the frame is the only border.
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(30);
                            Rect frameRect = EditorGUILayout.BeginVertical(EditorStyles.frameBox);

                            int leftWidth = Mathf.RoundToInt(frameRect.width * 0.5f);
                            Rect firstTabRect = new Rect(frameRect.x, frameRect.y, leftWidth, k_TabButtonHeight);
                            Rect lastTabRect = new Rect(frameRect.x + leftWidth, frameRect.y, frameRect.width - leftWidth, k_TabButtonHeight);

                            int currentTabIdx = Array.IndexOf(s_OperationModeTabOrder, profileInfo.viewedMode);
                            if (currentTabIdx < 0) currentTabIdx = 0;

                            if (GUI.Toggle(firstTabRect, currentTabIdx == 0, s_OperationModeTabs[0], s_TabFirstStyle) && currentTabIdx != 0)
                            {
                                profileInfo.viewedMode = s_OperationModeTabOrder[0];
                                OnViewedModeChanged();
                            }
                            if (GUI.Toggle(lastTabRect, currentTabIdx == 1, s_OperationModeTabs[1], s_TabLastStyle) && currentTabIdx != 1)
                            {
                                profileInfo.viewedMode = s_OperationModeTabOrder[1];
                                OnViewedModeChanged();
                            }

                            // Tabs are drawn at fixed Rects above; reserve matching layout
                            // space so subsequent elements don't overdraw them (same trick
                            // BeginPlatformGrouping uses).
                            GUILayoutUtility.GetRect(10, k_TabButtonHeight);

                            var reorderableListDefaultSettings = m_scalerList[i];
                            reorderableListDefaultSettings.list = m_IndexLists[i];

                            Rect controlRect = EditorGUILayout.GetControlRect(true, reorderableListDefaultSettings.GetHeight());
                            reorderableListDefaultSettings.DoList(controlRect);

                            EditorGUILayout.EndVertical();
                            EditorGUILayout.EndHorizontal();
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
                        MarkNonSerializedChange();
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

            if (index < scalerProfile.DefaultScalerSettings.Count)
            {
                var scalerSetting = scalerProfile.DefaultScalerSettings[index];
                rect = DrawScalerSetting(rect, scalerSetting, m_IndexerActiveProperty.boolValue && !EditorApplication.isPlayingOrWillChangePlaymode, scalerProfileSettingInfo);
            }
            else
            {
                var newIndex = index - scalerProfile.DefaultScalerSettings.Count;
                {
                    // Shift right past the foldout arrow so the field and the arrow don't
                    // overlap. Clicks on the field still go to the field; clicks on the
                    // arrow or any other empty part of the row fall through to the
                    // full-row Foldout in DrawScalerSetting (drawn below) and toggle it.
                    var objectRowRect = new Rect(rect.x + k_FoldoutArrowWidth, rect.y, 200, rect.height);
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

                        if (!isDuplicate && newObject !=  m_FieldObjects[m_SelectedProfileIndex][newIndex])
                        {
                            var copyObject = Instantiate(newObject);
                            m_FieldObjects[m_SelectedProfileIndex][newIndex] = copyObject;
                            m_FieldObjects[m_SelectedProfileIndex][newIndex].hideFlags = HideFlags.HideInHierarchy;
                            m_FieldObjects[m_SelectedProfileIndex][newIndex].Name = newObject.name;
                            scalerProfile.AddedScalers[newIndex] = copyObject;
                            scalerProfile.AddedScalers[newIndex].DefaultSetting.name = newObject.name;
                            scalerProfile.AddedScalers[newIndex].BatteryModeSetting.name = newObject.name;
                            AssetDatabase.AddObjectToAsset(copyObject, serializedObject.targetObject);
                            MarkNonSerializedChange();
                        }
                        else if(isDuplicate)
                        {
                            EditorUtility.DisplayDialog(s_WarningPopup, L10n.Tr("The Adaptive Performance Scaler named " + newObject.name + " already exists. Please rename and try again.", null), s_WarningPopupOption);
                            m_FieldObjects[m_SelectedProfileIndex][newIndex] = null;
                        }
                    }
                }

                if (scalerProfile.AddedScalers[newIndex] != null)
                {
                    // Pass the scaler instance so the row can resolve DefaultSetting /
                    // BatteryModeSetting directly from the tab selection, bypassing
                    // ActiveSetting (which routes by the global IndexerOperationMode,
                    // independent of which tab the inspector is currently showing).
                    rect = DrawScalerSetting(rect, scalerProfile.AddedScalers[newIndex].ActiveSetting, m_IndexerActiveProperty.boolValue && !EditorApplication.isPlayingOrWillChangePlaymode, scalerProfileSettingInfo, true, scalerProfile.AddedScalers[newIndex]);
                }
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
            MarkNonSerializedChange();
        }

        void MarkNonSerializedChange()
        {
            m_HasNonSerializedChanges = true;
        }

        // Resolves the per-mode struct the inspector should read from and write to for
        // the currently-viewed tab. Custom scalers expose their per-mode structs as
        // DefaultSetting / BatteryModeSetting properties; default-scaler wrappers expose
        // them via GetNormalModeSetting() / GetBatteryModeSetting(). Centralized here so
        // no caller has to repeat the "which mode + which kind of scaler" branching, and
        // so the editor never goes through ActiveSetting (which routes by the global
        // IndexerOperationMode and would ignore the viewed tab).
        static AdaptivePerformanceScalerSettingsBase GetActiveModeSetting(
            OperationMode viewedMode,
            AdaptivePerformanceScalerSettingsBase scalerSetting,
            AdaptivePerformanceScaler scaler)
        {
            bool battery = viewedMode == OperationMode.BatteryMode;
            if (scaler != null)
                return battery ? scaler.BatteryModeSetting : scaler.DefaultSetting;
            return battery ? scalerSetting.GetBatteryModeSetting() : scalerSetting.GetNormalModeSetting();
        }

        // Called when the per-profile Operation Mode tab selection changes. Clears
        // IMGUI's recycled text-field editor so any in-flight typed-but-uncommitted
        // edit on the previous tab is dropped. Without this, the FloatField / IntField
        // at the same screen position keeps the same control ID across the tab switch
        // and silently commits the cached string to the new tab's underlying struct on
        // the next frame.
        void OnViewedModeChanged()
        {
            GUIUtility.keyboardControl = 0;
            EditorGUIUtility.editingTextField = false;
            Repaint();
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
            MarkNonSerializedChange();

            // move pointer in the global index for 1 position up.
            list.index = Math.Clamp(removeIndex + defaultScalerCount - 1, 0, list.count - 1);
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
                if (scalerProfileSettingInfo != null)
                {
                    ScalerSettingInformation scalerSettingInfo;
                    scalerProfileSettingInfo.scalerSettingsInfos.TryGetValue(scalerName, out scalerSettingInfo);
                    bool isDisabledFramerate = DisabledAdaptiveFramerateScaler(scalerName);
                    // Default scaler section now opens whenever the user expanded it —
                    // the name foldout is always clickable, regardless of enabled state,
                    // because the per-mode Enabled toggles live INSIDE the expanded panel.
                    bool sectionOpen = scalerSettingInfo.showScalerSettings;

                    if (sectionOpen)
                    {
                        height += k_NumberOfScalerProperties * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

                        // if we have a framerate section that is disabled by VSync being on, we add space for the warning
                        if (isDisabledFramerate)
                        {
                            height += 2 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                        }
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

                    // Use ActiveSetting so we measure the same setting object the row edits
                    // (matches ActiveSetting routing in DrawScalerElementCallback). Read enabled
                    // off addedScaler.Enabled — the same source the toggle in DrawScalerSetting
                    // writes to via the scaler-instance branch — so the height callback stays in
                    // lockstep with the toggle. Without this, toggling the row would flip one
                    // field while the height callback read another, and the expanded properties
                    // would overflow into the next ReorderableList element.
                    var scalerSetting = addedScaler.ActiveSetting;
                    var scalerName = scalerSetting.name;

                    if (scalerProfileSettingInfo != null)
                    {
                        ScalerSettingInformation scalerSettingInfo;
                        scalerProfileSettingInfo.scalerSettingsInfos.TryGetValue(scalerName, out scalerSettingInfo);
                        // Custom scaler section now opens whenever the user expanded it —
                        // the row-header toggle is gone, so the Enabled toggle lives inside
                        // the panel and the panel must be reachable regardless of Enabled.
                        if (scalerSettingInfo.showScalerSettings)
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

        // Default scalers has settings serialized via the scalerSetting
        // Custom scalers are serialized entirely with the settings inside the scaler itself.
        // So the additional scaler param is to handle the custom scaler case.
        Rect DrawScalerSetting(Rect rect, AdaptivePerformanceScalerSettingsBase scalerSetting, bool renderNotDisabled, ScalerProfileSettingInformation scalerProfileSettingInfo, bool isCustomScaler = false, AdaptivePerformanceScaler scaler = null)
        {
            bool hasChanges = false;
            string scalerName = scalerSetting.name;
            var isEnabled = renderNotDisabled && !DisabledAdaptiveFramerateScaler(scalerName);

            GUI.enabled = isEnabled;

            ScalerSettingInformation scalerSettingInfo;
            if (!scalerProfileSettingInfo.scalerSettingsInfos.TryGetValue(scalerName, out scalerSettingInfo))
            {
                scalerSettingInfo = new ScalerSettingInformation()
                {
                    showScalerSettings = false
                };
            }

            // Resolve once — the header-row Enabled toggle and the expanded panel below
            // both read from / write to the per-mode struct chosen by the per-profile
            // Operation Mode tab strip in DisplayScalerSettings.
            var activeModeSetting = GetActiveModeSetting(scalerProfileSettingInfo.viewedMode, scalerSetting, scaler);

            // Header row: Enabled toggle on the right, foldout label/arrow filling the
            // rest of the row. Toggle is drawn FIRST so it gets first crack at the
            // MouseDown in its rect — the foldout (drawn after with full-row rect) then
            // sees the event already consumed in that strip and only handles clicks
            // elsewhere on the row.
            {
                Rect enabledRect = new Rect(rect.x + k_TickboxPosition, rect.y, 16, EditorGUIUtility.singleLineHeight);
                EditorGUI.BeginChangeCheck();
                bool newEnabled = EditorGUI.Toggle(enabledRect, activeModeSetting.enabled);
                if (EditorGUI.EndChangeCheck())
                {
                    activeModeSetting.enabled = newEnabled;
                    hasChanges = true;
                }

                EditorGUI.BeginChangeCheck();
                var style = new  GUIStyle(EditorStyles.foldout);
                style.clipping = TextClipping.Ellipsis;
                var newShowScalerSettings = EditorGUI.Foldout(rect, scalerSettingInfo.showScalerSettings, isCustomScaler? new GUIContent("") : ReturnScalerGUIContent(scalerName), true, style);
                if (EditorGUI.EndChangeCheck())
                {
                    scalerSettingInfo.showScalerSettings = newShowScalerSettings;
                }
            }

            if (scalerSettingInfo.showScalerSettings)
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

                var minBound = activeModeSetting.minBound;
                var maxBound = activeModeSetting.maxBound;

                EditorGUI.BeginChangeCheck();
                float newMinBound = EditorGUI.FloatField(rect, s_ScalerMinBound, minBound);
                if (EditorGUI.EndChangeCheck())
                {
                    minBound = Mathf.Clamp(newMinBound, 0, maxBound);
                    hasChanges = true;
                }
                activeModeSetting.minBound = minBound;

                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.BeginChangeCheck();
                float newMaxBound = EditorGUI.FloatField(rect, s_ScalerMaxBound, maxBound);
                if (EditorGUI.EndChangeCheck())
                {
                    maxBound = Mathf.Clamp(newMaxBound, minBound, 10000);
                    hasChanges = true;
                }
                activeModeSetting.maxBound = maxBound;

                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                var maxLevel = activeModeSetting.maxLevel;
                EditorGUI.BeginChangeCheck();
                int newMaxLevel = EditorGUI.IntField(rect, s_ScalerMaxLevel, maxLevel);
                if (EditorGUI.EndChangeCheck())
                {
                    maxLevel = Mathf.Clamp(newMaxLevel, 1, 100);
                    hasChanges = true;
                }
                activeModeSetting.maxLevel = maxLevel;

                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                ScalerVisualImpact visualImpact = (ScalerVisualImpact)activeModeSetting.visualImpact;
                EditorGUI.BeginChangeCheck();
                ScalerVisualImpact newVisualImpact = (ScalerVisualImpact)EditorGUI.EnumPopup(rect, s_ScalerVisualImpact, visualImpact);
                if (EditorGUI.EndChangeCheck())
                {
                    visualImpact = (ScalerVisualImpact)Mathf.Clamp((int)newVisualImpact, (int)ScalerVisualImpact.Low, (int)ScalerVisualImpact.High);
                    hasChanges = true;
                }
                activeModeSetting.visualImpact = visualImpact;

                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                ScalerTarget staticFlagMask = activeModeSetting.target;
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
            if (hasChanges)
            {
                MarkNonSerializedChange();
            }
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
                case "Adaptive On Demand Rendering":
                    return s_AdaptiveOnDemandRendering;

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
