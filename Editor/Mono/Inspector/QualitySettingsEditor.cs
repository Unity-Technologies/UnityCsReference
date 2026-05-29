// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Modules;
using UnityEditor.UIElements;
using UnityEditor.UIElements.ProjectSettings;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor
{
    [CustomEditor(typeof(QualitySettings))]
    internal class QualitySettingsEditor : ProjectSettingsBaseEditor
    {
        internal class Content
        {
            public static readonly GUIContent kPlatformTooltip = EditorGUIUtility.TrTextContent("", "Allow quality setting on platform");
            public static readonly GUIContent kAddQualityLevel = EditorGUIUtility.TrTextContent("Add Quality Level");

            public static readonly GUIContent kGlobalTextureMipmapLimit = EditorGUIUtility.TrTextContent("Global Mipmap Limit", "The base texture quality level.");

            public static readonly GUIContent kTextureMipmapLimitGroupsHeader = EditorGUIUtility.TrTextContent("Mipmap Limit Groups", "Mipmap Limit Groups are used to control quality on a per-texture basis.");
            public static readonly GUIContent[] kTextureMipmapLimitGroupsOverrideModeItems =
            {
                EditorGUIUtility.TrTextContent("Override Global Mipmap Limit: Full Resolution", "Global Mipmap Limit is ignored, upload at full resolution."),
                EditorGUIUtility.TrTextContent("Override Global Mipmap Limit: Half Resolution", "Global Mipmap Limit is ignored, upload at half resolution."),
                EditorGUIUtility.TrTextContent("Override Global Mipmap Limit: Quarter Resolution", "Global Mipmap Limit is ignored, upload at quarter resolution."),
                EditorGUIUtility.TrTextContent("Override Global Mipmap Limit: Eighth Resolution", "Global Mipmap Limit is ignored, upload at eighth resolution.")
            };
            public static readonly GUIContent[] kTextureMipmapLimitGroupsOffsetModeItems =
            {
                EditorGUIUtility.TrTextContent("Offset Global Mipmap Limit: -3", "Upload 3 mipmap levels extra compared to the Global Mipmap Limit."),
                EditorGUIUtility.TrTextContent("Offset Global Mipmap Limit: -2", "Upload 2 mipmap levels extra compared to the Global Mipmap Limit."),
                EditorGUIUtility.TrTextContent("Offset Global Mipmap Limit: -1", "Upload 1 mipmap level extra compared to the Global Mipmap Limit."),
                EditorGUIUtility.TrTextContent("Use Global Mipmap Limit", "No offset or override occurs, simply use the Global Mipmap Limit. (Default)"),
                EditorGUIUtility.TrTextContent("Offset Global Mipmap Limit: +1", "Upload 1 mipmap level less compared to the Global Mipmap Limit."),
                EditorGUIUtility.TrTextContent("Offset Global Mipmap Limit: +2", "Upload 2 mipmap levels less compared to the Global Mipmap Limit."),
                EditorGUIUtility.TrTextContent("Offset Global Mipmap Limit: +3", "Upload 3 mipmap levels less compared to the Global Mipmap Limit.")
            };
            public static readonly GUIContent kTextureMipmapLimitGroupsOptions = EditorGUIUtility.TrIconContent("_Menu", "Show additional options");
            public static readonly GUIContent kTextureMipmapLimitGroupsOptionsIdentify = EditorGUIUtility.TrTextContent("Identify textures");
            public static readonly GUIContent kTextureMipmapLimitGroupsOptionsDuplicate = EditorGUIUtility.TrTextContent("Duplicate group");
            public static readonly GUIContent kTextureMipmapLimitGroupsOptionsRename = EditorGUIUtility.TrTextContent("Rename group");
            public static readonly GUIContent kTextureMipmapLimitGroupsAddButton = EditorGUIUtility.TrIconContent("Toolbar Plus", "Create a new mipmap limit group. Note that this adds a group to all quality levels, not only the active one!");
            public static readonly GUIContent kTextureMipmapLimitGroupsRemoveButton = EditorGUIUtility.TrIconContent("Toolbar Minus", "Remove mipmap limit group. Note that this removes the group from all quality levels, not only the active one!");

            public static readonly string kTextureMipmapLimitGroupsDialogTitleOnUpdate = L10n.Tr("Mipmap Limit Groups: Update textures?");
            public static readonly string kTextureMipmapLimitGroupsDialogMessageOnRemove = L10n.Tr("Textures in your project may still be using '{0}'.\n\nSelect 'No' to remove the group without modifying its associated textures. Relevant textures stay bound to the group and fall back automatically to the global mipmap limit.\n\nSelect 'Yes' to remove the group and reset the group property of associated textures to 'None'. This triggers a re-import and may take some time. An undo cannot revert the importer changes.");
            public static readonly string kTextureMipmapLimitGroupsDialogMessageOnRename = L10n.Tr("Textures in your project may still be using '{0}'.\n\nSelect 'No' to rename the group without modifying its associated textures. Relevant textures stay bound to the group and fall back automatically to the global mipmap limit.\n\nSelect 'Yes' to rename the group and update the group property of associated textures to '{1}'. This triggers a re-import and may take some time. An undo cannot revert the importer changes.");
            public static readonly string kTextureMipmapLimitGroupsDialogTitleOnFailure = L10n.Tr("Mipmap Limit Groups: Operation failed");
            public static readonly string kTextureMipmapLimitGroupsDialogMessageOnRenameFail = L10n.Tr("The mipmap limit group '{0}' already exists.\n'{1}' was not renamed.");
            public static readonly string kTextureMipmapLimitGroupsDialogMessageOnUpdateAssetsError = L10n.Tr("An error occured while updating texture assets: {0}");
            public static readonly string kTextureMipmapLimitGroupsDialogMessageOnIdentifyFail = L10n.Tr("No textures are linked to the mipmap limit group '{0}'.");

            public static readonly GUIContent kStreamingMipmapsActive = EditorGUIUtility.TrTextContent("Mipmap Streaming", "When enabled, Unity only streams texture mipmap levels relevant to the current Camera's position in a Scene. This reduces the total amount of memory Unity needs for textures. Individual textures must also have 'Stream Mipmap Levels' enabled in their Import Settings.");
            public static readonly GUIContent kStreamingMipmapsMemoryBudget = EditorGUIUtility.TrTextContent("Memory Budget", "The amount of memory (in megabytes) to allocate for all loaded textures.");
            public static readonly GUIContent kStreamingMipmapsRenderersPerFrame = EditorGUIUtility.TrTextContent("Renderers Per Frame", "The number of renderers to process each frame. A lower number decreases the CPU load but delays mipmap loading.");
            public static readonly GUIContent kStreamingMipmapsAddAllCameras = EditorGUIUtility.TrTextContent("Add All Cameras", "When enabled, Unity uses mipmap streaming for every Camera in the Scene. Otherwise, Unity only uses mipmap streaming for Cameras that have an attached Streaming Controller component.");
            public static readonly GUIContent kStreamingMipmapsMaxLevelReduction = EditorGUIUtility.TrTextContent("Max Level Reduction", "The maximum number of mipmap levels a texture can drop.");
            public static readonly GUIContent kStreamingMipmapsMaxFileIORequests = EditorGUIUtility.TrTextContent("Max IO Requests", "The maximum number of texture file requests from the Mipmap Streaming system that can be active at the same time.");

            public static readonly GUIContent kIconTrash = EditorGUIUtility.TrIconContent("TreeEditor.Trash", "Delete Level");
            public static readonly GUIContent kSoftParticlesHint = EditorGUIUtility.TrTextContent("Soft Particles require either the Deferred Shading rendering path or Cameras that render depth textures.");
            public static readonly GUIContent kBillboardsFaceCameraPos = EditorGUIUtility.TrTextContent("Billboards Face Camera Position", "When enabled, terrain billboards face towards the camera position. Otherwise, they face towards the camera plane. This makes billboards look nicer when the camera rotates but it is more resource intensive to process.");
            public static readonly GUIContent kUseLegacyDistribution = EditorGUIUtility.TrTextContent("Use Legacy Details Distribution", "When enabled, terrain details will be scattered using the old scattering algorithm that often resulted in overlapping details. Included for backwards compatibility with terrain authored in Unity 2022.1 and earlier.");
            public static readonly GUIContent kVSyncCountLabel = EditorGUIUtility.TrTextContent("VSync Count", "Specifies how Unity synchronizes rendering with the refresh rate of the display device.");
            public static readonly GUIContent kRealtimeLGiCpuUsageLabel = EditorGUIUtility.TrTextContent("Realtime GI CPU Usage", "How many CPU worker threads to create for Realtime Global Illumination lighting calculations in the Player. Increasing this makes the system react faster to changes in lighting at a cost of using more CPU time. The higher the CPU Usage value, the more worker threads are created for solving Realtime GI.");
            public static readonly GUIContent kLODBiasLabel = EditorGUIUtility.TrTextContent("LOD Group Bias", "The bias Unity uses to determine which model to render when a GameObject’s on-screen size is between two LOD levels. Values between 0 and 1 favor the less detailed model. Values greater than 1 favor the more detailed model.");
            public static readonly GUIContent kMaximumLODLevelLabel = EditorGUIUtility.TrTextContent("Maximum LOD Group Level", "The highest LOD to use in the application.");
            public static readonly GUIContent kMeshLODThresholdLabel = EditorGUIUtility.TrTextContent("Mesh LOD Threshold", "Unity uses this parameter when selecting the Mesh LOD index to render. Increasing this setting makes Unity favor less detailed LODs in the evaluation process.");
            public static readonly GUIContent kEnableLODCrossFadeLabel = EditorGUIUtility.TrTextContent("LOD Cross Fade", "Enables or disables LOD Cross Fade.");
            public static readonly GUIContent kMipStrippingHint = EditorGUIUtility.TrTextContent("Detected platforms with textures that never use their highest resolution mipmap levels. Enable Texture Mipmap Stripping in the Player Settings to reduce the package size of those platforms.");

            public static readonly GUIContent kAsyncUploadTimeSlice = EditorGUIUtility.TrTextContent("Time Slice", "The amount of time (in milliseconds) Unity spends uploading Texture and Mesh data to the GPU per frame.");
            public static readonly GUIContent kAsyncUploadBufferSize = EditorGUIUtility.TrTextContent("Buffer Size", "The size (in megabytes) of the upload buffer Unity uses to stream Texture and Mesh data to GPU.");
            public static readonly GUIContent kAsyncUploadPersistentBuffer = EditorGUIUtility.TrTextContent("Persistent Buffer", "When enabled, the upload buffer persists even when there is nothing left to upload.");
            public static readonly GUIContent kAsyncUploadBufferSizeWarning = EditorGUIUtility.TrTextContent("Unity has detected that you are using an upload buffer size of {0} MB with the '{1}' setting enabled. If you have issues with excessive memory usage, you may need to reduce the upload buffer size or disable the '{1}' setting. Memory fragmentation can occur if you choose the latter option.");

            public static readonly GUIContent kOverrideTerrainPixelError = EditorGUIUtility.TrTextContent("", "Whether to override pixel error in active Terrains.");
            public static readonly GUIContent kOverrideTerrainBasemapDist = EditorGUIUtility.TrTextContent("", "Whether to override base map distance in active Terrains.");
            public static readonly GUIContent kOverrideTerrainDensityScale = EditorGUIUtility.TrTextContent("", "Whether to override detail density scale in active Terrains.");
            public static readonly GUIContent kOverrideTerrainDetailDistance = EditorGUIUtility.TrTextContent("", "Whether to override detail distance in active Terrains.");
            public static readonly GUIContent kOverrideTerrainTreeDistance = EditorGUIUtility.TrTextContent("", "Whether to override tree distance in active Terrains.");
            public static readonly GUIContent kOverrideTerrainBillboardStart = EditorGUIUtility.TrTextContent("", "Whether to override billboard start distance in active Terrains.");
            public static readonly GUIContent kOverrideTerrainFadeLength = EditorGUIUtility.TrTextContent("", "Whether to override billboard fade length in active Terrains.");
            public static readonly GUIContent kOverrideTerrainMaxTrees = EditorGUIUtility.TrTextContent("", "Whether to override max mesh trees in active Terrains.");
            public static readonly GUIContent kTerrainPixelError = EditorGUIUtility.TrTextContent("Pixel Error", "Value set to Terrain pixel error (See Terrain settings)");
            public static readonly GUIContent kTerrainBasemapDistance = EditorGUIUtility.TrTextContent("Base Map Dist.", "Value set to Terrain base map distance (See Terrain settings)");
            public static readonly GUIContent kTerrainDetailDensityScale = EditorGUIUtility.TrTextContent("Detail Density Scale", "Value set to Terrain detail density scale (See Terrain settings)");
            public static readonly GUIContent kTerrainDetailDistance = EditorGUIUtility.TrTextContent("Detail Distance", "Value set to Terrain detail object distance (See Terrain settings)");
            public static readonly GUIContent kTerrainTreeDistance = EditorGUIUtility.TrTextContent("Tree Distance", "Value set to Terrain tree distance (See Terrain settings)");
            public static readonly GUIContent kTerrainBillboardStart = EditorGUIUtility.TrTextContent("Billboard Start", "Value set to Terrain billboard start distance (See Terrain settings)");
            public static readonly GUIContent kTerrainFadeLength = EditorGUIUtility.TrTextContent("Fade Length", "Value set to Terrain billboard fade length (See Terrain settings)");
            public static readonly GUIContent kTerrainMaxTrees = EditorGUIUtility.TrTextContent("Max Mesh Trees", "Value set to Terrain max mesh trees (See Terrain settings)");

            public static readonly GUIContent kRenderPipelineObject = EditorGUIUtility.TrTextContent("Render Pipeline Asset", "Specifies the Render Pipeline Asset to use for this quality level. It overrides the value set in the Graphics Settings Window.");

            public static readonly string buildProfileQualitySettingsOverrideWarning = L10n.Tr("The current active build profile has overridden Quality levels inclusion. To ensure that the correct levels are included in your build, see the Build Profiles...");
            public static readonly string buildProfileQualitySettingsInformationSingular = L10n.Tr("Renaming and deleting Quality levels will impact one build profile. To edit Quality levels included in build profiles, go to Build Profiles...");
            public static readonly string buildProfileQualitySettingsInformationPlural = L10n.Tr("Renaming and deleting Quality levels will impact {0} build profiles. To edit Quality levels included in build profiles, go to Build Profiles...");
        }

        internal class Styles
        {
            public static readonly GUIStyle kToggle = "OL Toggle";
            public static readonly GUIStyle kDefaultToggle = "OL ToggleWhite";

            public static readonly GUIStyle kListEvenBg = "ObjectPickerResultsOdd";
            public static readonly GUIStyle kListOddBg = "ObjectPickerResultsEven";
            public static readonly GUIStyle kDefaultDropdown = "QualitySettingsDefault";

            public static readonly GUIStyle kTextureMipmapLimitGroupsOptionsButton = new GUIStyle(EditorStyles.miniButton) { padding = new RectOffset() };
            public static readonly GUIStyle kTextureMipmapLimitGroupNameLabel = new GUIStyle(EditorStyles.label) { clipping = TextClipping.Ellipsis };
            public static readonly GUIStyle kLevelLabelStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 20 };

            public const int kMinToggleWidth = 15;
            public const int kMaxToggleWidth = 20;
            public const int kHeaderRowHeight = 20;
            public const int kLabelWidth = 80;

            public const int kTextureMipmapLimitGroupsLabelWidthOffset = -6;
            public const int kTextureMipmapLimitGroupsOptionsWidth = 16;
            public const int kTextureMipmapLimitGroupsOffsetTop = 2;
            public const int kTextureMipmapLimitGroupsPaddingRight = 4;

            public static readonly Vector2 kTextureMipmapLimitGroupsOptionsMenuOffset = new Vector2(-134, 19);
        }

        public const int kMinAsyncRingBufferSize = 2;
        public const int kMaxAsyncRingBufferSize = 2047;
        public const int kAsyncRingBufferSizeWarningThreshold = 513;
        public const int kMinAsyncUploadTimeSlice = 1;
        public const int kMaxAsyncUploadTimeSlice = 33;

        private SerializedObject m_QualitySettings;
        private SerializedProperty m_QualitySettingsProperty;
        private SerializedProperty m_PerPlatformDefaultQualityProperty;
        private List<BuildPlatform> m_ValidPlatforms;

        private SerializedProperty m_TextureMipmapLimitGroupNamesProperty;
        private SerializedProperty m_TextureMipmapLimitGroupSettingsProperty; // Always refers to the active quality level
        private ReorderableList m_TextureMipmapLimitGroupsList;
        private bool m_TextureMipmapLimitGroupBeingRenamed = false;
        private int m_TextureMipmapLimitGroupBeingRenamedIndex = -1;
        private bool m_TextureMipmapLimitGroupsTextFieldNeedsFocus = false;
        private bool m_TextureMipmapLimitGroupsRenameShowUpdatePrompt = true;

        private Editor m_PresetEditor;
        private Presets.Preset m_QualitySettingsPreset;

        IAdaptiveVsyncSetting[] m_AdaptiveVsyncSettings;
        bool m_AdaptiveVSyncVisible;

        private SerializedProperty m_CurrentQualityProperty;
        private bool m_IsEditingQualitySettings; // true if editing actual QualitySettings, false if preset

        // UITK fields
        private VisualElement m_CurrentRoot;
        private VisualElement m_QualityTableContainer;
        private ListView m_QualityLevelsListView;
        private IMGUIContainer m_QualityDetailsContainer;
        private VisualElement m_HeaderElement;
        private Label m_LevelsLabel;
        private VisualElement m_QualityLevelHeader;
        private Label m_QualityLevelNameLabel;
        private Label m_QualityLevelCurrentTag;
        private UnityEngine.UIElements.Button m_SetCurrentButton;

        // Cache for platform defaults (platform name -> quality index)
        private Dictionary<string, int> m_CachedPlatformDefaults = new Dictionary<string, int>();

        // Inspected quality level (separate from current active level)
        private int? m_InspectedQualityLevelField;
        private const string kInspectedQualityLevelPrefKey = "QualitySettingsEditor.selectedLevel";

        private int selectedLevel
        {
            get
            {
                if (m_InspectedQualityLevelField == null)
                {
                    var stored = EditorPrefs.GetInt(GetInspectedLevelPrefKey(), GetCurrentTargetQualityLevel());
                    m_InspectedQualityLevelField =
                        Mathf.Clamp(stored, 0, Mathf.Max(0, m_QualitySettingsProperty.arraySize - 1));
                }

                return m_InspectedQualityLevelField.Value;
            }
            set
            {
                if (m_InspectedQualityLevelField != value)
                {
                    m_InspectedQualityLevelField = Mathf.Clamp(value, 0, Mathf.Max(0, m_QualitySettingsProperty.arraySize - 1));
                    EditorPrefs.SetInt(GetInspectedLevelPrefKey(), m_InspectedQualityLevelField.Value);
                }
            }
        }

        private int GetCurrentTargetQualityLevel()
        {
            int value = m_IsEditingQualitySettings ? QualitySettings.GetQualityLevel() : m_CurrentQualityProperty.intValue;
            return Mathf.Clamp(value, 0, Mathf.Max(0, m_QualitySettingsProperty.arraySize - 1));
        }

        private void SetCurrentTargetQualityLevel(int value)
        {
            value = Mathf.Clamp(value, 0, Mathf.Max(0, m_QualitySettingsProperty.arraySize - 1));

            if (GetCurrentTargetQualityLevel() == value)
                return;

            if (m_IsEditingQualitySettings)
            {
                // If editing the actual QualitySettings asset, use the API to trigger events
                // (e.g., activeQualityLevelIndexChanged for toolbar dropdown updates).
                QualitySettings.SetQualityLevel(value);
                m_QualitySettings.Update();
            }
            else
            {
                // Editing a preset - modify via SerializedProperty
                m_CurrentQualityProperty.intValue = value;
                m_QualitySettings.ApplyModifiedProperties();
            }
        }

        public void OnEnable()
        {
            m_QualitySettings = new SerializedObject(target);
            m_QualitySettingsProperty = m_QualitySettings.FindProperty("m_QualitySettings");
            m_PerPlatformDefaultQualityProperty = m_QualitySettings.FindProperty("m_PerPlatformDefaultQuality");
            m_CurrentQualityProperty = m_QualitySettings.FindProperty("m_CurrentQuality");
            m_ValidPlatforms = BuildPlatforms.instance.GetValidPlatforms();

            // Cache whether we're editing the actual QualitySettings or a preset
            m_IsEditingQualitySettings = (m_QualitySettings.targetObject == QualitySettings.GetQualitySettings());

            // Initialize cached platform defaults
            RebuildPlatformDefaultsCache();

            m_TextureMipmapLimitGroupNamesProperty = m_QualitySettings.FindProperty("m_TextureMipmapLimitGroupNames");
            m_TextureMipmapLimitGroupsList = new ReorderableList(m_QualitySettings, m_TextureMipmapLimitGroupNamesProperty, false, true, true, true);
            // The ReorderableList uses the GroupNames property as an indicator for how many groups really do exist.
            m_TextureMipmapLimitGroupsList.drawHeaderCallback = DrawTextureMipmapLimitGroupsHeader;
            m_TextureMipmapLimitGroupsList.drawElementCallback = DrawTextureMipmapLimitGroupsElement;
            m_TextureMipmapLimitGroupsList.drawFooterCallback = DrawTextureMipmapLimitGroupsFooter;
            m_TextureMipmapLimitGroupsList.onAddCallback = AddTextureMipmapLimitGroup;
            m_TextureMipmapLimitGroupsList.onRemoveCallback = RemoveTextureMipmapLimitGroup;

            var validPlatforms = m_ValidPlatforms.ToArray();
            var validPlatformsLength = validPlatforms.Length;
            m_AdaptiveVsyncSettings = new IAdaptiveVsyncSetting[validPlatformsLength];
            for (int i = 0; i < validPlatformsLength; i++)
            {
                string module = ModuleManager.GetTargetStringFromBuildTargetGroup(validPlatforms[i].namedBuildTarget.ToBuildTargetGroup());
                m_AdaptiveVsyncSettings[i] = ModuleManager.GetAdaptiveSettingEditorExtension(module);
            }

            // Update cached properties for the inspected level
            UpdateCachedProperties(selectedLevel);

            // Subscribe to quality level changes to refresh the UI
            QualitySettings.activeQualityLevelChanged += OnActiveQualityLevelChanged;
            Undo.undoRedoEvent += OnUndoRedoPerformed;
        }

        public void OnDisable()
        {
            // Unsubscribe from quality level changes
            QualitySettings.activeQualityLevelChanged -= OnActiveQualityLevelChanged;
            Undo.undoRedoEvent -= OnUndoRedoPerformed;
        }

        private void OnUndoRedoPerformed(in UndoRedoInfo info)
        {
            // Rebuild cache after undo/redo
            RebuildPlatformDefaultsCache();

            RefreshQualityUI();
        }

        private string GetInspectedLevelPrefKey()
        {
            // Create a unique key per project to persist the inspected quality level
            string projectName = Application.productName;
            return $"{kInspectedQualityLevelPrefKey}.{projectName}";
        }

        public void OnDestroy()
        {
            // Unsubscribe from button click event
            if (m_SetCurrentButton != null)
                m_SetCurrentButton.clicked -= OnSetCurrentButtonClicked;

            // Clear cached property references for current quality level
            m_CurrentQualityProperty = null;
            m_CurrentSettings = null;
            m_NameProperty = null;
            m_PixelLightCountProperty = null;
            m_ShadowsProperty = null;
            m_ShadowResolutionProperty = null;
            m_ShadowProjectionProperty = null;
            m_ShadowCascadesProperty = null;
            m_ShadowDistanceProperty = null;
            m_ShadowNearPlaneOffsetProperty = null;
            m_ShadowCascade2SplitProperty = null;
            m_ShadowCascade4SplitProperty = null;
            m_ShadowMaskUsageProperty = null;
            m_SkinWeightsProperty = null;
            m_GlobalTextureMipmapLimitProperty = null;
            m_AnisotropicTexturesProperty = null;
            m_AntiAliasingProperty = null;
            m_SoftParticlesProperty = null;
            m_RealtimeReflectionProbesProperty = null;
            m_BillboardsFaceCameraPositionProperty = null;
            m_UseLegacyDetailsDistributionProperty = null;
            m_TerrainQualityOverridesProperty = null;
            m_TerrainPixelErrorProperty = null;
            m_TerrainDetailDensityScaleProperty = null;
            m_TerrainBasemapDistanceProperty = null;
            m_TerrainDetailDistanceProperty = null;
            m_TerrainTreeDistanceProperty = null;
            m_TerrainBillboardStartProperty = null;
            m_TerrainFadeLengthProperty = null;
            m_TerrainMaxTreesProperty = null;
            m_VSyncCountProperty = null;
            m_RealtimeGICPUUsageProperty = null;
            m_AdaptiveVsyncProperty = null;
            m_LodBiasProperty = null;
            m_MeshLodThresholdProperty = null;
            m_MaximumLODLevelProperty = null;
            m_EnableLODCrossFadeProperty = null;
            m_ParticleRaycastBudgetProperty = null;
            m_AsyncUploadTimeSliceProperty = null;
            m_AsyncUploadBufferSizeProperty = null;
            m_AsyncUploadPersistentBufferProperty = null;
            m_ResolutionScalingFixedDPIFactorProperty = null;
            m_CustomRenderPipelineProperty = null;

            m_StreamingMipmapsActiveProperty = null;
            m_StreamingMipmapsAddAllCamerasProperty = null;
            m_StreamingMipmapsBudgetProperty = null;
            m_StreamingMipmapsRenderersPerFrameProperty = null;
            m_StreamingMipmapsMaxLevelReductionProperty = null;
            m_StreamingMipmapsMaxFileIORequestsProperty = null;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "MainScrollView"
            };

            root.AddToClassList("quality-settings__scroll-view");

            // Load USS
            var uss = EditorGUIUtility.Load("StyleSheets/ProjectSettings/QualitySettings.uss") as StyleSheet;
            if (uss != null)
                root.styleSheets.Add(uss);

            var styleSheet = EditorGUIUtility.Load("StyleSheets/ProjectSettings/ProjectSettingsCommon.uss") as StyleSheet;
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);

            m_CurrentRoot = root;

            // Bind to SerializedObject once when the panel is attached
            m_CurrentRoot.RegisterCallback<AttachToPanelEvent>(evt =>
            {
                m_CurrentRoot.Bind(m_QualitySettings);
            });

            var titleBar = new ProjectSettingsTitleBar("Quality");
            titleBar.Initialize(serializedObject);

            m_CurrentRoot.Add(titleBar);

            // Add current build target label
            var currentBuildTargetLabel = new Label();
            currentBuildTargetLabel.name = "CurrentBuildTargetLabel";
            currentBuildTargetLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            currentBuildTargetLabel.style.marginTop = 10;
            currentBuildTargetLabel.style.marginBottom = 5;
            currentBuildTargetLabel.style.marginLeft = 0;

            // Update the label text with current build target
            string buildTargetText = "Current Build Target: " +
                Modules.ModuleManager.GetTargetStringFromBuildTarget(EditorUserBuildSettings.activeBuildTarget);
            currentBuildTargetLabel.text = buildTargetText;

            m_CurrentRoot.Add(currentBuildTargetLabel);

            // Build quality level table
            BuildQualityLevelTable();

            // Build quality level header (level name + current tag + set current button)
            BuildQualityLevelHeader();

            // Add IMGUIContainer for quality level detail properties
            BuildQualityLevelDetailsIMGUI();

            return root;
        }

        internal void Dispose()
        {
            m_CurrentRoot?.Clear();
            m_CurrentRoot = null;
        }

        private void OnActiveQualityLevelChanged(int previousLevel, int currentLevel)
        {
            if (!m_IsEditingQualitySettings)
                return;

            // Refresh the table to update the "Current" tag visibility when quality level changes externally
            // Note: We don't change the inspected level here - the user's selection is preserved
            m_QualitySettings.Update();
            RefreshQualityUI();
        }

        private struct QualitySetting
        {
            public string m_Name;
            public string m_PropertyPath;
            public List<string> m_ExcludedPlatforms;
        }

        private List<QualitySetting> GetQualitySettings()
        {
            // Pull the quality settings from the runtime.
            var qualitySettings = new List<QualitySetting>();

            foreach (SerializedProperty prop in m_QualitySettingsProperty)
            {
                var qs = new QualitySetting
                {
                    m_Name = prop.FindPropertyRelative("name").stringValue,
                    m_PropertyPath = prop.propertyPath
                };

                var platforms = new List<string>();
                var platformsProp = prop.FindPropertyRelative("excludedTargetPlatforms");
                foreach (SerializedProperty platformProp in platformsProp)
                    platforms.Add(platformProp.stringValue);

                qs.m_ExcludedPlatforms = platforms;
                qualitySettings.Add(qs);
            }
            return qualitySettings;
        }

        private Dictionary<string, int> GetDefaultQualityForPlatforms()
        {
            m_QualitySettings.Update();

            var defaultPlatformQualities = new Dictionary<string, int>();

            foreach (SerializedProperty prop in m_PerPlatformDefaultQualityProperty)
            {
                defaultPlatformQualities.Add(prop.FindPropertyRelative("first").stringValue, prop.FindPropertyRelative("second").intValue);
            }
            return defaultPlatformQualities;
        }

        private void SetDefaultQualityForPlatforms(Dictionary<string, int> platformDefaults)
        {
            if (m_PerPlatformDefaultQualityProperty.arraySize != platformDefaults.Count)
                m_PerPlatformDefaultQualityProperty.arraySize = platformDefaults.Count;

            var count = 0;
            foreach (var def in platformDefaults)
            {
                var element = m_PerPlatformDefaultQualityProperty.GetArrayElementAtIndex(count);
                var firstProperty = element.FindPropertyRelative("first");
                var secondProperty = element.FindPropertyRelative("second");

                if (firstProperty.stringValue != def.Key || secondProperty.intValue != def.Value)
                {
                    firstProperty.stringValue = def.Key;
                    secondProperty.intValue = def.Value;
                }
                count++;
            }
        }

        private void RebuildPlatformDefaultsCache()
        {
            m_QualitySettings.Update();

            m_CachedPlatformDefaults.Clear();

            foreach (SerializedProperty prop in m_PerPlatformDefaultQualityProperty)
            {
                m_CachedPlatformDefaults.Add(prop.FindPropertyRelative("first").stringValue, prop.FindPropertyRelative("second").intValue);
            }
        }

        // UITK Table Building Methods
        private void BuildQualityLevelTable()
        {
            m_QualityTableContainer = new VisualElement();
            m_QualityTableContainer.name = "QualityLevelTable";
            m_QualityTableContainer.AddToClassList("quality-table");

            // Build header
            var header = BuildHeaderRow();
            m_QualityTableContainer.Add(header);

            // Build quality levels ListView
            m_QualityLevelsListView = new ListView
            {
                name = "QualityLevelsListView",
                showBoundCollectionSize = false,
                showFoldoutHeader = false,
                reorderable = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                reorderMode = ListViewReorderMode.Animated,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                selectionType = SelectionType.Single,
                showAddRemoveFooter = true,

                // Set up makeItem callback
                makeItem = MakeQualityLevelItem,

                // Set up bindItem callback
                bindItem = BindQualityLevelItem,

                // Set up unbindItem callback
                unbindItem = UnbindQualityLevelItem
            };

            m_QualityLevelsListView.itemIndexChanged += OnQualityLevelItemIndexChanged;

            // Handle selection changes
            m_QualityLevelsListView.selectionChanged += OnQualityLevelSelectionChanged;

            // Handle add/remove
            m_QualityLevelsListView.onAdd += OnAddQualityLevel;
            m_QualityLevelsListView.onRemove += OnRemoveQualityLevel;

            m_QualityTableContainer.Add(m_QualityLevelsListView);

            // Align levels label when the table is attached to panel
            m_QualityTableContainer.RegisterCallback<GeometryChangedEvent>(evt => AlignLevelsLabelWidth());

            m_QualityLevelsListView.RegisterCallback<AttachToPanelEvent>(evt =>
            {
                m_QualityLevelsListView.BindProperty(m_QualitySettingsProperty);
                UpdateInspectedLevelSelection();
            });

            m_QualityLevelsListView.TrackPropertyValue(m_QualitySettingsProperty, OnQualityLevelSelectionChanged);

            m_CurrentRoot.Add(m_QualityTableContainer);
        }

        private int AdjustIndexAfterReorder(int currentIndex, int oldIndex, int newIndex)
        {
            if (currentIndex == oldIndex)
            {
                // The item itself was moved
                return newIndex;
            }
            else if (oldIndex < newIndex)
            {
                // Moving down: shift index if it's in between
                if (currentIndex > oldIndex && currentIndex <= newIndex)
                {
                    return currentIndex - 1;
                }
            }
            else
            {
                // Moving up: shift index if it's in between
                if (currentIndex >= newIndex && currentIndex < oldIndex)
                {
                    return currentIndex + 1;
                }
            }

            return currentIndex;
        }

        private int AdjustIndexAfterDeletion(int currentIndex, int deletedIndex)
        {
            if (currentIndex == deletedIndex)
            {
                // The current index was deleted, move to previous or 0
                return Mathf.Max(0, deletedIndex - 1);
            }
            else if (currentIndex > deletedIndex)
            {
                // Current index is after the deleted item, shift down by 1
                return currentIndex - 1;
            }

            return currentIndex;
        }

        private void UpdateInspectedLevelSelection()
        {
            m_QualityLevelsListView.SetSelectionWithoutNotify(new List<int> { selectedLevel });
            UpdateCachedProperties(selectedLevel);
            UpdateQualityLevelHeader();
        }

        private void OnQualityLevelItemIndexChanged(int oldIndex, int newIndex)
        {
            // Update cached platform defaults to reflect the reorder
            // When a quality level moves, we need to adjust all default indices
            var platformKeys = new List<string>(m_CachedPlatformDefaults.Keys);
            foreach (var key in platformKeys)
            {
                int value = m_CachedPlatformDefaults[key];
                m_CachedPlatformDefaults[key] = AdjustIndexAfterReorder(value, oldIndex, newIndex);
            }

            // Write back to serialized property
            SetDefaultQualityForPlatforms(m_CachedPlatformDefaults);
            m_QualitySettings.ApplyModifiedProperties();

            // Update the current active quality level if it was moved
            var currentActiveLevel = GetCurrentTargetQualityLevel();
            int newActiveLevel = AdjustIndexAfterReorder(currentActiveLevel, oldIndex, newIndex);

            // Apply the new active level if it changed
            if (newActiveLevel != currentActiveLevel)
            {
                SetCurrentTargetQualityLevel(newActiveLevel);
            }

            // Update the inspected level to follow the reordered item
            selectedLevel = AdjustIndexAfterReorder(selectedLevel, oldIndex, newIndex);

            // Update selection
            UpdateInspectedLevelSelection();

            // Refresh UI to show updated defaults and current tag
            m_QualityLevelsListView?.RefreshItems();
        }

        private void OnQualityLevelSelectionChanged(SerializedProperty property)
        {
            // Rebuild cache when serialized property changes externally (e.g., reset, undo/redo)
            RebuildPlatformDefaultsCache();

            RefreshQualityUI();
            QualitySettings.OnActiveQualityLevelChanged(-1, GetCurrentTargetQualityLevel());
        }

        private VisualElement BuildHeaderRow()
        {
            m_HeaderElement = new VisualElement();
            m_HeaderElement.name = "QualityLevelTableHeader";
            m_HeaderElement.AddToClassList("quality-table__header");

            // "Levels" label
            m_LevelsLabel = new Label("Levels");
            m_LevelsLabel.AddToClassList("quality-table__levels-label");
            m_HeaderElement.Add(m_LevelsLabel);

            // Register callbacks to align levels label width with first checkbox in rows
            m_HeaderElement.RegisterCallback<AttachToPanelEvent>(evt => AlignLevelsLabelWidth());
            m_HeaderElement.RegisterCallback<GeometryChangedEvent>(evt => AlignLevelsLabelWidth());

            var arrowIcon = EditorGUIUtility.isProSkin ? EditorGUIUtility.LoadIcon("d_dropdown") : EditorGUIUtility.LoadIcon("dropdown");
            var currentBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            var currentNamedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            // Platform columns (icon + default button)
            foreach (var platform in m_ValidPlatforms)
            {
                var platformColumn = new VisualElement();
                platformColumn.name = $"PlatformColumn_{platform.name}";
                platformColumn.AddToClassList("quality-table__platform-column");

                // Platform icon
                var icon = platform.compoundSmallIconForQualitySettings;
                var iconContainer = new VisualElement();
                iconContainer.AddToClassList("quality-table__platform-icon");
                iconContainer.tooltip = platform.title.text;
                iconContainer.style.backgroundImage = new StyleBackground(icon);

                // Highlight the current active build target platform
                // Compare using both BuildTarget and NamedBuildTarget for more accurate matching
                bool isCurrentPlatform = (platform.defaultTarget == currentBuildTarget) ||
                                        (platform.namedBuildTarget == currentNamedBuildTarget);
                if (isCurrentPlatform)
                {
                    iconContainer.AddToClassList("quality-table__platform-icon--selected");
                }

                platformColumn.Add(iconContainer);

                var button = new UnityEngine.UIElements.Button();
                button.name = $"DefaultButton_{platform.name}";
                button.AddToClassList("quality-table__default-dropdown");

                // Set the arrow icon as background
                if (arrowIcon != null)
                {
                    button.style.backgroundImage = new StyleBackground(arrowIcon);
                }

                // Capture variables for closure
                string capturedPlatformName = platform.name;
                button.clicked += () =>
                {
                    var menu = new GenericMenu();
                    var currentSettings = GetQualitySettings();
                    var currentDefaults = GetDefaultQualityForPlatforms();
                    int currentDefault = currentDefaults.ContainsKey(capturedPlatformName) ? currentDefaults[capturedPlatformName] : 0;

                    for (int i = 0; i < currentSettings.Count; i++)
                    {
                        int qualityIndex = i;
                        string qualityName = currentSettings[i].m_Name;
                        bool isSelected = (i == currentDefault);

                        menu.AddItem(new GUIContent(qualityName), isSelected, () =>
                        {
                            var defs = GetDefaultQualityForPlatforms();
                            defs[capturedPlatformName] = qualityIndex;
                            SetDefaultQualityForPlatforms(defs);
                            m_QualitySettings.ApplyModifiedProperties();

                            // Rebuild cache after modifying defaults
                            RebuildPlatformDefaultsCache();

                            // Update the visual state of the toggles to show new default (green styling)
                            m_QualityLevelsListView?.RefreshItems();
                        });
                    }

                    menu.ShowAsContext();
                };

                platformColumn.Add(button);
                m_HeaderElement.Add(platformColumn);
            }

            return m_HeaderElement;
        }

        private void AlignLevelsLabelWidth()
        {
            // Use cached references to avoid repeated Q lookups
            if (m_HeaderElement == null || m_LevelsLabel == null)
                return;

            // Find the first row in the ListView to get the first toggle position
            var firstRow = m_QualityLevelsListView?.Q<VisualElement>(null, "quality-table__row");
            if (firstRow != null)
            {
                // Get the first toggle in the row
                var firstToggle = firstRow.Q<Toggle>(null, "quality-table__platform-toggle");
                if (firstToggle != null)
                {
                    // Calculate the x position of the first toggle relative to the header
                    // Subtract half the toggle width to center the platform column over the checkbox
                    float toggleX = firstToggle.worldBound.x - (m_HeaderElement.worldBound.x + firstToggle.worldBound.width * 0.5f);

                    // Set the levels label width to align with where the first toggle center is
                    if (toggleX > 0)
                    {
                        m_LevelsLabel.style.width = toggleX;
                    }
                }
            }
        }

        private VisualElement MakeQualityLevelItem()
        {
            var row = new VisualElement();
            row.name = "QualityLevelRow";
            row.AddToClassList("quality-table__row");

            var nameField = new TextField
            {
                name = "QualityName"
            };
            nameField.AddToClassList("quality-table__quality-name");
            nameField.isDelayed = true; // Only trigger callback on focus lost or Enter, not every keystroke

            nameField.RegisterValueChangedCallback(evt =>
            {
                if (evt.target is TextField field && field.userData is int index)
                {
                    OnQualityNameChanged(index, evt.previousValue, evt.newValue);
                }
            });

            row.Add(nameField);

            // Add "Current" tag label after the name field
            var currentTag = new Label("Current");
            currentTag.name = "CurrentQualityLevelTag";
            currentTag.AddToClassList("quality-level-current-tag");
            currentTag.tooltip = "This is the current active quality level";
            currentTag.style.display = DisplayStyle.None; // Hidden by default, shown in BindQualityLevelItem
            row.Add(currentTag);

            // Add spacer element to maintain alignment when "Current" tag is not visible
            var spacer = new VisualElement();
            spacer.name = "CurrentQualityLevelSpacer";
            spacer.style.width = 56; // 50px (tag width) + 6px (margin) = 56px
            spacer.style.flexShrink = 0;
            spacer.style.display = DisplayStyle.Flex; // Visible by default
            row.Add(spacer);

            for (int i = 0; i < m_ValidPlatforms.Count; i++)
            {
                var toggle = new Toggle();
                toggle.name = $"PlatformToggle_{i}";
                toggle.AddToClassList("quality-table__platform-toggle");
                int platformIndex = i; // Capture for closure
                toggle.RegisterValueChangedCallback(evt =>
                {
                    if (evt.target is Toggle t && t.userData is int qualityIndex)
                    {
                        var platformName = m_ValidPlatforms[platformIndex].name;
                        OnPlatformToggleChanged(qualityIndex, platformName, evt.newValue);
                    }
                });
                row.Add(toggle);
            }

            return row;
        }

        private void BindQualityLevelItem(VisualElement element, int index)
        {
            if (index < 0 || index >= m_QualitySettingsProperty.arraySize)
                return;

            var qualityProperty = m_QualitySettingsProperty.GetArrayElementAtIndex(index);
            var defaults = m_CachedPlatformDefaults;
            var currentLevel = GetCurrentTargetQualityLevel();
            bool isCurrentLevel = (index == currentLevel);

            // Show/hide the "Current" tag based on whether this is the current active level
            // and we're editing the actual QualitySettings (not a preset)
            var currentTag = element.Q<Label>("CurrentQualityLevelTag");
            var spacer = element.Q<VisualElement>("CurrentQualityLevelSpacer");
            var nameField = element.Q<TextField>("QualityName");
            bool showTag = m_IsEditingQualitySettings && isCurrentLevel;

            // Toggle between showing the "Current" tag or the spacer
            if (currentTag != null)
            {
                currentTag.style.display = showTag ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (spacer != null)
            {
                spacer.style.display = showTag ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (nameField != null)
            {
                var nameProperty = qualityProperty.FindPropertyRelative("name");
                nameField.BindProperty(nameProperty);
                nameField.userData = index;
            }

            // Set selection to the inspected level (not necessarily the current level)
            if (index == selectedLevel && m_QualityLevelsListView.selectedIndex != index)
                m_QualityLevelsListView.SetSelectionWithoutNotify(new List<int> { index });

            var platforms = new List<string>();
            var platformsProp = qualityProperty.FindPropertyRelative("excludedTargetPlatforms");
            foreach (SerializedProperty platformProp in platformsProp)
                platforms.Add(platformProp.stringValue);

            // Bind platform toggles
            for (int i = 0; i < m_ValidPlatforms.Count; i++)
            {
                var platform = m_ValidPlatforms[i];
                var toggle = element.Q<Toggle>($"PlatformToggle_{i}");
                if (toggle != null)
                {
                    bool isDefault = defaults.TryGetValue(platform.name, out var defaultIndex) ? defaultIndex == index : index == 0;
                    bool isEnabled = !platforms.Contains(platform.name);

                    toggle.SetValueWithoutNotify(isEnabled);
                    toggle.EnableInClassList("quality-table__platform-toggle--default", isDefault);
                    // Update userData with current index (callback registered once in MakeQualityLevelItem)
                    toggle.userData = index;
                }
            }
        }

        private void UnbindQualityLevelItem(VisualElement element, int index)
        {
            // Clear userData (callbacks are registered once in MakeQualityLevelItem and don't need unregistering)
            var nameField = element.Q<TextField>("QualityName");
            if (nameField != null)
            {
                nameField.userData = null;
                nameField.Unbind();
            }

            for (int i = 0; i < m_ValidPlatforms.Count; i++)
            {
                var toggle = element.Q<Toggle>($"PlatformToggle_{i}");
                if (toggle != null)
                    toggle.userData = null;
            }
        }


        // Event Handlers
        private void OnQualityLevelSelectionChanged(IEnumerable<object> selectedItems)
        {
            if (m_QualityLevelsListView == null)
                return;

            int selectedIndex = m_QualityLevelsListView.selectedIndex;
            if (selectedIndex >= 0)
            {
                // Update the inspected quality level (not the current active level)
                selectedLevel = selectedIndex;

                // Update the cached properties and refresh the UI to show the selected level's properties
                UpdateCachedProperties(selectedIndex);
                RefreshQualityUI();
            }
        }

        private void OnPlatformToggleChanged(int qualityIndex, string platformName, bool enabled)
        {
            // Validate index
            if (qualityIndex < 0 || qualityIndex >= m_QualitySettingsProperty.arraySize)
                return;

            // Get the quality setting at the specified index
            var qualitySettingProp = m_QualitySettingsProperty.GetArrayElementAtIndex(qualityIndex);
            var platformsProp = qualitySettingProp.FindPropertyRelative("excludedTargetPlatforms");

            if (platformsProp == null)
                return;

            if (enabled)
            {
                // Remove platform from excluded list (enabled means NOT excluded)
                RemovePlatformFromArray(platformsProp, platformName);
            }
            else
            {
                // Add platform to excluded list (disabled means excluded)
                AddPlatformToArray(platformsProp, platformName);
            }
        }

        private void AddPlatformToArray(SerializedProperty arrayProp, string platformName)
        {
            // Check if platform already exists
            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                if (arrayProp.GetArrayElementAtIndex(i).stringValue == platformName)
                    return; // Already exists
            }

            // Add new element
            arrayProp.InsertArrayElementAtIndex(arrayProp.arraySize);
            arrayProp.GetArrayElementAtIndex(arrayProp.arraySize - 1).stringValue = platformName;
            m_QualitySettings.ApplyModifiedProperties();
        }

        private void RemovePlatformFromArray(SerializedProperty arrayProp, string platformName)
        {
            // Find and remove the platform
            for (int i = arrayProp.arraySize - 1; i >= 0; i--)
            {
                if (arrayProp.GetArrayElementAtIndex(i).stringValue == platformName)
                {
                    arrayProp.DeleteArrayElementAtIndex(i);
                    break;
                }
            }
            m_QualitySettings.ApplyModifiedProperties();
        }

        private void OnQualityNameChanged(int qualityIndex, string previousName, string newName)
        {
            if (qualityIndex < 0 || qualityIndex >= m_QualitySettingsProperty.arraySize)
                return;

            var qualityProperty = m_QualitySettingsProperty.GetArrayElementAtIndex(qualityIndex);
            var nameProperty = qualityProperty.FindPropertyRelative("name");
            if (nameProperty != null)
            {
                // Handle empty names
                if (string.IsNullOrEmpty(newName))
                    newName = "Level " + qualityIndex;

                // Update the property and track the new name for pending rename
                nameProperty.stringValue = newName;
                m_QualitySettings.ApplyModifiedProperties();

                if (m_IsEditingQualitySettings)
                {
                    BuildProfileModuleUtil.RenameQualityLevelInAllProfiles(previousName, newName);
                    QualitySettings.OnActiveQualityLevelRenamed(previousName, newName);
                }

                // Update the header if the renamed level is currently inspected
                if (qualityIndex == selectedLevel)
                    UpdateQualityLevelHeader();
            }
        }

        private void OnAddQualityLevel(BaseListView listView)
        {
            int index = m_QualitySettingsProperty.arraySize;

            m_QualitySettingsProperty.InsertArrayElementAtIndex(index);

            var qualityProperty = m_QualitySettingsProperty.GetArrayElementAtIndex(index);
            var nameProperty = qualityProperty.FindPropertyRelative("name");
            if (nameProperty != null)
                nameProperty.stringValue = "Level " + (index + 1);

            m_QualitySettings.ApplyModifiedProperties();

            // Update inspected level to point to the new item
            selectedLevel = index + 1;

            // Select the newly added level
            UpdateInspectedLevelSelection();

            // Rebuild cache after adding
            RebuildPlatformDefaultsCache();
        }

        private void OnRemoveQualityLevel(BaseListView listView)
        {
            m_QualitySettings.Update();

            if (m_QualitySettingsProperty.arraySize <= 1)
                return;

            int index = selectedLevel;

            // Store quality level name for profile removal
            var deleteLevelName = m_QualitySettingsProperty.GetArrayElementAtIndex(index).FindPropertyRelative("name")?.stringValue;

            // Adjust platform defaults BEFORE deletion
            var defaults = GetDefaultQualityForPlatforms();
            using (ListPool<string>.Get(out var keys))
            {
                keys.AddRange(defaults.Keys);
                foreach (var key in keys)
                {
                    int value = defaults[key];
                    if (value == index)
                    {
                        // Platform was pointing to deleted level, set to previous level or 0
                        defaults[key] = Mathf.Max(0, index - 1);
                    }
                    else if (value > index)
                    {
                        // Platform was pointing to a level after the deleted one, shift down
                        defaults[key] = value - 1;
                    }
                }
            }
            
            // Remove from array
            m_QualitySettingsProperty.DeleteArrayElementAtIndex(index);
            m_QualitySettings.ApplyModifiedProperties();

            // Set adjusted defaults
            SetDefaultQualityForPlatforms(defaults);
            m_QualitySettings.ApplyModifiedProperties();

            BuildProfileModuleUtil.RemoveQualityLevelFromAllProfiles(deleteLevelName);

            // Rebuild cache after removing
            RebuildPlatformDefaultsCache();

            // Adjust the inspected level after deletion
            selectedLevel = AdjustIndexAfterDeletion(selectedLevel, index);

            // Update selection and cached properties
            UpdateInspectedLevelSelection();

            // Adjust the current active quality level
            if (m_IsEditingQualitySettings)
            {
                var currentActive = GetCurrentTargetQualityLevel();
                int newActiveIndex = AdjustIndexAfterDeletion(currentActive, index);

                // Apply the adjustment if needed
                if (newActiveIndex != currentActive)
                {
                    SetCurrentTargetQualityLevel(newActiveIndex);
                    QualitySettings.OnActiveQualityLevelChanged(index, newActiveIndex);
                }
            }

            // Refresh UI to update the "Current" tag
            RefreshQualityUI();
        }

        private void BuildQualityLevelHeader()
        {
            m_QualityLevelHeader = new VisualElement
            {
                name = "QualityLevelHeader",
                style = {
                    marginTop = 10,
                    marginBottom = 10,
                    alignItems = Align.Center,
                    flexDirection = FlexDirection.Row
                }
            };
            
            // Level name label
            m_QualityLevelNameLabel = new Label
            {
                name = "QualityLevelNameLabel",
                style = {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 18
                }
            };
            m_QualityLevelHeader.Add(m_QualityLevelNameLabel);

            // Flexible space to push button to the right
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            m_QualityLevelHeader.Add(spacer);

            // "Current" tag (same style as in the list)
            m_QualityLevelCurrentTag = new Label("Current")
            {
                name = "QualityLevelHeaderCurrentTag",
                tooltip = "This is the current active quality level",
                style =
                {
                    marginLeft = 10,
                    display = DisplayStyle.None
                }
            };
            m_QualityLevelCurrentTag.AddToClassList("quality-level-current-tag");
            m_QualityLevelHeader.Add(m_QualityLevelCurrentTag);

            // "Set Current Active Quality" button
            m_SetCurrentButton = new UnityEngine.UIElements.Button
            {
                name = "SetCurrentButton",
                text = "Set Current Active Quality",
                style =
                {
                    display = DisplayStyle.None
                }
            };
            m_SetCurrentButton.clicked += OnSetCurrentButtonClicked;
            m_QualityLevelHeader.Add(m_SetCurrentButton);

            m_CurrentRoot.Add(m_QualityLevelHeader);
        }

        private void UpdateQualityLevelHeader()
        {
            if (m_QualityLevelNameLabel == null ||
                m_QualityLevelCurrentTag == null ||
                m_SetCurrentButton == null ||
                selectedLevel < 0)
                return;

            // Get the inspected level name
            if (selectedLevel >= 0 && selectedLevel < m_QualitySettingsProperty.arraySize)
            {
                var qualityProperty = m_QualitySettingsProperty.GetArrayElementAtIndex(selectedLevel);
                var nameProperty = qualityProperty.FindPropertyRelative("name");
                var levelName = nameProperty.stringValue;

                if (string.IsNullOrEmpty(levelName))
                    levelName = "Level " + selectedLevel;

                m_QualityLevelNameLabel.text = levelName;

                // Show/hide "Current" tag and button based on whether we're editing QualitySettings
                // and whether this is the current active level
                if (m_IsEditingQualitySettings)
                {
                    var currentActiveLevel = GetCurrentTargetQualityLevel();
                    bool isCurrentLevel = (selectedLevel == currentActiveLevel);

                    m_QualityLevelCurrentTag.style.display = isCurrentLevel ? DisplayStyle.Flex : DisplayStyle.None;
                    m_SetCurrentButton.style.display = isCurrentLevel ? DisplayStyle.None : DisplayStyle.Flex;
                }
                else
                {
                    // For presets, hide both tag and button
                    m_QualityLevelCurrentTag.style.display = DisplayStyle.None;
                    m_SetCurrentButton.style.display = DisplayStyle.None;
                }
            }
        }

        private void OnSetCurrentButtonClicked()
        {
            SetCurrentTargetQualityLevel(selectedLevel);
            RefreshQualityUI();
            UpdateQualityLevelHeader();
        }

        private void BuildQualityLevelDetailsIMGUI()
        {
            m_QualityDetailsContainer = new IMGUIContainer(DrawQualityLevelDetailsIMGUI)
            {
                name = "QualityLevelDetails"
            };
            m_QualityDetailsContainer.AddToClassList("quality-details__imgui-container");
            m_CurrentRoot.Add(m_QualityDetailsContainer);
        }

        private void DrawQualityLevelDetailsIMGUI()
        {
            m_QualitySettings.Update();

            // Use the inspected quality level (not necessarily the current active level)
            selectedLevel = Mathf.Clamp(selectedLevel, 0, Mathf.Max(0, m_QualitySettingsProperty.arraySize - 1));
            RefreshCacheIfNeeded(selectedLevel);

            ShowAffectedBuildProfileInformation();
            CheckSameRenderPipelineAssetForOverridenQualityLevels();

            if (BuildProfileContext.ActiveProfileHasQualitySettings())
                EditorGUILayout.HelpBox(Content.buildProfileQualitySettingsOverrideWarning, MessageType.Warning, true);

            if (m_AdaptiveVSyncVisible)
                EditorGUILayout.HelpBox("There are settings below that are only applicable to the current Build Target such as Adaptive Vsync. To change the Build Target, go to the Build Settings", MessageType.Info);

            // Render all quality level property fields
            DrawQualityLevelPropertiesIMGUI(selectedLevel);

            // Apply modified properties at the end
            m_QualitySettings.ApplyModifiedProperties();
        }

        private void RefreshQualityUI()
        {
            UpdateQualityLevelHeader();
            m_QualityLevelsListView?.RefreshItems();
            m_QualityDetailsContainer?.MarkDirtyRepaint();
        }

        private void DrawQualityLevelPropertiesIMGUI(int selectedLevel)
        {
            float restoreLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 220;

            // This contains the bulk of property rendering from OnInspectorGUI
            GUILayout.Label(EditorGUIUtility.TempContent("Rendering"), EditorStyles.boldLabel);

            EditorGUI.RenderPipelineAssetField(Content.kRenderPipelineObject, m_QualitySettings, m_CustomRenderPipelineProperty);

            // Show unified render pipeline status message
            ShowRenderPipelineStatusMessage(out bool usingSRP);

            if (!usingSRP)
                EditorGUILayout.PropertyField(m_PixelLightCountProperty);

            // still valid with SRP
            if (!usingSRP)
            {
                EditorGUILayout.PropertyField(m_AntiAliasingProperty);
            }

            if (!SupportedRenderingFeatures.active.overridesRealtimeReflectionProbes)
                EditorGUILayout.PropertyField(m_RealtimeReflectionProbesProperty);
            EditorGUILayout.PropertyField(m_ResolutionScalingFixedDPIFactorProperty);

            if (usingSRP)
                EditorGUILayout.PropertyField(m_RealtimeGICPUUsageProperty, Content.kRealtimeLGiCpuUsageLabel);

            EditorGUILayout.PropertyField(m_VSyncCountProperty, Content.kVSyncCountLabel);

            if (BuildTargetDiscovery.TryGetBuildTarget(EditorUserBuildSettings.activeBuildTarget, out var iBuildTarget))
            {
                if (m_VSyncCountProperty.intValue > 0 && (iBuildTarget.GraphicsPlatformProperties?.IgnoresVSyncCount ?? false))
                    EditorGUILayout.HelpBox(EditorGUIUtility.TrTextContent($"VSync Count '{m_VSyncCountProperty.enumLocalizedDisplayNames[m_VSyncCountProperty.enumValueIndex]}' is ignored on {iBuildTarget.DisplayName}.", EditorGUIUtility.GetHelpIcon(MessageType.Warning)));
            }

            m_AdaptiveVSyncVisible = false;
            var externalUI = false;
            switch (m_VSyncCountProperty.intValue)
            {
                case > 0:
                {
                    using var vertical = new EditorGUILayout.VerticalScope();
                    using var scope = new EditorGUI.IndentLevelScope();
                    using var disabledScope = new EditorGUI.DisabledScope(m_VSyncCountProperty.intValue == 0);

                    var validPlatforms = m_ValidPlatforms.ToArray();
                    for (int i = 0; i < validPlatforms.Length; i++)
                    {
                        if (validPlatforms[i].defaultTarget != EditorUserBuildSettings.activeBuildTarget || m_AdaptiveVsyncSettings[i] == null)
                            continue;
                        m_AdaptiveVsyncSettings[i].AdaptiveVsyncUI(m_CurrentSettings);
                        m_AdaptiveVSyncVisible = true;
                        externalUI = true;
                        break;
                    }
                    if (!externalUI)
                    {
                        var gfxTypes = PlayerSettings.GetGraphicsAPIs(EditorUserBuildSettings.activeBuildTarget);
                        for (int i = 0;i < gfxTypes.Length; i++)
                        {
                            if (m_AdaptiveVsyncProperty == null || gfxTypes[i] != GraphicsDeviceType.Vulkan)
                                continue;
                            EditorGUILayout.PropertyField(m_AdaptiveVsyncProperty);
                            EditorGUILayout.HelpBox("If Adaptive Vsync extension is available at runtime with Vulkan it will use this, else fallback to vsync.", MessageType.Info);
                            m_AdaptiveVSyncVisible = true;
                        }
                    }
                    break;
                }
                case 0:
                    m_AdaptiveVsyncProperty.boolValue = false;
                    break;
            }

            bool shadowMaskSupported = SupportedRenderingFeatures.IsMixedLightingModeSupported(MixedLightingMode.Shadowmask);
            bool showShadowMaskUsage = shadowMaskSupported && !SupportedRenderingFeatures.active.overridesShadowmask;

            GUILayout.Space(10);
            GUILayout.Label(EditorGUIUtility.TempContent("Textures"), EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_GlobalTextureMipmapLimitProperty, Content.kGlobalTextureMipmapLimit);
            if (EditorGUI.EndChangeCheck() && usingSRP)
            {
                RenderPipelineManager.CleanupRenderPipeline();
            }

            EditorGUILayout.Space(3);
            if (QualitySettings.IsTextureResReducedOnAnyPlatform())
                MipStrippingHintGUI();
            m_TextureMipmapLimitGroupsList.DoLayoutList();
            EditorGUILayout.PropertyField(m_AnisotropicTexturesProperty);

            EditorGUILayout.PropertyField(m_StreamingMipmapsActiveProperty, Content.kStreamingMipmapsActive);
            if (m_StreamingMipmapsActiveProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_StreamingMipmapsAddAllCamerasProperty, Content.kStreamingMipmapsAddAllCameras);
                EditorGUILayout.PropertyField(m_StreamingMipmapsBudgetProperty, Content.kStreamingMipmapsMemoryBudget);
                EditorGUILayout.PropertyField(m_StreamingMipmapsRenderersPerFrameProperty, Content.kStreamingMipmapsRenderersPerFrame);
                EditorGUILayout.PropertyField(m_StreamingMipmapsMaxLevelReductionProperty, Content.kStreamingMipmapsMaxLevelReduction);
                EditorGUILayout.PropertyField(m_StreamingMipmapsMaxFileIORequestsProperty, Content.kStreamingMipmapsMaxFileIORequests);
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(10);
            GUILayout.Label(EditorGUIUtility.TempContent("Particles"), EditorStyles.boldLabel);
            if (!usingSRP)
            {
                EditorGUILayout.PropertyField(m_SoftParticlesProperty);
                if (m_SoftParticlesProperty.boolValue)
                    SoftParticlesHintGUI();
            }
            EditorGUILayout.PropertyField(m_ParticleRaycastBudgetProperty);

            GUILayout.Space(10);
            GUILayout.Label(EditorGUIUtility.TempContent("Terrain"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_BillboardsFaceCameraPositionProperty, Content.kBillboardsFaceCameraPos);
            EditorGUILayout.PropertyField(m_UseLegacyDetailsDistributionProperty, Content.kUseLegacyDistribution);

            GUILayout.Space(5);
            GUILayout.Label(EditorGUIUtility.TempContent("Terrain Setting Overrides"), EditorStyles.boldLabel);

            var originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth -= EditorStyles.toggle.CalcSize(GUIContent.none).x + EditorStyles.toggle.margin.left;

            EditorGUILayout.BeginHorizontal();
            bool pixelErrorActive = DrawOverrideToggle(ref m_TerrainQualityOverridesProperty, TerrainQualityOverrides.PixelError, Content.kOverrideTerrainPixelError);
            using (new EditorGUI.DisabledScope(!pixelErrorActive))
                EditorGUILayout.Slider(m_TerrainPixelErrorProperty, 1.0f, 200.0f, Content.kTerrainPixelError);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool basemapActive = DrawOverrideToggle(ref m_TerrainQualityOverridesProperty, TerrainQualityOverrides.BasemapDistance, Content.kOverrideTerrainBasemapDist);
            using (new EditorGUI.DisabledScope(!basemapActive))
            {
                EditorGUI.BeginChangeCheck();
                var newValue = EditorGUILayout.PowerSlider(Content.kTerrainBasemapDistance, Mathf.Clamp(m_TerrainBasemapDistanceProperty.floatValue, 0.0f, 20000.0f), 0.0f, 20000.0f, 2);
                if (EditorGUI.EndChangeCheck())
                    m_TerrainBasemapDistanceProperty.floatValue = newValue;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool detailDensityActive = DrawOverrideToggle(ref m_TerrainQualityOverridesProperty, TerrainQualityOverrides.DetailDensity, Content.kOverrideTerrainDensityScale);
            using (new EditorGUI.DisabledScope(!detailDensityActive))
                EditorGUILayout.Slider(m_TerrainDetailDensityScaleProperty, 0.0f, 1.0f, Content.kTerrainDetailDensityScale);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool detailDistanceActive = DrawOverrideToggle(ref m_TerrainQualityOverridesProperty, TerrainQualityOverrides.DetailDistance, Content.kOverrideTerrainDetailDistance);
            using (new EditorGUI.DisabledScope(!detailDistanceActive))
                EditorGUILayout.Slider(m_TerrainDetailDistanceProperty, 0, 1000, Content.kTerrainDetailDistance);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool treeDistanceActive = DrawOverrideToggle(ref m_TerrainQualityOverridesProperty, TerrainQualityOverrides.TreeDistance, Content.kOverrideTerrainTreeDistance);
            using (new EditorGUI.DisabledScope(!treeDistanceActive))
                EditorGUILayout.Slider(m_TerrainTreeDistanceProperty, 0.0f, 5000.0f, Content.kTerrainTreeDistance);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool billboardStartActive = DrawOverrideToggle(ref m_TerrainQualityOverridesProperty, TerrainQualityOverrides.BillboardStart, Content.kOverrideTerrainBillboardStart);
            using (new EditorGUI.DisabledScope(!billboardStartActive))
                EditorGUILayout.Slider(m_TerrainBillboardStartProperty, 5, 2000, Content.kTerrainBillboardStart);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool fadeLengthActive = DrawOverrideToggle(ref m_TerrainQualityOverridesProperty, TerrainQualityOverrides.FadeLength, Content.kOverrideTerrainFadeLength);
            using (new EditorGUI.DisabledScope(!fadeLengthActive))
                EditorGUILayout.Slider(m_TerrainFadeLengthProperty, 0, 200, Content.kTerrainFadeLength);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool maxTreesActive = DrawOverrideToggle(ref m_TerrainQualityOverridesProperty, TerrainQualityOverrides.MaxTrees, Content.kOverrideTerrainMaxTrees);
            using (new EditorGUI.DisabledScope(!maxTreesActive))
                EditorGUILayout.IntSlider(m_TerrainMaxTreesProperty, 0, 10000, Content.kTerrainMaxTrees);
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = originalLabelWidth;

            if (!usingSRP || showShadowMaskUsage)
            {
                GUILayout.Space(10);

                GUILayout.Label(EditorGUIUtility.TempContent("Shadows"), EditorStyles.boldLabel);

                if (showShadowMaskUsage)
                    EditorGUILayout.PropertyField(m_ShadowMaskUsageProperty);

                if (!usingSRP)
                {
                    EditorGUILayout.PropertyField(m_ShadowsProperty);
                    EditorGUILayout.PropertyField(m_ShadowResolutionProperty);
                    EditorGUILayout.PropertyField(m_ShadowProjectionProperty);
                    EditorGUILayout.PropertyField(m_ShadowDistanceProperty);
                    EditorGUILayout.PropertyField(m_ShadowNearPlaneOffsetProperty);
                    EditorGUILayout.PropertyField(m_ShadowCascadesProperty);

                    if (m_ShadowCascadesProperty.intValue == 2)
                        DrawCascadeSplitGUI<float>(ref m_ShadowCascade2SplitProperty);
                    else if (m_ShadowCascadesProperty.intValue == 4)
                        DrawCascadeSplitGUI<Vector3>(ref m_ShadowCascade4SplitProperty);
                }
            }

            GUILayout.Space(10);
            GUILayout.Label(EditorGUIUtility.TempContent("Async Asset Upload"), EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(m_AsyncUploadTimeSliceProperty, Content.kAsyncUploadTimeSlice);
            EditorGUILayout.PropertyField(m_AsyncUploadBufferSizeProperty, Content.kAsyncUploadBufferSize);
            EditorGUILayout.PropertyField(m_AsyncUploadPersistentBufferProperty, Content.kAsyncUploadPersistentBuffer);

            m_AsyncUploadTimeSliceProperty.intValue = Mathf.Clamp(m_AsyncUploadTimeSliceProperty.intValue, kMinAsyncUploadTimeSlice, kMaxAsyncUploadTimeSlice);
            m_AsyncUploadBufferSizeProperty.intValue = Mathf.Clamp(m_AsyncUploadBufferSizeProperty.intValue, kMinAsyncRingBufferSize, kMaxAsyncRingBufferSize);

            if (m_AsyncUploadBufferSizeProperty.intValue >= kAsyncRingBufferSizeWarningThreshold && m_AsyncUploadPersistentBufferProperty.boolValue)
            {
                string messageToDisplay = string.Format(Content.kAsyncUploadBufferSizeWarning.text, m_AsyncUploadBufferSizeProperty.intValue, Content.kAsyncUploadPersistentBuffer.text);
                EditorGUILayout.HelpBox(messageToDisplay, MessageType.Warning, false);
            }

            GUILayout.Space(10);
            GUILayout.Label(EditorGUIUtility.TempContent("Level of Detail"), EditorStyles.boldLabel);

            if (!SupportedRenderingFeatures.active.overridesLODBias)
                EditorGUILayout.PropertyField(m_LodBiasProperty, Content.kLODBiasLabel);
            if (!SupportedRenderingFeatures.active.overridesMaximumLODLevel)
                EditorGUILayout.PropertyField(m_MaximumLODLevelProperty, Content.kMaximumLODLevelLabel);
            EditorGUILayout.PropertyField(m_MeshLodThresholdProperty, Content.kMeshLODThresholdLabel);
            if (!SupportedRenderingFeatures.active.overridesEnableLODCrossFade)
                EditorGUILayout.PropertyField(m_EnableLODCrossFadeProperty, Content.kEnableLODCrossFadeLabel);

            GUILayout.Space(10);
            GUILayout.Label(EditorGUIUtility.TempContent("Meshes"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_SkinWeightsProperty);

            EditorGUIUtility.labelWidth = restoreLabelWidth;
        }

        private static void DrawHorizontalDivider()
        {
            var spacerLine = GUILayoutUtility.GetRect(GUIContent.none,
                GUIStyle.none,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(1));
            var oldBgColor = GUI.backgroundColor;
            if (EditorGUIUtility.isProSkin)
                GUI.backgroundColor = oldBgColor * 0.7058f;
            else
                GUI.backgroundColor = Color.black;

            if (Event.current.type == EventType.Repaint)
                EditorGUIUtility.whiteTextureStyle.Draw(spacerLine, GUIContent.none, false, false, false, false);

            GUI.backgroundColor = oldBgColor;
        }

        void SoftParticlesHintGUI()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            RenderingPath renderPath = mainCamera.actualRenderingPath;
            if (renderPath == RenderingPath.DeferredShading)
                return; // using deferred, all is good

            if ((mainCamera.depthTextureMode & DepthTextureMode.Depth) != 0)
                return; // already produces depth texture, all is good

            EditorGUILayout.HelpBox(Content.kSoftParticlesHint.text, MessageType.Warning, false);
        }

        void MipStrippingHintGUI()
        {
            if (PlayerSettings.mipStripping)
                return;

            EditorGUILayout.HelpBox(Content.kMipStrippingHint.text, MessageType.Info);
        }

        /**
         * Internal function that takes the shadow cascade splits property field, and dispatches a call to render the GUI.
         * It also transfers the result back
         */

        private void DrawCascadeSplitGUI<T>(ref SerializedProperty shadowCascadeSplit)
        {
            float[] cascadePartitionSizes = null;

            System.Type type = typeof(T);
            if (type == typeof(float))
                cascadePartitionSizes = new float[] { shadowCascadeSplit.floatValue };
            else if (type == typeof(Vector3))
            {
                Vector3 splits = shadowCascadeSplit.vector3Value;
                cascadePartitionSizes = new float[]
                {
                    Mathf.Clamp(splits[0], 0.0f, 1.0f),
                    Mathf.Clamp(splits[1] - splits[0], 0.0f, 1.0f),
                    Mathf.Clamp(splits[2] - splits[1], 0.0f, 1.0f)
                };
            }

            if (cascadePartitionSizes != null)
            {
                EditorGUI.BeginChangeCheck();
                ShadowCascadeSplitGUI.HandleCascadeSliderGUI(ref cascadePartitionSizes);
                if (EditorGUI.EndChangeCheck())
                {
                    if (type == typeof(float))
                        shadowCascadeSplit.floatValue = cascadePartitionSizes[0];
                    else
                    {
                        Vector3 updatedValue = new Vector3();
                        updatedValue[0] = cascadePartitionSizes[0];
                        updatedValue[1] = updatedValue[0] + cascadePartitionSizes[1];
                        updatedValue[2] = updatedValue[1] + cascadePartitionSizes[2];
                        shadowCascadeSplit.vector3Value = updatedValue;
                    }
                }
            }
        }

        private bool DrawOverrideToggle(ref SerializedProperty overrideProperty, TerrainQualityOverrides overrideFlag, GUIContent overrideStyle)
        {
            var overrideFlagsPropertyValue = (TerrainQualityOverrides)overrideProperty.enumValueFlag;
            bool overrideActive = overrideFlagsPropertyValue.HasFlag(overrideFlag);

            var overrideRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.toggle, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.ExpandWidth(false));
            overrideActive = GUI.Toggle(overrideRect, overrideActive, overrideStyle);
            overrideProperty.enumValueFlag = overrideActive
                ? (int)(overrideFlagsPropertyValue | overrideFlag)
                : (int)(overrideFlagsPropertyValue & ~overrideFlag);

            return overrideActive;
        }

        static List<string> s_PlatformsWithDifferentRPAssets = new();

        void CheckSameRenderPipelineAssetForOverridenQualityLevels()
        {
            s_PlatformsWithDifferentRPAssets.Clear();

            foreach (var platform in m_ValidPlatforms)
            {
                var buildTarget = BuildPipeline.GetBuildTargetByName(platform.namedBuildTarget.TargetName);
                var buildTargetGroupName = BuildPipeline.GetBuildTargetGroupName(buildTarget);

                if (!QualitySettings.SamePipelineAssetsForPlatform(buildTargetGroupName))
                    s_PlatformsWithDifferentRPAssets.Add(platform.title.ToString());
            }

            if (s_PlatformsWithDifferentRPAssets.Count > 0)
            {
                EditorGUILayout.HelpBox($"The following platforms have assets in its associated Quality levels that belong to different render pipelines: {string.Join(", ", s_PlatformsWithDifferentRPAssets)}", MessageType.Error);
            }
        }

        private static System.Text.StringBuilder s_MessageBuilder = new System.Text.StringBuilder();
        void ShowRenderPipelineStatusMessage(out bool usingSRP)
        {
            s_MessageBuilder.Clear();
            MessageType messageType = MessageType.Info;
            bool showLearnMoreLink = false;
            usingSRP = false;

            // Check the render pipeline configuration
            if (GraphicsSettings.defaultRenderPipeline != null)
            {
                usingSRP = true;
                s_MessageBuilder.Append("A Scriptable Render Pipeline is in use");

                if (m_CustomRenderPipelineProperty.objectReferenceValue == null)
                {
                    s_MessageBuilder.AppendLine(" because a Default Render Pipeline Asset is set in Graphics.");
                    s_MessageBuilder.Append("You can assign a Render Pipeline Asset in this quality level to override the one used in Graphics");
                }

                s_MessageBuilder.Append(". Some settings will not be used and are hidden.");
            }
            else if (m_CustomRenderPipelineProperty.objectReferenceValue != null)
            {
                usingSRP = true;
                s_MessageBuilder.Append("A Scriptable Render Pipeline is in use. But a Default Render Pipeline Asset in Graphics is missing.");
                s_MessageBuilder.Append(" Some settings will not be used and are hidden.");
            }
            else
            {
                // Built-in Render Pipeline (deprecated)
                bool installedSRP = false;
                foreach (var type in TypeCache.GetTypesDerivedFrom<RenderPipelineAsset>())
                {
                    if (!type.IsAbstract)
                    {
                        installedSRP = true;
                        break;
                    }
                }

                if (installedSRP)
                {
                    s_MessageBuilder.Append("If you don't use a render pipeline asset, Unity uses the Built-In Render Pipeline which is deprecated. ");
                    s_MessageBuilder.Append("Migrate your project to the Universal Render Pipeline instead.");
                    messageType = MessageType.Warning;
                }
                else
                {
                    s_MessageBuilder.Append("The Built-In Render Pipeline is deprecated. ");
                    s_MessageBuilder.Append("Migrate your project to the Universal Render Pipeline instead.");
                    messageType = MessageType.Info;
                }

                showLearnMoreLink = true;
            }

            // Show the unified message
            if (s_MessageBuilder.Length > 0)
            {
                if (showLearnMoreLink)
                {
                    // Show with "Learn more" link for Built-in deprecation
                    // Workaround, as there is a bug in the documentation forwarder that causes links containing a / to be broken:
                    string linkHref = $"https://docs.unity3d.com/{Application.unityVersionVer}.{Application.unityVersionMaj}/Documentation/Manual/urp/upgrading-from-birp.html";

                    // Use this once the documentation forwarder has been fixed
                    // Slack thread: https://unity.slack.com/archives/CTD5B2N7J/p1765805896918059
                    // Ticket: https://jira.unity3d.com/browse/WEBDOCS-2894
                    //   linkHref = System.IO.Path.Combine(Help.baseDocumentationUrl, "urp", "upgrading-from-birp");

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        var infoLabel = EditorGUIUtility.TempContent(s_MessageBuilder.ToString(), EditorGUIUtility.GetHelpIcon(messageType));
                        Rect r = GUILayoutUtility.GetRect(infoLabel, EditorStyles.wordWrappedLabel);
                        int oldIndent = EditorGUI.indentLevel;
                        EditorGUI.indentLevel = 0;
                        EditorGUI.LabelField(r, infoLabel, EditorStyles.wordWrappedLabel);
                        EditorGUI.indentLevel = oldIndent;

                        // Right align the Link
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();
                            EditorGUI.indentLevel = 2;
                            if (EditorGUILayout.LinkButton("Learn more..."))
                            {
                                Help.BrowseURL(linkHref);
                            }
                        }
                        EditorGUI.indentLevel = oldIndent;
                        GUILayout.Space(3);
                    }
                }
                else
                {
                    // Standard help box for SRP messages
                    EditorGUILayout.HelpBox(s_MessageBuilder.ToString(), messageType);
                }
            }
        }

        private void ShowAffectedBuildProfileInformation()
        {
            var profilesWithQualityLevelOverrides = BuildProfileQualitySettingsEditor.GetBuildProfilesWithSettingsOverrideCount();

            if (profilesWithQualityLevelOverrides > 0)
            {
                if (profilesWithQualityLevelOverrides == 1)
                    EditorGUILayout.HelpBox(string.Format(Content.buildProfileQualitySettingsInformationSingular), MessageType.Info);
                else
                    EditorGUILayout.HelpBox(string.Format(Content.buildProfileQualitySettingsInformationPlural, profilesWithQualityLevelOverrides), MessageType.Info);
            }
        }

        private int m_CachedSelectedLevel = -1;
        private int m_CachedQualityLevelCount = -1;

        // Cached SerializedProperty references for current quality level
        private SerializedProperty m_CurrentSettings;
        private SerializedProperty m_NameProperty;
        private SerializedProperty m_PixelLightCountProperty;
        private SerializedProperty m_ShadowsProperty;
        private SerializedProperty m_ShadowResolutionProperty;
        private SerializedProperty m_ShadowProjectionProperty;
        private SerializedProperty m_ShadowCascadesProperty;
        private SerializedProperty m_ShadowDistanceProperty;
        private SerializedProperty m_ShadowNearPlaneOffsetProperty;
        private SerializedProperty m_ShadowCascade2SplitProperty;
        private SerializedProperty m_ShadowCascade4SplitProperty;
        private SerializedProperty m_ShadowMaskUsageProperty;
        private SerializedProperty m_SkinWeightsProperty;
        private SerializedProperty m_GlobalTextureMipmapLimitProperty;
        private SerializedProperty m_AnisotropicTexturesProperty;
        private SerializedProperty m_AntiAliasingProperty;
        private SerializedProperty m_SoftParticlesProperty;
        private SerializedProperty m_RealtimeReflectionProbesProperty;
        private SerializedProperty m_BillboardsFaceCameraPositionProperty;
        private SerializedProperty m_UseLegacyDetailsDistributionProperty;
        private SerializedProperty m_TerrainQualityOverridesProperty;
        private SerializedProperty m_TerrainPixelErrorProperty;
        private SerializedProperty m_TerrainDetailDensityScaleProperty;
        private SerializedProperty m_TerrainBasemapDistanceProperty;
        private SerializedProperty m_TerrainDetailDistanceProperty;
        private SerializedProperty m_TerrainTreeDistanceProperty;
        private SerializedProperty m_TerrainBillboardStartProperty;
        private SerializedProperty m_TerrainFadeLengthProperty;
        private SerializedProperty m_TerrainMaxTreesProperty;
        private SerializedProperty m_VSyncCountProperty;
        private SerializedProperty m_RealtimeGICPUUsageProperty;
        private SerializedProperty m_AdaptiveVsyncProperty;
        private SerializedProperty m_LodBiasProperty;
        private SerializedProperty m_MeshLodThresholdProperty;
        private SerializedProperty m_MaximumLODLevelProperty;
        private SerializedProperty m_EnableLODCrossFadeProperty;
        private SerializedProperty m_ParticleRaycastBudgetProperty;
        private SerializedProperty m_AsyncUploadTimeSliceProperty;
        private SerializedProperty m_AsyncUploadBufferSizeProperty;
        private SerializedProperty m_AsyncUploadPersistentBufferProperty;
        private SerializedProperty m_ResolutionScalingFixedDPIFactorProperty;
        private SerializedProperty m_CustomRenderPipelineProperty;

        private SerializedProperty m_StreamingMipmapsActiveProperty;
        private SerializedProperty m_StreamingMipmapsAddAllCamerasProperty;
        private SerializedProperty m_StreamingMipmapsBudgetProperty;
        private SerializedProperty m_StreamingMipmapsRenderersPerFrameProperty;
        private SerializedProperty m_StreamingMipmapsMaxLevelReductionProperty;
        private SerializedProperty m_StreamingMipmapsMaxFileIORequestsProperty;

        private void UpdateCachedProperties(int selectedLevel)
        {
            m_CurrentSettings = m_QualitySettingsProperty.GetArrayElementAtIndex(selectedLevel);

            m_NameProperty = m_CurrentSettings.FindPropertyRelative("name");
            m_PixelLightCountProperty = m_CurrentSettings.FindPropertyRelative("pixelLightCount");
            m_ShadowsProperty = m_CurrentSettings.FindPropertyRelative("shadows");
            m_ShadowResolutionProperty = m_CurrentSettings.FindPropertyRelative("shadowResolution");
            m_ShadowProjectionProperty = m_CurrentSettings.FindPropertyRelative("shadowProjection");
            m_ShadowCascadesProperty = m_CurrentSettings.FindPropertyRelative("shadowCascades");
            m_ShadowDistanceProperty = m_CurrentSettings.FindPropertyRelative("shadowDistance");
            m_ShadowNearPlaneOffsetProperty = m_CurrentSettings.FindPropertyRelative("shadowNearPlaneOffset");
            m_ShadowCascade2SplitProperty = m_CurrentSettings.FindPropertyRelative("shadowCascade2Split");
            m_ShadowCascade4SplitProperty = m_CurrentSettings.FindPropertyRelative("shadowCascade4Split");
            m_ShadowMaskUsageProperty = m_CurrentSettings.FindPropertyRelative("shadowmaskMode");
            m_SkinWeightsProperty = m_CurrentSettings.FindPropertyRelative("skinWeights");
            m_GlobalTextureMipmapLimitProperty = m_CurrentSettings.FindPropertyRelative("globalTextureMipmapLimit");
            m_TextureMipmapLimitGroupSettingsProperty = m_CurrentSettings.FindPropertyRelative("textureMipmapLimitSettings");
            m_AnisotropicTexturesProperty = m_CurrentSettings.FindPropertyRelative("anisotropicTextures");
            m_AntiAliasingProperty = m_CurrentSettings.FindPropertyRelative("antiAliasing");
            m_SoftParticlesProperty = m_CurrentSettings.FindPropertyRelative("softParticles");
            m_RealtimeReflectionProbesProperty = m_CurrentSettings.FindPropertyRelative("realtimeReflectionProbes");
            m_BillboardsFaceCameraPositionProperty = m_CurrentSettings.FindPropertyRelative("billboardsFaceCameraPosition");
            m_UseLegacyDetailsDistributionProperty = m_CurrentSettings.FindPropertyRelative("useLegacyDetailDistribution");
            m_TerrainQualityOverridesProperty = m_CurrentSettings.FindPropertyRelative("terrainQualityOverrides");
            m_TerrainPixelErrorProperty = m_CurrentSettings.FindPropertyRelative("terrainPixelError");
            m_TerrainDetailDensityScaleProperty = m_CurrentSettings.FindPropertyRelative("terrainDetailDensityScale");
            m_TerrainBasemapDistanceProperty = m_CurrentSettings.FindPropertyRelative("terrainBasemapDistance");
            m_TerrainDetailDistanceProperty = m_CurrentSettings.FindPropertyRelative("terrainDetailDistance");
            m_TerrainTreeDistanceProperty = m_CurrentSettings.FindPropertyRelative("terrainTreeDistance");
            m_TerrainBillboardStartProperty = m_CurrentSettings.FindPropertyRelative("terrainBillboardStart");
            m_TerrainFadeLengthProperty = m_CurrentSettings.FindPropertyRelative("terrainFadeLength");
            m_TerrainMaxTreesProperty = m_CurrentSettings.FindPropertyRelative("terrainMaxTrees");
            m_VSyncCountProperty = m_CurrentSettings.FindPropertyRelative("vSyncCount");
            m_RealtimeGICPUUsageProperty = m_CurrentSettings.FindPropertyRelative("realtimeGICPUUsage");
            m_AdaptiveVsyncProperty = m_CurrentSettings.FindPropertyRelative("adaptiveVsync");
            m_LodBiasProperty = m_CurrentSettings.FindPropertyRelative("lodBias");
            m_MeshLodThresholdProperty = m_CurrentSettings.FindPropertyRelative("meshLodThreshold");
            m_MaximumLODLevelProperty = m_CurrentSettings.FindPropertyRelative("maximumLODLevel");
            m_EnableLODCrossFadeProperty = m_CurrentSettings.FindPropertyRelative("enableLODCrossFade");
            m_ParticleRaycastBudgetProperty = m_CurrentSettings.FindPropertyRelative("particleRaycastBudget");
            m_AsyncUploadTimeSliceProperty = m_CurrentSettings.FindPropertyRelative("asyncUploadTimeSlice");
            m_AsyncUploadBufferSizeProperty = m_CurrentSettings.FindPropertyRelative("asyncUploadBufferSize");
            m_AsyncUploadPersistentBufferProperty = m_CurrentSettings.FindPropertyRelative("asyncUploadPersistentBuffer");
            m_ResolutionScalingFixedDPIFactorProperty = m_CurrentSettings.FindPropertyRelative("resolutionScalingFixedDPIFactor");
            m_CustomRenderPipelineProperty = m_CurrentSettings.FindPropertyRelative("customRenderPipeline");

            m_StreamingMipmapsActiveProperty = m_CurrentSettings.FindPropertyRelative("streamingMipmapsActive");
            m_StreamingMipmapsAddAllCamerasProperty = m_CurrentSettings.FindPropertyRelative("streamingMipmapsAddAllCameras");
            m_StreamingMipmapsBudgetProperty = m_CurrentSettings.FindPropertyRelative("streamingMipmapsMemoryBudget");
            m_StreamingMipmapsRenderersPerFrameProperty = m_CurrentSettings.FindPropertyRelative("streamingMipmapsRenderersPerFrame");
            m_StreamingMipmapsMaxLevelReductionProperty = m_CurrentSettings.FindPropertyRelative("streamingMipmapsMaxLevelReduction");
            m_StreamingMipmapsMaxFileIORequestsProperty = m_CurrentSettings.FindPropertyRelative("streamingMipmapsMaxFileIORequests");

            m_CachedSelectedLevel = selectedLevel;
            m_CachedQualityLevelCount = m_QualitySettingsProperty.arraySize;
        }

        void RefreshCacheIfNeeded(int selectedLevel)
        {
            int currentLevelCount = m_QualitySettingsProperty.arraySize;
            int currentPlatformCount = m_ValidPlatforms.Count;
            if (m_CachedSelectedLevel != selectedLevel || m_CachedQualityLevelCount != currentLevelCount || selectedLevel >= currentLevelCount)
            {
                UpdateCachedProperties(selectedLevel);
            }
        }

        void DrawTextureMipmapLimitGroupsHeader(Rect rect)
        {
            EditorGUI.PrefixLabel(rect, Content.kTextureMipmapLimitGroupsHeader);

            Event e = Event.current;
            if (rect.Contains(e.mousePosition) && e.type == EventType.ContextClick)
            {
                GenericMenu menu = EditorGUI.FillPropertyContextMenu(m_TextureMipmapLimitGroupSettingsProperty);

                if (Presets.Preset.IsEditorTargetAPreset(target))
                {
                    // If we are dealing with presets, we will be able to find the "Include Property" and/or "Exclude Property" menu items.
                    // Our texture mipmap limit group names and group settings arrays are always separate properties entirely, which means
                    // that those menu items will not always function as one would expect out-of-the-box, so we apply some custom logic here.
                    GenericMenu.MenuItem includePropItem = menu.menuItems.Find(menu => menu.content.text == L10n.Tr("Include Property"));
                    GenericMenu.MenuItem excludePropItem = menu.menuItems.Find(menu => menu.content.text == L10n.Tr("Exclude Property"));

                    if (m_PresetEditor is null) // Can cache the PresetEditor/preset asset since they won't change.
                    {
                        m_PresetEditor = (includePropItem?.func2?.Target ?? excludePropItem.func2.Target) as Editor;
                        m_QualitySettingsPreset = m_PresetEditor.target as Presets.Preset;
                    }
                    string groupSettingsArrayPropertyPath = (includePropItem?.userData ?? excludePropItem.userData) as string;
                    const string groupNamesArrayPropertyPath = "m_TextureMipmapLimitGroupNames";

                    // Whenever we include the group settings array of any quality level, it could be that the
                    // group names array is not included. (if we used "Exclude all properties", for example)
                    // In that case, forcefully include the group names array. (including only the settings will
                    // not transfer group names over and can replace the settings of unrelated groups!)
                    if (includePropItem is not null)
                    {
                        var includePropertyMethod = includePropItem.func2.Method;

                        includePropItem.func2 = null;
                        includePropItem.func = () =>
                        {
                            includePropertyMethod.Invoke(m_PresetEditor, new object[] { groupSettingsArrayPropertyPath });

                            if (m_QualitySettingsPreset.excludedProperties.Contains(groupNamesArrayPropertyPath))
                            {
                                if (!System.Array.Exists(m_QualitySettingsPreset.excludedProperties, p => p == groupSettingsArrayPropertyPath || groupSettingsArrayPropertyPath.StartsWith(p + ".", System.StringComparison.Ordinal)))
                                {
                                    // If and only if the group names array was excluded in the first place,
                                    // and the group settings array was successfully included, then include
                                    // the group names array too.
                                    includePropertyMethod.Invoke(m_PresetEditor, new object[] { groupNamesArrayPropertyPath });
                                }
                            }
                        };
                    }

                    // Depending on how many group settings arrays are still included after the
                    // current one gets excluded, we take different paths.
                    // 1 or more group settings arrays included -> group names stay included too.
                    // 0 group settings arrays included -> exclude group names.
                    if (excludePropItem is not null)
                    {
                        var excludePropertyMethod = excludePropItem.func2.Method;

                        excludePropItem.func2 = null;
                        excludePropItem.func = () =>
                        {
                            excludePropertyMethod.Invoke(m_PresetEditor, new object[] { groupSettingsArrayPropertyPath });

                            // If the group names array was included (it can be manually excluded!),
                            // then check if we've got all group settings arrays excluded. If that is
                            // the case, exclude the group names array too as described earlier.
                            if (!m_QualitySettingsPreset.excludedProperties.Contains(groupNamesArrayPropertyPath))
                            {
                                bool areAllGroupSettingsExcluded = true;
                                int counter = 0;
                                while (areAllGroupSettingsExcluded && counter < m_QualitySettingsProperty.arraySize)
                                {
                                    string propertyPathToCheck = $"m_QualitySettings.Array.data[{counter}].textureMipmapLimitSettings";
                                    if (!System.Array.Exists(m_QualitySettingsPreset.excludedProperties, p => p == propertyPathToCheck || propertyPathToCheck.StartsWith(p + ".", System.StringComparison.Ordinal)))
                                    {
                                        areAllGroupSettingsExcluded = false;
                                    }
                                    ++counter;
                                }
                                if (areAllGroupSettingsExcluded)
                                {
                                    excludePropertyMethod.Invoke(m_PresetEditor, new object[] { groupNamesArrayPropertyPath });
                                }
                            }
                        };
                    }
                }

                e.Use();
                menu.ShowAsContext();
            }
        }

        void DrawTextureMipmapLimitGroupsElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.y += Styles.kTextureMipmapLimitGroupsOffsetTop; // Elements need to be manually centered due to the usage of Rects
            Rect labelPosition = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth + Styles.kTextureMipmapLimitGroupsLabelWidthOffset, EditorGUI.lineHeight);
            Rect optionsPosition = new Rect(rect.xMax - Styles.kTextureMipmapLimitGroupsOptionsWidth, rect.y, Styles.kTextureMipmapLimitGroupsOptionsWidth, rect.height);
            Rect dropdownPosition = new Rect(labelPosition.xMax + Styles.kTextureMipmapLimitGroupsPaddingRight, rect.y,
                rect.width - labelPosition.width - Styles.kTextureMipmapLimitGroupsPaddingRight - (optionsPosition.width + Styles.kTextureMipmapLimitGroupsPaddingRight), rect.height);

            string groupName = m_TextureMipmapLimitGroupNamesProperty.GetArrayElementAtIndex(index).stringValue;
            SerializedProperty groupSettingsProp = m_TextureMipmapLimitGroupSettingsProperty.GetArrayElementAtIndex(index);
            bool isOffset = groupSettingsProp.FindPropertyRelative("limitBiasMode").intValue == 0;
            int mipmapLimit = groupSettingsProp.FindPropertyRelative("limitBias").intValue;

            // We provide a label with tooltip because the text can be clipped for long group names
            DoTextureMipmapLimitGroupNameLabel(labelPosition, EditorGUIUtility.TempContent(groupName, groupName), index, groupName);
            DoTextureMipmapLimitGroupsSettingsDropdown(dropdownPosition, isOffset, mipmapLimit, index, groupName);
            DoTextureMipmapLimitGroupsOptions(optionsPosition, index, groupName);
        }

        void DrawTextureMipmapLimitGroupsFooter(Rect rect)
        {
            GUIContent toolbarPlus = ReorderableList.defaultBehaviours.iconToolbarPlus;
            GUIContent toolbarMinus = ReorderableList.defaultBehaviours.iconToolbarMinus;

            // Temporarily replace tooltips
            ReorderableList.defaultBehaviours.iconToolbarPlus = Content.kTextureMipmapLimitGroupsAddButton;
            ReorderableList.defaultBehaviours.iconToolbarMinus = Content.kTextureMipmapLimitGroupsRemoveButton;
            ReorderableList.defaultBehaviours.DrawFooter(rect, m_TextureMipmapLimitGroupsList);
            ReorderableList.defaultBehaviours.iconToolbarPlus = toolbarPlus;
            ReorderableList.defaultBehaviours.iconToolbarMinus = toolbarMinus;
        }

        void DoTextureMipmapLimitGroupNameLabel(Rect rect, GUIContent label, int index, string groupName)
        {
            if (m_TextureMipmapLimitGroupBeingRenamed && index == m_TextureMipmapLimitGroupBeingRenamedIndex)
            {
                DoTextureMipmapLimitGroupNameTextField(rect, label);
            }
            else
            {
                GUI.Label(rect, label, Styles.kTextureMipmapLimitGroupNameLabel);
                Event e = Event.current;
                if (rect.Contains(e.mousePosition) && e.type == EventType.ContextClick)
                {
                    ShowTextureMipmapLimitGroupsContextMenu(new Rect(e.mousePosition, Vector2.zero), Vector2.zero, index, groupName);
                }
            }
        }

        void DoTextureMipmapLimitGroupNameTextField(Rect rect, GUIContent label)
        {
            const string controlName = "TextFieldTextureMipmapLimitGroup";

            GUI.SetNextControlName(controlName);
            EditorGUI.DelayedTextField(rect, label.text, EditorStyles.textField);

            Event e = Event.current;
            if (m_TextureMipmapLimitGroupsTextFieldNeedsFocus)
            {
                if (e.type == EventType.Repaint) // Wait until all other events are out of the way
                {
                    EditorGUI.s_DelayedTextEditor.text = label.text; // Should already be the case, but just to make sure.
                    EditorGUI.s_DelayedTextEditor.SelectAll();
                    EditorGUI.FocusTextInControl(controlName);
                    m_TextureMipmapLimitGroupsTextFieldNeedsFocus = false;
                }
            }
            else
            {
                // If clicking out, the rename is NOT cancelled.
                if (e.isMouse && !rect.Contains(e.mousePosition))
                {
                    EndRenamingTextureMipmapLimitGroup(EditorGUI.s_DelayedTextEditor.text);
                }
                // If editing stops or focus is lost, submit the current content of the text editor.
                // Pressing ESC effectively cancels the rename. Pressing Enter, Tab or clicking away
                // submits the user's new group name. (renaming is cancelled if the name didn't change)
                else if (!EditorGUIUtility.editingTextField || GUI.GetNameOfFocusedControl() != controlName)
                {
                    // Cannot open a dialog box until user is done with the text input or clicks away from the QualitySettingsEditor.
                    // Also need to avoid it while view's DrawRect Method is in progress
                    if ((e.type is EventType.KeyUp || !rect.Contains(e.mousePosition)) && e.type is not EventType.Repaint)
                    {
                        EndRenamingTextureMipmapLimitGroup(EditorGUI.s_DelayedTextEditor.text);
                        GUIUtility.ExitGUI(); // Prevents layout errors when clicking on certain other windows. (Hierarchy)
                    }
                    else
                        GUI.InternalRepaintEditorWindow(); // Prevents missing controls when window doesn't repaint on its own.
                }
            }
        }

        void DoTextureMipmapLimitGroupsSettingsDropdown(Rect rect, bool isOffset, int mipmapLimit, int index, string groupName)
        {
            const int limitValueToItemsArrayIndexOffset = 3;
            GUIContent content = (isOffset ? (mipmapLimit > -4 && mipmapLimit < 4) : (mipmapLimit >= 0 && mipmapLimit < 4)) // Is limit within array bounds?
                ? (isOffset ? Content.kTextureMipmapLimitGroupsOffsetModeItems[mipmapLimit + limitValueToItemsArrayIndexOffset] : Content.kTextureMipmapLimitGroupsOverrideModeItems[mipmapLimit])
                : EditorGUIUtility.TrTextContent($"{(isOffset ? "Offset" : "Override")} Global Mipmap Limit: {((mipmapLimit >= 0) ? $"+{mipmapLimit}" : $"{mipmapLimit}")}", "Custom User Setting");

            if (EditorGUI.Button(rect, content, EditorStyles.popup))
            {
                m_TextureMipmapLimitGroupsList.index = index;

                GenericMenu menu = new GenericMenu();
                for (int j = 0; j < Content.kTextureMipmapLimitGroupsOffsetModeItems.Length; ++j)
                {
                    int limitValueFromItemsArrayIndex = j - limitValueToItemsArrayIndexOffset;
                    menu.AddItem(Content.kTextureMipmapLimitGroupsOffsetModeItems[j], isOffset ? mipmapLimit == limitValueFromItemsArrayIndex : false,
                        () => SetTextureMipmapLimitGroupSettings(index, limitValueFromItemsArrayIndex, TextureMipmapLimitBiasMode.OffsetGlobalLimit));
                }
                menu.AddSeparator(string.Empty);
                for (int j = 0; j < Content.kTextureMipmapLimitGroupsOverrideModeItems.Length; ++j)
                {
                    int limit = j;
                    menu.AddItem(Content.kTextureMipmapLimitGroupsOverrideModeItems[j], !isOffset ? mipmapLimit == limit : false,
                        () => SetTextureMipmapLimitGroupSettings(index, limit, TextureMipmapLimitBiasMode.OverrideGlobalLimit));
                }

                menu.DropDown(rect);
            }
        }

        void DoTextureMipmapLimitGroupsOptions(Rect rect, int index, string groupName)
        {
            if (EditorGUI.Button(rect, Content.kTextureMipmapLimitGroupsOptions, Styles.kTextureMipmapLimitGroupsOptionsButton))
            {
                m_TextureMipmapLimitGroupsList.index = index;
                EndRenamingTextureMipmapLimitGroup();

                ShowTextureMipmapLimitGroupsContextMenu(rect, Styles.kTextureMipmapLimitGroupsOptionsMenuOffset, index, groupName);
            }
        }

        void ShowTextureMipmapLimitGroupsContextMenu(Rect rect, Vector2 offset, int index, string groupName)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(Content.kTextureMipmapLimitGroupsOptionsIdentify, false, () => IdentifyAssetsUsingTextureMipmapLimitGroup(groupName));
            menu.AddSeparator(string.Empty);
            menu.AddItem(Content.kTextureMipmapLimitGroupsOptionsDuplicate, false, () => DuplicateTextureMipmapLimitGroup(index));
            menu.AddItem(Content.kTextureMipmapLimitGroupsOptionsRename, false, () => StartRenamingTextureMipmapLimitGroup(index));
            menu.DropDown(new Rect(rect.position + offset, Vector2.zero));
        }

        void AddTextureMipmapLimitGroup(ReorderableList list)
        {
            int newGroupIndex = (++m_TextureMipmapLimitGroupNamesProperty.arraySize) - 1;
            string newGroupName = GetNewTextureMipmapLimitGroupName();
            m_TextureMipmapLimitGroupNamesProperty.GetArrayElementAtIndex(newGroupIndex).stringValue = newGroupName;

            // For all quality levels, we need to add default settings for the new group.
            for (int i = 0; i < m_QualitySettingsProperty.arraySize; ++i)
            {
                SerializedProperty settingsArrProp = m_QualitySettingsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("textureMipmapLimitSettings");
                settingsArrProp.arraySize++;
                SerializedProperty settingsProp = settingsArrProp.GetArrayElementAtIndex(newGroupIndex);
                settingsProp.FindPropertyRelative("limitBias").intValue = 0;
                settingsProp.FindPropertyRelative("limitBiasMode").intValue = 0;
            }

            m_QualitySettings.ApplyModifiedProperties();
            StartRenamingTextureMipmapLimitGroup(newGroupIndex, true);
        }

        void RemoveTextureMipmapLimitGroup(ReorderableList list)
        {
            int indexOfGroupToRemove = list.index;
            string nameOfGroupToRemove = m_TextureMipmapLimitGroupNamesProperty.GetArrayElementAtIndex(indexOfGroupToRemove).stringValue;

            bool isRemoveOperationCancelled = false;

            if (!Presets.Preset.IsEditorTargetAPreset(target))
            {
                int selection = EditorUtility.DisplayDialogComplex(Content.kTextureMipmapLimitGroupsDialogTitleOnUpdate,
                string.Format(Content.kTextureMipmapLimitGroupsDialogMessageOnRemove, GetShortTextureMipmapLimitGroupName(nameOfGroupToRemove)),
                L10n.Tr("No"), L10n.Tr("Cancel"), L10n.Tr("Yes"));

                switch (selection)
                {
                    case 2: // Yes
                        UpdateTextureAssetsLinkedToOldTextureMipmapLimitGroup(nameOfGroupToRemove, string.Empty);
                        break;

                    case 0: // No
                        break;

                    case 1: // Cancel
                        isRemoveOperationCancelled = true;
                        break;
                }
            }

            if (!isRemoveOperationCancelled)
            {
                m_TextureMipmapLimitGroupNamesProperty.DeleteArrayElementAtIndex(indexOfGroupToRemove);

                // For all quality levels, we need to remove the settings of the deleted group.
                for (int i = 0; i < m_QualitySettingsProperty.arraySize; ++i)
                {
                    SerializedProperty settingsArrProp = m_QualitySettingsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("textureMipmapLimitSettings");
                    settingsArrProp.DeleteArrayElementAtIndex(indexOfGroupToRemove);
                }

                m_QualitySettings.ApplyModifiedProperties();
                m_TextureMipmapLimitGroupsList.m_Selection = new List<int>();
                InspectorWindow.RepaintAllInspectors();
            }

            EndRenamingTextureMipmapLimitGroup();
        }

        void DuplicateTextureMipmapLimitGroup(int indexOfGroupToDuplicate)
        {
            int newGroupIndex = (++m_TextureMipmapLimitGroupNamesProperty.arraySize) - 1;
            string newGroupName = GetNewTextureMipmapLimitGroupName();
            m_TextureMipmapLimitGroupNamesProperty.GetArrayElementAtIndex(newGroupIndex).stringValue = newGroupName;

            // For all quality levels, we need to duplicate the settings of the other group.
            for (int i = 0; i < m_QualitySettingsProperty.arraySize; ++i)
            {
                SerializedProperty settingsArrProp = m_QualitySettingsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("textureMipmapLimitSettings");
                settingsArrProp.arraySize++;

                SerializedProperty newSettingsProp = settingsArrProp.GetArrayElementAtIndex(newGroupIndex);
                SerializedProperty settingsPropToDuplicate = settingsArrProp.GetArrayElementAtIndex(indexOfGroupToDuplicate);
                newSettingsProp.FindPropertyRelative("limitBias").intValue = settingsPropToDuplicate.FindPropertyRelative("limitBias").intValue;
                newSettingsProp.FindPropertyRelative("limitBiasMode").intValue = settingsPropToDuplicate.FindPropertyRelative("limitBiasMode").intValue;
            }

            m_QualitySettings.ApplyModifiedProperties();
            StartRenamingTextureMipmapLimitGroup(newGroupIndex, true);
        }

        void UpdateTextureAssetsLinkedToOldTextureMipmapLimitGroup(string oldName, string newName)
        {
            // If we operate on importers while they are selected, the user will still be prompted to confirm changes
            // that they already agreed to. To avoid this: reset the selection, and restore it after we're done.
            UnityEngine.Object[] originalSelection = Selection.objects;

            try
            {
                Selection.objects = null;

                string[] guids = AssetDatabase.FindAssets("t:texture t:preset");
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < guids.Length; ++i)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    Presets.Preset preset = AssetDatabase.LoadMainAssetAtPath(assetPath) as Presets.Preset;
                    bool isPreset = preset is not null;
                    AssetImporter importer = isPreset ? preset.GetReferenceObject() as AssetImporter : AssetImporter.GetAtPath(assetPath);

                    if (importer is TextureImporter)
                    {
                        TextureImporter texImporter = importer as TextureImporter;
                        bool supportsMipmapLimits = texImporter.textureShape == TextureImporterShape.Texture2D || texImporter.textureShape == TextureImporterShape.Texture2DArray;
                        if (supportsMipmapLimits && texImporter.mipmapLimitGroupName == oldName)
                        {
                            texImporter.mipmapLimitGroupName = newName;
                            if (!isPreset)
                                importer.SaveAndReimport();
                            else
                            {
                                preset.UpdateProperties(importer);
                                AssetDatabase.ImportAsset(assetPath);
                            }
                        }
                    }
                    else if (importer is IHVImageFormatImporter)
                    {
                        IHVImageFormatImporter ihvImporter = importer as IHVImageFormatImporter;
                        if (ihvImporter.mipmapLimitGroupName == oldName)
                        {
                            ihvImporter.mipmapLimitGroupName = newName;
                            if (!isPreset)
                                importer.SaveAndReimport();
                            else
                            {
                                preset.UpdateProperties(importer);
                                AssetDatabase.ImportAsset(assetPath);
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog(Content.kTextureMipmapLimitGroupsDialogTitleOnFailure,
                    string.Format(Content.kTextureMipmapLimitGroupsDialogMessageOnUpdateAssetsError, e.Message),
                    L10n.Tr("OK"));
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                Selection.objects = originalSelection;
            }
        }

        // Acts on the current quality level
        void SetTextureMipmapLimitGroupSettings(int groupIndex, int mipmapLimit, TextureMipmapLimitBiasMode mode)
        {
            SerializedProperty settingsToModify = m_TextureMipmapLimitGroupSettingsProperty.GetArrayElementAtIndex(groupIndex);

            settingsToModify.FindPropertyRelative("limitBias").intValue = mipmapLimit;
            settingsToModify.FindPropertyRelative("limitBiasMode").intValue = (int)mode;

            m_QualitySettings.ApplyModifiedProperties();
        }

        void IdentifyAssetsUsingTextureMipmapLimitGroup(string groupNameToIdentify)
        {
            List<UnityEngine.Object> newSelection = new List<UnityEngine.Object>();
            string[] guids = AssetDatabase.FindAssets("t:texture");

            for (int i = 0; i < guids.Length; ++i)
            {
                TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guids[i])) as TextureImporter;
                if (importer is not null && (importer.textureShape == TextureImporterShape.Texture2D || importer.textureShape == TextureImporterShape.Texture2DArray) && importer.mipmapLimitGroupName == groupNameToIdentify)
                {
                    newSelection.Add(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(importer.assetPath));
                }
            }

            if (newSelection.Count > 0)
            {
                Selection.objects = newSelection.ToArray();
            }
            else
            {
                EditorUtility.DisplayDialog(Content.kTextureMipmapLimitGroupsDialogTitleOnFailure,
                    string.Format(Content.kTextureMipmapLimitGroupsDialogMessageOnIdentifyFail, GetShortTextureMipmapLimitGroupName(groupNameToIdentify)),
                    L10n.Tr("OK"));
            }
        }

        void StartRenamingTextureMipmapLimitGroup(int index, bool isNewGroup = false)
        {
            m_TextureMipmapLimitGroupsList.index = index;
            m_TextureMipmapLimitGroupsTextFieldNeedsFocus = true;
            m_TextureMipmapLimitGroupBeingRenamed = true;
            m_TextureMipmapLimitGroupBeingRenamedIndex = index;
            m_TextureMipmapLimitGroupsRenameShowUpdatePrompt = !Presets.Preset.IsEditorTargetAPreset(target) && !isNewGroup;

            if (isNewGroup)
            {
                InspectorWindow.RepaintAllInspectors();
            }
        }

        void EndRenamingTextureMipmapLimitGroup(string newName = "")
        {
            if (!m_TextureMipmapLimitGroupBeingRenamed)
            {
                return;
            }
            m_TextureMipmapLimitGroupBeingRenamed = false;

            if (newName != string.Empty)
            {
                string toRename = m_TextureMipmapLimitGroupNamesProperty.GetArrayElementAtIndex(m_TextureMipmapLimitGroupBeingRenamedIndex).stringValue;
                if (toRename == newName)
                {
                    return;
                }

                string shortNewName = GetShortTextureMipmapLimitGroupName(newName);
                string shortToRename = GetShortTextureMipmapLimitGroupName(toRename);

                for (int i = 0; i < m_TextureMipmapLimitGroupNamesProperty.arraySize; ++i)
                {
                    if (m_TextureMipmapLimitGroupNamesProperty.GetArrayElementAtIndex(i).stringValue == newName)
                    {
                        EditorUtility.DisplayDialog(Content.kTextureMipmapLimitGroupsDialogTitleOnFailure,
                            string.Format(Content.kTextureMipmapLimitGroupsDialogMessageOnRenameFail, shortNewName, shortToRename),
                            L10n.Tr("OK"));
                        return;
                    }
                }

                bool applyModifiedProperties = true;
                if (m_TextureMipmapLimitGroupsRenameShowUpdatePrompt)
                {
                    int selection = EditorUtility.DisplayDialogComplex(Content.kTextureMipmapLimitGroupsDialogTitleOnUpdate,
                        string.Format(Content.kTextureMipmapLimitGroupsDialogMessageOnRename, shortToRename, shortNewName),
                        L10n.Tr("No"), L10n.Tr("Cancel"), L10n.Tr("Yes"));

                    switch (selection)
                    {
                        case 2: // Yes
                            UpdateTextureAssetsLinkedToOldTextureMipmapLimitGroup(toRename, newName);
                            break;

                        case 0: // No
                            break;

                        case 1: // Cancel
                            applyModifiedProperties = false;
                            break;
                    }
                }

                if (applyModifiedProperties)
                {
                    m_TextureMipmapLimitGroupNamesProperty.GetArrayElementAtIndex(m_TextureMipmapLimitGroupBeingRenamedIndex).stringValue = newName;
                    m_QualitySettings.ApplyModifiedProperties();
                    InspectorWindow.RepaintAllInspectors();
                }
            }

            m_TextureMipmapLimitGroupBeingRenamedIndex = -1;
            GUI.FocusControl(string.Empty);
        }

        string GetNewTextureMipmapLimitGroupName()
        {
            string newName = L10n.Tr("New Group");
            string[] existingNames = GetAllKnownTextureMipmapLimitGroupNames();

            int counter = 0;
            while (System.Array.Exists(existingNames, existingName => existingName == newName))
            {
                newName = L10n.Tr("New Group") + string.Format(" ({0})", ++counter);
            }

            return newName;
        }

        string[] GetAllKnownTextureMipmapLimitGroupNames()
        {
            string[] existingNames = new string[m_TextureMipmapLimitGroupNamesProperty.arraySize];
            for (int i = 0; i < m_TextureMipmapLimitGroupNamesProperty.arraySize; ++i)
            {
                existingNames[i] = m_TextureMipmapLimitGroupNamesProperty.GetArrayElementAtIndex(i).stringValue;
            }
            return existingNames;
        }

        // Only meant to limit the length of various messages addressed to the user that concern mipmap limit groups.
        // Don't use elsewhere.
        string GetShortTextureMipmapLimitGroupName(string groupName)
        {
            const int maxGroupNameLength = 41;
            // ^ Cannot fit more than this many characters on 1 line.
            if (groupName.Length > maxGroupNameLength)
            {
                const string suffix = " (...)";
                groupName = groupName.Substring(0, maxGroupNameLength - suffix.Length) + suffix;
            }
            return groupName;
        }

        internal class QualitySettingsProvider : SettingsProvider
        {
            internal static readonly string s_QualitySettingsProviderPath = "Project/Quality";
            internal QualitySettingsEditor inspector;

            [SettingsProvider]
            public static SettingsProvider CreateProjectSettingsProvider()
            {
                var qualitySettingsProvider = new QualitySettingsProvider(s_QualitySettingsProviderPath, SettingsScope.Project)
                {
                    icon = EditorGUIUtility.FindTexture("QualitySettings Icon")
                };
                return qualitySettingsProvider;
            }

            internal QualitySettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
                : base(path, scopes, keywords)
            {
                UpdateKeywords();
                activateHandler = (_, root) =>
                {
                    var qualitySettings = QualitySettings.GetQualitySettings();
                    if (qualitySettings != null)
                    {
                        inspector = Editor.CreateEditor(qualitySettings) as QualitySettingsEditor;
                        var inspectorGUI = inspector.CreateInspectorGUI();
                        if (inspectorGUI != null)
                        {
                            root.Add(inspectorGUI);
                        }
                    }
                };
                deactivateHandler = (() =>
                {
                    if (inspector != null)
                    {
                        UnityEngine.Object.DestroyImmediate(inspector);
                        inspector = null;
                    }
                });
            }

            void UpdateKeywords()
            {
                var keywordsList = new List<string>();
                keywordsList.AddRange(GetSearchKeywordsFromGUIContentProperties<QualitySettingsEditor.Styles>());
                keywordsList.AddRange(GetSearchKeywordsFromGUIContentProperties<QualitySettingsEditor.Content>());

                var qualitySettings = QualitySettings.GetQualitySettings();
                var qualitySettingsSO = new SerializedObject(qualitySettings);
                keywordsList.AddRange(GetSearchKeywordsFromSerializedObject(qualitySettingsSO));

                keywords = keywordsList;
            }
        }
    }
}
