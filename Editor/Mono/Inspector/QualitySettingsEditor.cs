// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    [CustomEditor(typeof(QualitySettings))]
    internal class QualitySettingsEditor : ProjectSettingsBaseEditor
    {
        private class Content
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
                EditorGUIUtility.TrTextContent("Offset Global Mipmap Limit: -3", "Upload 3 mips less compared to the Global Mipmap Limit."),
                EditorGUIUtility.TrTextContent("Offset Global Mipmap Limit: -2", "Upload 2 mips less compared to the Global Mipmap Limit."),
                EditorGUIUtility.TrTextContent("Offset Global Mipmap Limit: -1", "Upload 1 mip less compared to the Global Mipmap Limit."),
                EditorGUIUtility.TrTextContent("Use Global Mipmap Limit", "No offset or override occurs, simply use the Global Mipmap Limit. (Default)"),
                EditorGUIUtility.TrTextContent("Offset Global Mipmap Limit: +1", "Upload 1 mip extra compared to the Global Mipmap Limit."),
                EditorGUIUtility.TrTextContent("Offset Global Mipmap Limit: +2", "Upload 2 mips extra compared to the Global Mipmap Limit."),
                EditorGUIUtility.TrTextContent("Offset Global Mipmap Limit: +3", "Upload 3 mips extra compared to the Global Mipmap Limit.")
            };
            public static readonly GUIContent kTextureMipmapLimitGroupsOptions = EditorGUIUtility.TrIconContent("_Menu", "Show additional options");
            public static readonly GUIContent kTextureMipmapLimitGroupsOptionsIdentify = EditorGUIUtility.TrTextContent("Identify textures");
            public static readonly GUIContent kTextureMipmapLimitGroupsOptionsDuplicate = EditorGUIUtility.TrTextContent("Duplicate group");
            public static readonly GUIContent kTextureMipmapLimitGroupsOptionsRename = EditorGUIUtility.TrTextContent("Rename group");
            public static readonly GUIContent kTextureMipmapLimitGroupsAddButton = EditorGUIUtility.TrIconContent("Toolbar Plus", "Create a new mipmap limit group. Note that this adds a group to all quality levels, not only the active one!");
            public static readonly GUIContent kTextureMipmapLimitGroupsRemoveButton = EditorGUIUtility.TrIconContent("Toolbar Minus", "Remove mipmap limit group. Note that this removes the group from all quality levels, not only the active one!");

            public static readonly string kTextureMipmapLimitGroupsDialogTitle = L10n.Tr("Mipmap Limit Groups");
            public static readonly string kTextureMipmapLimitGroupsDialogMessageOnRemove = L10n.Tr("Textures in your project may still be using '{0}'.\n\nSelect 'No' to remove '{0}' without modifying its associated textures. Relevant textures stay bound to '{0}' and fall back automatically to the global mipmap limit.\n\nSelect 'Yes' to remove '{0}' and reset the group property of associated textures to 'None'. This triggers a re-import and may take some time.\n\nSelect 'Cancel' if you do not wish to remove '{0}' anymore.");
            public static readonly string kTextureMipmapLimitGroupsDialogMessageOnRename = L10n.Tr("Textures in your project may still be using '{0}'.\n\nSelect 'No' to rename '{0}' without modifying its associated textures. Relevant textures stay bound to '{0}' and fall back automatically to the global mipmap limit.\n\nSelect 'Yes' to rename '{0}' and update the group property of associated textures to '{1}'. This triggers a re-import and may take some time.\n\nSelect 'Cancel' if you do not wish to rename '{0}' anymore.");
            public static readonly string kTextureMipmapLimitGroupsDialogMessageOnRenameFail = L10n.Tr("The mipmap limit group '{0}' already exists!\n'{1}' was not renamed.");
            public static readonly string kTextureMipmapLimitGroupsDialogMessageOnUpdateAssetsError = L10n.Tr("An error occured while updating texture assets: {0}");
            public static readonly string kTextureMipmapLimitGroupsDialogMessageOnIdentifyFail = L10n.Tr("No textures are linked to the mipmap limit group '{0}'!");

            public static readonly GUIContent kStreamingMipmapsActive = EditorGUIUtility.TrTextContent("Texture Streaming", "When enabled, Unity only streams texture mipmaps relevant to the current Camera's position in a Scene. This reduces the total amount of memory Unity needs for textures. Individual textures must also have 'Streaming Mip Maps' enabled in their Import Settings.");
            public static readonly GUIContent kStreamingMipmapsMemoryBudget = EditorGUIUtility.TrTextContent("Memory Budget", "The amount of memory (in megabytes) to allocate for all loaded textures.");
            public static readonly GUIContent kStreamingMipmapsRenderersPerFrame = EditorGUIUtility.TrTextContent("Renderers Per Frame", "The number of renderers to process each frame. A lower number decreases the CPU load but delays mipmap loading.");
            public static readonly GUIContent kStreamingMipmapsAddAllCameras = EditorGUIUtility.TrTextContent("Add All Cameras", "When enabled, Unity uses texture streaming for every Camera in the Scene. Otherwise, Unity only uses texture streaming for Cameras that have an attached Streaming Controller component.");
            public static readonly GUIContent kStreamingMipmapsMaxLevelReduction = EditorGUIUtility.TrTextContent("Max Level Reduction", "The maximum number of mipmap levels a texture can drop.");
            public static readonly GUIContent kStreamingMipmapsMaxFileIORequests = EditorGUIUtility.TrTextContent("Max IO Requests", "The maximum number of texture file requests from the Texture Streaming system that can be active at the same time.");

            public static readonly GUIContent kIconTrash = EditorGUIUtility.TrIconContent("TreeEditor.Trash", "Delete Level");
            public static readonly GUIContent kSoftParticlesHint = EditorGUIUtility.TrTextContent("Soft Particles require either the Deferred Shading rendering path or Cameras that render depth textures.");
            public static readonly GUIContent kBillboardsFaceCameraPos = EditorGUIUtility.TrTextContent("Billboards Face Camera Position", "When enabled, terrain billboards face towards the camera position. Otherwise, they face towards the camera plane. This makes billboards look nicer when the camera rotates but it is more resource intensive to process.");
            public static readonly GUIContent kUseLegacyDistribution = EditorGUIUtility.TrTextContent("Use Legacy Details Distribution", "When enabled, terrain details will be scattered using the old scattering algorithm that often resulted in overlapping details. Included for backwards compatibility with terrain authored in Unity 2022.1 and earlier.");
            public static readonly GUIContent kVSyncCountLabel = EditorGUIUtility.TrTextContent("VSync Count", "Specifies how Unity synchronizes rendering with the refresh rate of the display device.");
            public static readonly GUIContent kLODBiasLabel = EditorGUIUtility.TrTextContent("LOD Bias", "The bias Unity uses to determine which model to render when a GameObject’s on-screen size is between two LOD levels. Values between 0 and 1 favor the less detailed model. Values greater than 1 favor the more detailed model.");
            public static readonly GUIContent kMaximumLODLevelLabel = EditorGUIUtility.TrTextContent("Maximum LOD Level", "The highest LOD to use in the application.");
            public static readonly GUIContent kEnableLODCrossFadeLabel = EditorGUIUtility.TrTextContent("LOD Cross Fade", "Enables or disables LOD Cross Fade.");
            public static readonly GUIContent kMipStrippingHint = EditorGUIUtility.TrTextContent("Where the maximum possible texture mip resolution for a platform is less than full, enabling Texture MipMap Stripping in Player Settings can reduce the package size.");

            public static readonly GUIContent kAsyncUploadTimeSlice = EditorGUIUtility.TrTextContent("Time Slice", "The amount of time (in milliseconds) Unity spends uploading Texture and Mesh data to the GPU per frame.");
            public static readonly GUIContent kAsyncUploadBufferSize = EditorGUIUtility.TrTextContent("Buffer Size", "The size (in megabytes) of the upload buffer Unity uses to stream Texture and Mesh data to GPU.");
            public static readonly GUIContent kAsyncUploadPersistentBuffer = EditorGUIUtility.TrTextContent("Persistent Buffer", "When enabled, the upload buffer persists even when there is nothing left to upload.");

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
        }

        private class Styles
        {
            public static readonly GUIStyle kToggle = "OL Toggle";
            public static readonly GUIStyle kDefaultToggle = "OL ToggleWhite";

            public static readonly GUIStyle kListEvenBg = "ObjectPickerResultsOdd";
            public static readonly GUIStyle kListOddBg = "ObjectPickerResultsEven";
            public static readonly GUIStyle kDefaultDropdown = "QualitySettingsDefault";

            public static readonly GUIStyle kTextureMipmapLimitGroupsOptionsButton = new GUIStyle(EditorStyles.miniButton) { padding = new RectOffset() };

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
        public const int kMaxAsyncRingBufferSize = 512;
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

        public void OnEnable()
        {
            m_QualitySettings = new SerializedObject(target);
            m_QualitySettingsProperty = m_QualitySettings.FindProperty("m_QualitySettings");
            m_PerPlatformDefaultQualityProperty = m_QualitySettings.FindProperty("m_PerPlatformDefaultQuality");
            m_ValidPlatforms = BuildPlatforms.instance.GetValidPlatforms();

            m_TextureMipmapLimitGroupNamesProperty = m_QualitySettings.FindProperty("m_TextureMipmapLimitGroupNames");
            m_TextureMipmapLimitGroupsList = new ReorderableList(m_QualitySettings, m_TextureMipmapLimitGroupNamesProperty, false, true, true, true);
            // The ReorderableList uses the GroupNames property as an indicator for how many groups really do exist.
            m_TextureMipmapLimitGroupsList.drawHeaderCallback = DrawTextureMipmapLimitGroupsHeader;
            m_TextureMipmapLimitGroupsList.drawElementCallback = DrawTextureMipmapLimitGroupsElement;
            m_TextureMipmapLimitGroupsList.drawFooterCallback = DrawTextureMipmapLimitGroupsFooter;
            m_TextureMipmapLimitGroupsList.onAddCallback = AddTextureMipmapLimitGroup;
            m_TextureMipmapLimitGroupsList.onRemoveCallback = RemoveTextureMipmapLimitGroup;
        }

        private struct QualitySetting
        {
            public string m_Name;
            public string m_PropertyPath;
            public List<string> m_ExcludedPlatforms;
        }

        private readonly int m_QualityElementHash = "QualityElementHash".GetHashCode();
        private class Dragging
        {
            public int m_StartPosition;
            public int m_Position;
        }

        private Dragging m_Dragging;
        private bool m_ShouldAddNewLevel;
        private int m_DeleteLevel = -1;
        private int DoQualityLevelSelection(int currentQualitylevel, IList<QualitySetting> qualitySettings, Dictionary<string, int> platformDefaultQualitySettings)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            var selectedLevel = currentQualitylevel;

            //Header row
            GUILayout.BeginHorizontal();

            Rect header = GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle, GUILayout.ExpandWidth(false), GUILayout.Width(Styles.kLabelWidth), GUILayout.Height(Styles.kHeaderRowHeight));
            header.x += EditorGUI.indent;
            header.width -= EditorGUI.indent;
            GUI.Label(header, "Levels", EditorStyles.boldLabel);

            //Header row icons
            foreach (var platform in m_ValidPlatforms)
            {
                var iconRect = GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle, GUILayout.MinWidth(Styles.kMinToggleWidth), GUILayout.MaxWidth(Styles.kMaxToggleWidth), GUILayout.Height(Styles.kHeaderRowHeight));
                var temp = EditorGUIUtility.TempContent(platform.smallIcon);
                temp.tooltip = platform.title.text;
                GUI.Label(iconRect, temp);
                temp.tooltip = "";
            }

            //Extra column for deleting setting button
            GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle, GUILayout.MinWidth(Styles.kMinToggleWidth), GUILayout.MaxWidth(Styles.kMaxToggleWidth), GUILayout.Height(Styles.kHeaderRowHeight));

            GUILayout.EndHorizontal();

            //Draw the row for each quality setting
            var currentEvent = Event.current;
            for (var i = 0; i < qualitySettings.Count; i++)
            {
                GUILayout.BeginHorizontal();
                var bgStyle = i % 2 == 0 ? Styles.kListEvenBg : Styles.kListOddBg;
                bool selected = (selectedLevel == i);

                //Draw the selected icon if required
                Rect r = GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle, GUILayout.ExpandWidth(false), GUILayout.Width(Styles.kLabelWidth));

                switch (currentEvent.type)
                {
                    case EventType.Repaint:
                        bgStyle.Draw(r, GUIContent.none, false, false, selected, false);
                        GUI.Label(r, EditorGUIUtility.TempContent(qualitySettings[i].m_Name));
                        break;
                    case EventType.MouseDown:
                        if (r.Contains(currentEvent.mousePosition))
                        {
                            selectedLevel = i;
                            GUIUtility.keyboardControl = 0;
                            GUIUtility.hotControl = m_QualityElementHash;
                            GUI.changed = true;
                            m_Dragging = new Dragging {m_StartPosition = i, m_Position = i};
                            currentEvent.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == m_QualityElementHash)
                        {
                            if (r.Contains(currentEvent.mousePosition))
                            {
                                m_Dragging.m_Position = i;
                                currentEvent.Use();
                            }
                        }
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == m_QualityElementHash)
                        {
                            GUIUtility.hotControl = 0;
                            currentEvent.Use();
                        }
                        break;
                    case EventType.KeyDown:
                        if (currentEvent.keyCode == KeyCode.UpArrow || currentEvent.keyCode == KeyCode.DownArrow)
                        {
                            selectedLevel += currentEvent.keyCode == KeyCode.UpArrow ? -1 : 1;
                            selectedLevel = Mathf.Clamp(selectedLevel, 0, qualitySettings.Count - 1);
                            GUIUtility.keyboardControl = 0;
                            GUI.changed = true;
                            currentEvent.Use();
                        }
                        break;
                }

                //Build a list of the current platform selection and draw it.
                foreach (var platform in m_ValidPlatforms)
                {
                    bool isDefaultQuality = platformDefaultQualitySettings.ContainsKey(platform.name) &&  platformDefaultQualitySettings[platform.name] == i;

                    var toggleRect = GUILayoutUtility.GetRect(Content.kPlatformTooltip, Styles.kToggle, GUILayout.MinWidth(Styles.kMinToggleWidth), GUILayout.MaxWidth(Styles.kMaxToggleWidth));
                    if (Event.current.type == EventType.Repaint)
                    {
                        bgStyle.Draw(toggleRect, GUIContent.none, false, false, selected, false);
                    }

                    var color = GUI.backgroundColor;
                    if (isDefaultQuality && !EditorApplication.isPlayingOrWillChangePlaymode)
                        GUI.backgroundColor = Color.green;

                    var supported = !qualitySettings[i].m_ExcludedPlatforms.Contains(platform.name);
                    var newSupported = GUI.Toggle(toggleRect, supported, Content.kPlatformTooltip, isDefaultQuality ? Styles.kDefaultToggle : Styles.kToggle);
                    if (supported != newSupported)
                    {
                        if (newSupported)
                            qualitySettings[i].m_ExcludedPlatforms.Remove(platform.name);
                        else
                            qualitySettings[i].m_ExcludedPlatforms.Add(platform.name);
                    }

                    GUI.backgroundColor = color;
                }

                //Extra column for deleting quality button
                var deleteButton = GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle, GUILayout.MinWidth(Styles.kMinToggleWidth), GUILayout.MaxWidth(Styles.kMaxToggleWidth));
                if (Event.current.type == EventType.Repaint)
                {
                    bgStyle.Draw(deleteButton, GUIContent.none, false, false, selected, false);
                }
                if (GUI.Button(deleteButton, Content.kIconTrash, GUIStyle.none))
                    m_DeleteLevel = i;
                GUILayout.EndHorizontal();
            }

            //Add a spacer line to separate the levels from the defaults
            GUILayout.BeginHorizontal();
            DrawHorizontalDivider();
            GUILayout.EndHorizontal();

            //Default platform selection dropdowns
            GUILayout.BeginHorizontal();

            var defaultQualityTitle = GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle, GUILayout.ExpandWidth(false), GUILayout.Width(Styles.kLabelWidth), GUILayout.Height(Styles.kHeaderRowHeight));
            defaultQualityTitle.x += EditorGUI.indent;
            defaultQualityTitle.width -= EditorGUI.indent;
            GUI.Label(defaultQualityTitle, "Default", EditorStyles.boldLabel);

            // Draw default dropdown arrows
            foreach (var platform in m_ValidPlatforms)
            {
                var iconRect = GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle,
                    GUILayout.MinWidth(Styles.kMinToggleWidth),
                    GUILayout.MaxWidth(Styles.kMaxToggleWidth),
                    GUILayout.Height(Styles.kHeaderRowHeight));

                int position;
                if (!platformDefaultQualitySettings.TryGetValue(platform.name, out position))
                    platformDefaultQualitySettings.Add(platform.name, 0);

                position = EditorGUI.Popup(iconRect, position, qualitySettings.Select(x => x.m_Name).ToArray(), Styles.kDefaultDropdown);
                platformDefaultQualitySettings[platform.name] = position;
            }

            //Extra column for deleting setting button
            GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle, GUILayout.MinWidth(Styles.kMinToggleWidth), GUILayout.MaxWidth(Styles.kMaxToggleWidth), GUILayout.Height(Styles.kHeaderRowHeight));

            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            //Add an extra row for 'Add' button
            GUILayout.BeginHorizontal();
            var addButtonRect = GUILayoutUtility.GetRect(Content.kAddQualityLevel, Styles.kToggle, GUILayout.ExpandWidth(true));

            if (GUI.Button(addButtonRect, Content.kAddQualityLevel))
                m_ShouldAddNewLevel = true;

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            return selectedLevel;
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

                qs.m_PropertyPath = prop.propertyPath;

                var platforms = new List<string>();
                var platformsProp = prop.FindPropertyRelative("excludedTargetPlatforms");
                foreach (SerializedProperty platformProp in platformsProp)
                    platforms.Add(platformProp.stringValue);

                qs.m_ExcludedPlatforms = platforms;
                qualitySettings.Add(qs);
            }
            return qualitySettings;
        }

        private void SetQualitySettings(IEnumerable<QualitySetting> settings)
        {
            foreach (var setting in settings)
            {
                var property = m_QualitySettings.FindProperty(setting.m_PropertyPath);
                if (property == null)
                    continue;

                var platformsProp = property.FindPropertyRelative("excludedTargetPlatforms");
                if (platformsProp.arraySize != setting.m_ExcludedPlatforms.Count)
                    platformsProp.arraySize = setting.m_ExcludedPlatforms.Count;

                var count = 0;
                foreach (SerializedProperty platform in platformsProp)
                {
                    if (platform.stringValue != setting.m_ExcludedPlatforms[count])
                        platform.stringValue = setting.m_ExcludedPlatforms[count];
                    count++;
                }
            }
        }

        private void HandleAddRemoveQualitySetting(ref int selectedLevel, Dictionary<string, int> platformDefaults)
        {
            if (m_DeleteLevel >= 0)
            {
                if (m_DeleteLevel < selectedLevel || m_DeleteLevel == m_QualitySettingsProperty.arraySize - 1)
                {
                    selectedLevel = Mathf.Max(0, selectedLevel - 1);
                    QualitySettings.SetQualityLevel(selectedLevel);
                }

                //Always ensure there is one quality setting
                if (m_QualitySettingsProperty.arraySize > 1 && m_DeleteLevel >= 0 && m_DeleteLevel < m_QualitySettingsProperty.arraySize)
                {
                    m_QualitySettingsProperty.DeleteArrayElementAtIndex(m_DeleteLevel);

                    // Fix defaults offset
                    List<string> keys = new List<string>(platformDefaults.Keys);
                    foreach (var key in keys)
                    {
                        int value = platformDefaults[key];
                        if (value != 0 && value >= m_DeleteLevel)
                            platformDefaults[key]--;
                    }
                }

                m_DeleteLevel = -1;
            }

            if (m_ShouldAddNewLevel)
            {
                m_QualitySettingsProperty.arraySize++;
                var addedSetting = m_QualitySettingsProperty.GetArrayElementAtIndex(m_QualitySettingsProperty.arraySize - 1);
                var nameProperty = addedSetting.FindPropertyRelative("name");
                nameProperty.stringValue = "Level " + (m_QualitySettingsProperty.arraySize - 1);

                m_ShouldAddNewLevel = false;
            }
        }

        private Dictionary<string, int> GetDefaultQualityForPlatforms()
        {
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

            EditorGUILayout.HelpBox(Content.kMipStrippingHint.text, MessageType.Info, false);
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
                var buildTargetGroupName = BuildPipeline.GetBuildTargetGroup(buildTarget).ToString();

                if (!QualitySettings.SamePipelineAssetsForPlatform(buildTargetGroupName))
                    s_PlatformsWithDifferentRPAssets.Add(platform.title.ToString());
            }

            if (s_PlatformsWithDifferentRPAssets.Any())
            {
                EditorGUILayout.HelpBox($"The following platforms have assets in its associated Quality levels that belong to different render pipelines: {string.Join(", ", s_PlatformsWithDifferentRPAssets)}", MessageType.Error);
            }
        }

        public override void OnInspectorGUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox("Changes made in play mode will not be saved.", MessageType.Warning, true);
            }

            m_QualitySettings.Update();

            var settings = GetQualitySettings();
            var defaults = GetDefaultQualityForPlatforms();
            var selectedLevel = QualitySettings.GetQualityLevel();
            if (selectedLevel >= m_QualitySettingsProperty.arraySize)
            {
                selectedLevel = m_QualitySettingsProperty.arraySize - 1;
            }

            EditorGUI.BeginChangeCheck();
            selectedLevel = DoQualityLevelSelection(selectedLevel, settings, defaults);
            if (EditorGUI.EndChangeCheck())
                QualitySettings.SetQualityLevel(selectedLevel);

            SetQualitySettings(settings);
            HandleAddRemoveQualitySetting(ref selectedLevel, defaults);
            SetDefaultQualityForPlatforms(defaults);
            GUILayout.Space(10.0f);
            DrawHorizontalDivider();
            GUILayout.Space(10.0f);

            var currentSettings = m_QualitySettingsProperty.GetArrayElementAtIndex(selectedLevel);
            var nameProperty = currentSettings.FindPropertyRelative("name");
            var pixelLightCountProperty = currentSettings.FindPropertyRelative("pixelLightCount");
            var shadowsProperty = currentSettings.FindPropertyRelative("shadows");
            var shadowResolutionProperty = currentSettings.FindPropertyRelative("shadowResolution");
            var shadowProjectionProperty = currentSettings.FindPropertyRelative("shadowProjection");
            var shadowCascadesProperty = currentSettings.FindPropertyRelative("shadowCascades");
            var shadowDistanceProperty = currentSettings.FindPropertyRelative("shadowDistance");
            var shadowNearPlaneOffsetProperty = currentSettings.FindPropertyRelative("shadowNearPlaneOffset");
            var shadowCascade2SplitProperty = currentSettings.FindPropertyRelative("shadowCascade2Split");
            var shadowCascade4SplitProperty = currentSettings.FindPropertyRelative("shadowCascade4Split");
            var shadowMaskUsageProperty = currentSettings.FindPropertyRelative("shadowmaskMode");
            var skinWeightsProperty = currentSettings.FindPropertyRelative("skinWeights");
            var globalTextureMipmapLimitProperty = currentSettings.FindPropertyRelative("globalTextureMipmapLimit");
            m_TextureMipmapLimitGroupSettingsProperty = currentSettings.FindPropertyRelative("textureMipmapLimitSettings");
            var anisotropicTexturesProperty = currentSettings.FindPropertyRelative("anisotropicTextures");
            var antiAliasingProperty = currentSettings.FindPropertyRelative("antiAliasing");
            var softParticlesProperty = currentSettings.FindPropertyRelative("softParticles");
            var realtimeReflectionProbes = currentSettings.FindPropertyRelative("realtimeReflectionProbes");
            var billboardsFaceCameraPosition = currentSettings.FindPropertyRelative("billboardsFaceCameraPosition");
            var useLegacyDetailsDistribution = currentSettings.FindPropertyRelative("useLegacyDetailDistribution");
            var terrainQualityOverridesProperty = currentSettings.FindPropertyRelative("terrainQualityOverrides");
            var terrainPixelErrorProperty = currentSettings.FindPropertyRelative("terrainPixelError");
            var terrainDetailDensityScaleProperty = currentSettings.FindPropertyRelative("terrainDetailDensityScale");
            var terrainBasemapDistanceProperty = currentSettings.FindPropertyRelative("terrainBasemapDistance");
            var terrainDetailDistanceProperty = currentSettings.FindPropertyRelative("terrainDetailDistance");
            var terrainTreeDistanceProperty = currentSettings.FindPropertyRelative("terrainTreeDistance");
            var terrainBillboardStartProperty = currentSettings.FindPropertyRelative("terrainBillboardStart");
            var terrainFadeLengthProperty = currentSettings.FindPropertyRelative("terrainFadeLength");
            var terrainMaxTreesProperty = currentSettings.FindPropertyRelative("terrainMaxTrees");
            var vSyncCountProperty = currentSettings.FindPropertyRelative("vSyncCount");
            var lodBiasProperty = currentSettings.FindPropertyRelative("lodBias");
            var maximumLODLevelProperty = currentSettings.FindPropertyRelative("maximumLODLevel");
            var enableLODCrossFadeProperty = currentSettings.FindPropertyRelative("enableLODCrossFade");
            var particleRaycastBudgetProperty = currentSettings.FindPropertyRelative("particleRaycastBudget");
            var asyncUploadTimeSliceProperty = currentSettings.FindPropertyRelative("asyncUploadTimeSlice");
            var asyncUploadBufferSizeProperty = currentSettings.FindPropertyRelative("asyncUploadBufferSize");
            var asyncUploadPersistentBufferProperty = currentSettings.FindPropertyRelative("asyncUploadPersistentBuffer");
            var resolutionScalingFixedDPIFactorProperty = currentSettings.FindPropertyRelative("resolutionScalingFixedDPIFactor");

            var customRenderPipeline = currentSettings.FindPropertyRelative("customRenderPipeline");

            CheckSameRenderPipelineAssetForOverridenQualityLevels();

            bool usingSRP = GraphicsSettings.currentRenderPipeline != null;

            if (string.IsNullOrEmpty(nameProperty.stringValue))
                nameProperty.stringValue = "Level " + selectedLevel;

            GUILayout.Label(EditorGUIUtility.TempContent("Current Active Quality Level"), EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(nameProperty);

            if (usingSRP)
                EditorGUILayout.HelpBox("A Scriptable Render Pipeline is in use, some settings will not be used and are hidden", MessageType.Info);
            GUILayout.Space(10);

            GUILayout.Label(EditorGUIUtility.TempContent("Rendering"), EditorStyles.boldLabel);

            EditorGUI.RenderPipelineAssetField(Content.kRenderPipelineObject, m_QualitySettings, customRenderPipeline);
            if (!usingSRP && customRenderPipeline.objectReferenceValue != null)
                EditorGUILayout.HelpBox("Missing a Scriptable Render Pipeline in Graphics: \"Scriptable Render Pipeline Settings\" to use Scriptable Render Pipeline from Quality: \"Custom Render Pipeline\".", MessageType.Warning);

            if (!usingSRP)
                EditorGUILayout.PropertyField(pixelLightCountProperty);

            // still valid with SRP
            if (!usingSRP)
            {
                EditorGUILayout.PropertyField(antiAliasingProperty);
            }

            if (!SupportedRenderingFeatures.active.overridesRealtimeReflectionProbes)
                EditorGUILayout.PropertyField(realtimeReflectionProbes);
            EditorGUILayout.PropertyField(resolutionScalingFixedDPIFactorProperty);

            EditorGUILayout.PropertyField(vSyncCountProperty, Content.kVSyncCountLabel);

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android ||
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS ||
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.tvOS)
            {
                if (vSyncCountProperty.intValue > 0)
                    EditorGUILayout.HelpBox(EditorGUIUtility.TrTextContent($"VSync Count '{vSyncCountProperty.enumLocalizedDisplayNames[vSyncCountProperty.enumValueIndex]}' is ignored on Android, iOS and tvOS.", EditorGUIUtility.GetHelpIcon(MessageType.Warning)));
            }

            bool shadowMaskSupported = SupportedRenderingFeatures.IsMixedLightingModeSupported(MixedLightingMode.Shadowmask);
            bool showShadowMaskUsage = shadowMaskSupported && !SupportedRenderingFeatures.active.overridesShadowmask;

            GUILayout.Space(10);
            GUILayout.Label(EditorGUIUtility.TempContent("Textures"), EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(globalTextureMipmapLimitProperty, Content.kGlobalTextureMipmapLimit);
            if (EditorGUI.EndChangeCheck() && usingSRP)
            {
                RenderPipelineManager.CleanupRenderPipeline();
            }
            if (QualitySettings.IsTextureResReducedOnAnyPlatform())
            {
                MipStrippingHintGUI();
            }

            EditorGUILayout.Space(3);
            m_TextureMipmapLimitGroupsList.DoLayoutList();

            EditorGUILayout.PropertyField(anisotropicTexturesProperty);

            var streamingMipmapsActiveProperty = currentSettings.FindPropertyRelative("streamingMipmapsActive");
            EditorGUILayout.PropertyField(streamingMipmapsActiveProperty, Content.kStreamingMipmapsActive);
            if (streamingMipmapsActiveProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                var streamingMipmapsAddAllCameras = currentSettings.FindPropertyRelative("streamingMipmapsAddAllCameras");
                EditorGUILayout.PropertyField(streamingMipmapsAddAllCameras, Content.kStreamingMipmapsAddAllCameras);
                var streamingMipmapsBudgetProperty = currentSettings.FindPropertyRelative("streamingMipmapsMemoryBudget");
                EditorGUILayout.PropertyField(streamingMipmapsBudgetProperty, Content.kStreamingMipmapsMemoryBudget);
                var streamingMipmapsRenderersPerFrameProperty = currentSettings.FindPropertyRelative("streamingMipmapsRenderersPerFrame");
                EditorGUILayout.PropertyField(streamingMipmapsRenderersPerFrameProperty, Content.kStreamingMipmapsRenderersPerFrame);
                var streamingMipmapsMaxLevelReductionProperty = currentSettings.FindPropertyRelative("streamingMipmapsMaxLevelReduction");
                EditorGUILayout.PropertyField(streamingMipmapsMaxLevelReductionProperty, Content.kStreamingMipmapsMaxLevelReduction);
                var streamingMipmapsMaxFileIORequestsProperty = currentSettings.FindPropertyRelative("streamingMipmapsMaxFileIORequests");
                EditorGUILayout.PropertyField(streamingMipmapsMaxFileIORequestsProperty, Content.kStreamingMipmapsMaxFileIORequests);
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(10);
            GUILayout.Label(EditorGUIUtility.TempContent("Particles"), EditorStyles.boldLabel);
            if (!usingSRP)
            {
                EditorGUILayout.PropertyField(softParticlesProperty);
                if (softParticlesProperty.boolValue)
                    SoftParticlesHintGUI();
            }
            EditorGUILayout.PropertyField(particleRaycastBudgetProperty);

            GUILayout.Space(10);
            GUILayout.Label(EditorGUIUtility.TempContent("Terrain"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(billboardsFaceCameraPosition, Content.kBillboardsFaceCameraPos);
            EditorGUILayout.PropertyField(useLegacyDetailsDistribution, Content.kUseLegacyDistribution);

            GUILayout.Space(5);
            GUILayout.Label(EditorGUIUtility.TempContent("Terrain Setting Overrides"), EditorStyles.boldLabel);

            var originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth -= EditorStyles.toggle.CalcSize(GUIContent.none).x + EditorStyles.toggle.margin.left;

            EditorGUILayout.BeginHorizontal();
            bool pixelErrorActive = DrawOverrideToggle(ref terrainQualityOverridesProperty, TerrainQualityOverrides.PixelError, Content.kOverrideTerrainPixelError);
            using (new EditorGUI.DisabledScope(!pixelErrorActive))
                EditorGUILayout.Slider(terrainPixelErrorProperty, 1.0f, 200.0f, Content.kTerrainPixelError);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool basemapActive = DrawOverrideToggle(ref terrainQualityOverridesProperty, TerrainQualityOverrides.BasemapDistance, Content.kOverrideTerrainBasemapDist);
            using (new EditorGUI.DisabledScope(!basemapActive))
            {
                EditorGUI.BeginChangeCheck();
                var newValue = EditorGUILayout.PowerSlider(Content.kTerrainBasemapDistance, Mathf.Clamp(terrainBasemapDistanceProperty.floatValue, 0.0f, 20000.0f), 0.0f, 20000.0f, 2);
                if (EditorGUI.EndChangeCheck())
                    terrainBasemapDistanceProperty.floatValue = newValue;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool detailDensityActive = DrawOverrideToggle(ref terrainQualityOverridesProperty, TerrainQualityOverrides.DetailDensity, Content.kOverrideTerrainDensityScale);
            using (new EditorGUI.DisabledScope(!detailDensityActive))
                EditorGUILayout.Slider(terrainDetailDensityScaleProperty, 0.0f, 1.0f, Content.kTerrainDetailDensityScale);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool detailDistanceActive = DrawOverrideToggle(ref terrainQualityOverridesProperty, TerrainQualityOverrides.DetailDistance, Content.kOverrideTerrainDetailDistance);
            using (new EditorGUI.DisabledScope(!detailDistanceActive))
                EditorGUILayout.Slider(terrainDetailDistanceProperty, 0, 1000, Content.kTerrainDetailDistance);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool treeDistanceActive = DrawOverrideToggle(ref terrainQualityOverridesProperty, TerrainQualityOverrides.TreeDistance, Content.kOverrideTerrainTreeDistance);
            using (new EditorGUI.DisabledScope(!treeDistanceActive))
                EditorGUILayout.Slider(terrainTreeDistanceProperty, 0.0f, 5000.0f, Content.kTerrainTreeDistance);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool billboardStartActive = DrawOverrideToggle(ref terrainQualityOverridesProperty, TerrainQualityOverrides.BillboardStart, Content.kOverrideTerrainBillboardStart);
            using (new EditorGUI.DisabledScope(!billboardStartActive))
                EditorGUILayout.Slider(terrainBillboardStartProperty, 5, 2000, Content.kTerrainBillboardStart);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool fadeLengthActive = DrawOverrideToggle(ref terrainQualityOverridesProperty, TerrainQualityOverrides.FadeLength, Content.kOverrideTerrainFadeLength);
            using (new EditorGUI.DisabledScope(!fadeLengthActive))
                EditorGUILayout.Slider(terrainFadeLengthProperty, 0, 200, Content.kTerrainFadeLength);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool maxTreesActive = DrawOverrideToggle(ref terrainQualityOverridesProperty, TerrainQualityOverrides.MaxTrees, Content.kOverrideTerrainMaxTrees);
            using (new EditorGUI.DisabledScope(!maxTreesActive))
                EditorGUILayout.IntSlider(terrainMaxTreesProperty, 0, 10000, Content.kTerrainMaxTrees);
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = originalLabelWidth;

            if (!usingSRP || showShadowMaskUsage)
            {
                GUILayout.Space(10);

                GUILayout.Label(EditorGUIUtility.TempContent("Shadows"), EditorStyles.boldLabel);

                if (showShadowMaskUsage)
                    EditorGUILayout.PropertyField(shadowMaskUsageProperty);

                if (!usingSRP)
                {
                    EditorGUILayout.PropertyField(shadowsProperty);
                    EditorGUILayout.PropertyField(shadowResolutionProperty);
                    EditorGUILayout.PropertyField(shadowProjectionProperty);
                    EditorGUILayout.PropertyField(shadowDistanceProperty);
                    EditorGUILayout.PropertyField(shadowNearPlaneOffsetProperty);
                    EditorGUILayout.PropertyField(shadowCascadesProperty);

                    if (shadowCascadesProperty.intValue == 2)
                        DrawCascadeSplitGUI<float>(ref shadowCascade2SplitProperty);
                    else if (shadowCascadesProperty.intValue == 4)
                        DrawCascadeSplitGUI<Vector3>(ref shadowCascade4SplitProperty);
                }
            }

            GUILayout.Space(10);
            GUILayout.Label(EditorGUIUtility.TempContent("Async Asset Upload"), EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(asyncUploadTimeSliceProperty, Content.kAsyncUploadTimeSlice);
            EditorGUILayout.PropertyField(asyncUploadBufferSizeProperty, Content.kAsyncUploadBufferSize);
            EditorGUILayout.PropertyField(asyncUploadPersistentBufferProperty, Content.kAsyncUploadPersistentBuffer);

            asyncUploadTimeSliceProperty.intValue = Mathf.Clamp(asyncUploadTimeSliceProperty.intValue, kMinAsyncUploadTimeSlice, kMaxAsyncUploadTimeSlice);
            asyncUploadBufferSizeProperty.intValue = Mathf.Clamp(asyncUploadBufferSizeProperty.intValue, kMinAsyncRingBufferSize, kMaxAsyncRingBufferSize);

            GUILayout.Space(10);
            GUILayout.Label(EditorGUIUtility.TempContent("Level of Detail"), EditorStyles.boldLabel);

            if (!SupportedRenderingFeatures.active.overridesLODBias)
                EditorGUILayout.PropertyField(lodBiasProperty, Content.kLODBiasLabel);
            if (!SupportedRenderingFeatures.active.overridesMaximumLODLevel)
                EditorGUILayout.PropertyField(maximumLODLevelProperty, Content.kMaximumLODLevelLabel);
            if (!SupportedRenderingFeatures.active.overridesEnableLODCrossFade)
                EditorGUILayout.PropertyField(enableLODCrossFadeProperty, Content.kEnableLODCrossFadeLabel);

            GUILayout.Space(10);
            GUILayout.Label(EditorGUIUtility.TempContent("Meshes"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(skinWeightsProperty);

            if (m_Dragging != null && m_Dragging.m_Position != m_Dragging.m_StartPosition)
            {
                m_QualitySettingsProperty.MoveArrayElement(m_Dragging.m_StartPosition, m_Dragging.m_Position);
                m_Dragging.m_StartPosition = m_Dragging.m_Position;
                selectedLevel = m_Dragging.m_Position;

                m_QualitySettings.ApplyModifiedProperties();
                QualitySettings.SetQualityLevel(Mathf.Clamp(selectedLevel, 0, m_QualitySettingsProperty.arraySize - 1));
            }

            m_QualitySettings.ApplyModifiedProperties();
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
                                if (!m_QualitySettingsPreset.excludedProperties.Any(p => p == groupSettingsArrayPropertyPath || groupSettingsArrayPropertyPath.StartsWith(p + ".", System.StringComparison.Ordinal)))
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
                                    if (!m_QualitySettingsPreset.excludedProperties.Any(p => p == propertyPathToCheck || propertyPathToCheck.StartsWith(p + ".", System.StringComparison.Ordinal)))
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

            DoTextureMipmapLimitGroupNameLabel(labelPosition, EditorGUIUtility.TempContent(groupName, L10n.Tr("Mipmap Limit Group") + $" {index}"), index, groupName);
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
                GUI.Label(rect, label, EditorStyles.label);
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

            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                GUI.SetNextControlName(controlName);
                string newName = EditorGUI.DelayedTextField(rect, label.text, EditorStyles.textField);

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
                    if (changed.changed)
                    {
                        EndRenamingTextureMipmapLimitGroup(newName);
                    }
                    // If clicking out, the rename is NOT cancelled.
                    else if (e.isMouse && !rect.Contains(e.mousePosition))
                    {
                        EndRenamingTextureMipmapLimitGroup(EditorGUI.s_DelayedTextEditor.text);
                    }
                    else if (!EditorGUIUtility.editingTextField)
                    {
                        EndRenamingTextureMipmapLimitGroup();
                    }
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
                int selection = EditorUtility.DisplayDialogComplex(Content.kTextureMipmapLimitGroupsDialogTitle,
                string.Format(Content.kTextureMipmapLimitGroupsDialogMessageOnRemove, nameOfGroupToRemove),
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
            try
            {
                string[] guids = AssetDatabase.FindAssets("t:texture");
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < guids.Length; ++i)
                {
                    AssetImporter importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guids[i]));

                    if (importer is TextureImporter)
                    {
                        TextureImporter texImporter = importer as TextureImporter;
                        if (texImporter.textureShape == TextureImporterShape.Texture2D && texImporter.mipmapLimitGroupName == oldName)
                        {
                            texImporter.mipmapLimitGroupName = newName;
                            importer.SaveAndReimport();
                        }
                    }
                    else if (importer is IHVImageFormatImporter)
                    {
                        IHVImageFormatImporter ihvImporter = importer as IHVImageFormatImporter;
                        if (ihvImporter.mipmapLimitGroupName == oldName)
                        {
                            ihvImporter.mipmapLimitGroupName = newName;
                            importer.SaveAndReimport();
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog(Content.kTextureMipmapLimitGroupsDialogTitle,
                    string.Format(Content.kTextureMipmapLimitGroupsDialogMessageOnUpdateAssetsError, e.Message),
                    L10n.Tr("OK"));
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
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
            List<Object> newSelection = new List<Object>();
            string[] guids = AssetDatabase.FindAssets("t:texture");

            for (int i = 0; i < guids.Length; ++i)
            {
                TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guids[i])) as TextureImporter;
                if (importer is not null && importer.textureShape == TextureImporterShape.Texture2D && importer.mipmapLimitGroupName == groupNameToIdentify)
                {
                    newSelection.Add(AssetDatabase.LoadAssetAtPath<Object>(importer.assetPath));
                }
            }

            if (newSelection.Count > 0)
            {
                Selection.objects = newSelection.ToArray();
            }
            else
            {
                EditorUtility.DisplayDialog(Content.kTextureMipmapLimitGroupsDialogTitle,
                    string.Format(Content.kTextureMipmapLimitGroupsDialogMessageOnIdentifyFail, groupNameToIdentify),
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

                for (int i = 0; i < m_TextureMipmapLimitGroupNamesProperty.arraySize; ++i)
                {
                    if (m_TextureMipmapLimitGroupNamesProperty.GetArrayElementAtIndex(i).stringValue == newName)
                    {
                        EditorUtility.DisplayDialog(Content.kTextureMipmapLimitGroupsDialogTitle,
                            string.Format(Content.kTextureMipmapLimitGroupsDialogMessageOnRenameFail, newName, toRename),
                            L10n.Tr("OK"));
                        return;
                    }
                }

                bool applyModifiedProperties = true;
                if (m_TextureMipmapLimitGroupsRenameShowUpdatePrompt)
                {
                    int selection = EditorUtility.DisplayDialogComplex(Content.kTextureMipmapLimitGroupsDialogTitle,
                        string.Format(Content.kTextureMipmapLimitGroupsDialogMessageOnRename, toRename, newName),
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
            while (existingNames.Any(existingName => existingName == newName))
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

        [SettingsProvider]
        internal static SettingsProvider CreateProjectSettingsProvider()
        {
            var provider = AssetSettingsProvider.CreateProviderFromAssetPath(
                "Project/Quality", "ProjectSettings/QualitySettings.asset",
                SettingsProvider.GetSearchKeywordsFromGUIContentProperties<Styles>()
                    .Concat(SettingsProvider.GetSearchKeywordsFromGUIContentProperties<Content>())
                    .Concat(SettingsProvider.GetSearchKeywordsFromPath("ProjectSettings/QualitySettings.asset")));
            return provider;
        }
    }
}
