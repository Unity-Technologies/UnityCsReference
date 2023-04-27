// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngineInternal;
using UnityEngine.Rendering;
using System.Text;
using System.Globalization;
using System.Linq;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Lighting", icon = "Lighting")]
    internal class LightingWindow : EditorWindow
    {
        static class Styles
        {
            public static readonly GUIContent[] modeStrings =
            {
                EditorGUIUtility.TrTextContent("Scene"),
                EditorGUIUtility.TrTextContent("Environment"),
                EditorGUIUtility.TrTextContent("Realtime Lightmaps"),
                EditorGUIUtility.TrTextContent("Baked Lightmaps")
            };

            public static readonly GUIStyle labelStyle = EditorStyles.wordWrappedMiniLabel;
            public static readonly GUIStyle buttonStyle = "LargeButton";
            public static readonly GUIContent continuousBakeLabel = EditorGUIUtility.TrTextContent("Auto Generate", "Generate lighting data in the Scene when there are changes that affect Scene lighting, such as modifications to lights, materials, or geometry. This option is only available when there is a Lighting Settings Asset assigned in the Lighting Window.");
            public static readonly GUIContent bakeLabel = EditorGUIUtility.TrTextContent("Generate Lighting", "Generates the lightmap data for the current active scene. This lightmap data (for realtime and baked global illumination) is stored in the GI Cache. For GI Cache settings see the Preferences panel.");
            public static readonly GUIContent bakeLabelAnythingCompiling = EditorGUIUtility.TrTextContent("Generate Lighting", "Generate Lighting is currently unavailable. Waiting for asynchronous shader compilation.");
            public static readonly GUIContent cancelLabel = EditorGUIUtility.TrTextContent("Cancel");

            public static readonly GUIContent progressiveGPUBakingDevice = EditorGUIUtility.TrTextContent("GPU Baking Device", "Will list all available GPU devices.");
            public static readonly GUIContent progressiveGPUUnknownDeviceInfo = EditorGUIUtility.TrTextContent("No devices found. Please start an initial bake to make this information available.");
            public static readonly GUIContent progressiveGPUChangeWarning = EditorGUIUtility.TrTextContent("Changing the compute device used by the Progressive GPU Lightmapper requires the editor to be relaunched. Do you want to change device and restart?");
            public static readonly GUIContent gpuBakingProfile = EditorGUIUtility.TrTextContent("GPU Baking Profile", "The profile chosen for trading off between performance and memory usage when baking using the GPU.");

            public static readonly int[] progressiveGPUUnknownDeviceValues = { 0 };
            public static readonly GUIContent[] progressiveGPUUnknownDeviceStrings =
            {
                EditorGUIUtility.TrTextContent("Unknown"),
            };

            // Keep in sync with BakingProfile.h::BakingProfile
            public static readonly int bakingProfileDefault = 2;
            public static readonly int[] bakingProfileValues = { 0, 1, 2, 3, 4 };
            public static readonly GUIContent[] bakingProfileStrings =
            {
                EditorGUIUtility.TrTextContent("Highest Performance"),
                EditorGUIUtility.TrTextContent("High Performance"),
                EditorGUIUtility.TrTextContent("Automatic"),
                EditorGUIUtility.TrTextContent("Low Memory Usage"),
                EditorGUIUtility.TrTextContent("Lowest Memory Usage"),
            };

            public static string[] BakeModeStrings =
            {
                "Bake Reflection Probes",
                "Clear Baked Data"
            };

            public static readonly string BakingPausedHelpText = $"{Styles.bakeLabel.text} is currently unavailable. Waiting for asynchronous shader compilation to finish...";
        }

        public interface WindowTab
        {
            void OnEnable();
            void OnDisable();
            void OnGUI();
            void OnSummaryGUI();
            void OnSelectionChange();
        }

        enum BakeMode
        {
            BakeReflectionProbes = 0,
            Clear = 1
        }

        enum Mode
        {
            LightingSettings = 0,
            EnvironmentSettings,
            RealtimeLightmaps,
            BakedLightmaps
        }

        const string kGlobalIlluminationUnityManualPage = "https://docs.unity3d.com/Manual/lighting-window.html";

        const string m_LightmappingDeviceIndexKey = "lightmappingDeviceIndex";
        const string m_BakingProfileKey = "lightmappingBakingProfile";

        int m_SelectedModeIndex = 0;
        List<Mode> m_Modes = null;
        GUIContent[] m_ModeStrings;

        Dictionary<Mode, WindowTab> m_Tabs = new Dictionary<Mode, WindowTab>();

        SerializedObject m_LightingSettings;
        SerializedProperty m_WorkflowMode;

        bool m_IsRealtimeSupported = false;
        bool m_IsBakedSupported = false;
        bool m_IsEnvironmentSupported = false;
        bool m_LightingSettingsReadOnlyMode = false;

        SerializedObject lightingSettings
        {
            get
            {
                // if we set a new scene as the active scene, we need to make sure to respond to those changes
                if (m_LightingSettings == null || m_LightingSettings.targetObject == null || m_LightingSettings.targetObject != Lightmapping.lightingSettingsInternal)
                {
                    var targetObject = Lightmapping.lightingSettingsInternal;
                    m_LightingSettingsReadOnlyMode = false;

                    if (targetObject == null)
                    {
                        targetObject = Lightmapping.lightingSettingsDefaults;
                        m_LightingSettingsReadOnlyMode = true;
                    }

                    m_LightingSettings = new SerializedObject(targetObject);
                    m_WorkflowMode = m_LightingSettings.FindProperty("m_GIWorkflowMode");
                }
                return m_LightingSettings;
            }
        }

        // for internal debug use only
        internal void SetSelectedTabIndex(int index)
        {
            m_SelectedModeIndex = index;
        }

        LightingWindow()
        {
            m_Tabs.Add(Mode.LightingSettings, new LightingWindowLightingTab());
            m_Tabs.Add(Mode.EnvironmentSettings, new LightingWindowEnvironmentTab());
            m_Tabs.Add(Mode.RealtimeLightmaps, new LightingWindowLightmapPreviewTab(LightmapType.DynamicLightmap));
            m_Tabs.Add(Mode.BakedLightmaps, new LightingWindowLightmapPreviewTab(LightmapType.StaticLightmap));

            var customWindowTabs = TypeCache.GetTypesDerivedFrom<LightingWindowTab>();
            foreach (Type tabType in customWindowTabs)
            {
                var tab = Activator.CreateInstance(tabType) as LightingWindowTab;
                m_Tabs.Add((Mode)tabType.Name.GetHashCode(), tab);
                tab.m_Parent = this;
            }
        }

        // Repaint when MRays/sec changes
        float m_LastRepaintedMraysPerSec;
        protected void Update()
        {
            float totalNow = Lightmapping.GetLightmapBakePerformanceTotal();
            if (Math.Abs(totalNow - m_LastRepaintedMraysPerSec) < s_MraysPerSecRepaintThreshold)
                return;

            m_LastRepaintedMraysPerSec = totalNow;
            Repaint();
        }

        void OnEnable()
        {
            s_Window = this;

            titleContent = GetLocalizedTitleContent();

            foreach (var pair in m_Tabs)
                pair.Value.OnEnable();

            Undo.undoRedoEvent += OnUndoRedo;
            Lightmapping.lightingDataUpdated += Repaint;

            Repaint();
        }

        void OnDisable()
        {
            foreach (var pair in m_Tabs)
                pair.Value.OnDisable();

            Undo.undoRedoEvent -= OnUndoRedo;
            Lightmapping.lightingDataUpdated -= Repaint;
        }

        private void OnUndoRedo(in UndoRedoInfo info)
        {
            Repaint();
        }

        void OnBecameVisible()
        {
            RepaintSceneAndGameViews();
        }

        void OnBecameInvisible()
        {
            RepaintSceneAndGameViews();
        }

        void OnSelectionChange()
        {
            if (m_Modes == null)
                return;

            foreach (var pair in m_Tabs)
            {
                if (m_Modes.Contains(pair.Key))
                    pair.Value.OnSelectionChange();
            }

            Repaint();
        }

        static internal void RepaintSceneAndGameViews()
        {
            SceneView.RepaintAll();
            PlayModeView.RepaintAll();
        }

        void OnGUI()
        {
            // This is done so that we can adjust the UI when the user swiches SRP
            SetupModes();

            lightingSettings.Update();

            // reset index to settings page if one of the tabs went away
            if (m_SelectedModeIndex < 0 || m_SelectedModeIndex >= m_Modes.Count)
                m_SelectedModeIndex = 0;

            Mode selectedMode = m_Modes[m_SelectedModeIndex];

            DrawTopBarGUI(selectedMode);

            EditorGUILayout.Space();

            if (m_Tabs.ContainsKey(selectedMode))
                m_Tabs[selectedMode].OnGUI();

            // Draw line to separate the bottom portion of the window from the tab being displayed
            Rect lineRect = GUILayoutUtility.topLevel.PeekNext();
            lineRect.height = 1;
            EditorGUI.DrawDelimiterLine(lineRect);
            EditorGUILayout.Space();

            DrawBottomBarGUI(selectedMode);

            lightingSettings.ApplyModifiedProperties();
        }

        void SetupModes()
        {
            if (m_Modes == null)
            {
                m_Modes = new List<Mode>();
            }

            bool isRealtimeSupported = SupportedRenderingFeatures.IsLightmapBakeTypeSupported(LightmapBakeType.Realtime);
            bool isBakedSupported = SupportedRenderingFeatures.IsLightmapBakeTypeSupported(LightmapBakeType.Baked);
            bool isEnvironmentSupported = !(SupportedRenderingFeatures.active.overridesEnvironmentLighting && SupportedRenderingFeatures.active.overridesFog && SupportedRenderingFeatures.active.overridesOtherLightingSettings);

            if (m_IsRealtimeSupported != isRealtimeSupported || m_IsBakedSupported != isBakedSupported || m_IsEnvironmentSupported != isEnvironmentSupported)
            {
                m_Modes.Clear();

                m_IsBakedSupported = isBakedSupported;
                m_IsRealtimeSupported = isRealtimeSupported;
                m_IsEnvironmentSupported = isEnvironmentSupported;
            }

            // if nothing has changed since last time and we have data, we return
            if (m_Modes.Count > 0)
                return;

            List<GUIContent> modeStringList = new List<GUIContent>();

            m_Modes.Add(Mode.LightingSettings);
            modeStringList.Add(Styles.modeStrings[(int)Mode.LightingSettings]);

            if (m_IsEnvironmentSupported)
            {
                m_Modes.Add(Mode.EnvironmentSettings);
                modeStringList.Add(Styles.modeStrings[(int)Mode.EnvironmentSettings]);
            }

            if (m_IsRealtimeSupported)
            {
                m_Modes.Add(Mode.RealtimeLightmaps);
                modeStringList.Add(Styles.modeStrings[(int)Mode.RealtimeLightmaps]);
            }

            if (m_IsBakedSupported)
            {
                m_Modes.Add(Mode.BakedLightmaps);
                modeStringList.Add(Styles.modeStrings[(int)Mode.BakedLightmaps]);
            }

            foreach (var pair in m_Tabs)
            {
                var customTab = pair.Value as LightingWindowTab;
                if (customTab == null) continue;

                int priority = customTab.priority < 0 ? m_Modes.Count : Mathf.Min(customTab.priority, m_Modes.Count);
                m_Modes.Insert(priority, pair.Key);
                modeStringList.Insert(priority, customTab.titleContent);
            }

            Debug.Assert(m_Modes.Count == modeStringList.Count);

            m_ModeStrings = modeStringList.ToArray();
        }

        internal void SetToolbarDirty()
        {
            m_Modes = null;
        }

        void DrawHelpGUI()
        {
            var iconSize = EditorStyles.iconButton.CalcSize(EditorGUI.GUIContents.helpIcon);
            var rect = GUILayoutUtility.GetRect(iconSize.x, iconSize.y);

            if (GUI.Button(rect, EditorGUI.GUIContents.helpIcon, EditorStyles.iconButton))
            {
                Help.ShowHelpPage(kGlobalIlluminationUnityManualPage);
            }
        }

        void DrawSettingsGUI(Mode mode)
        {
            if (mode == Mode.LightingSettings || mode == Mode.EnvironmentSettings)
            {
                var iconSize = EditorStyles.iconButton.CalcSize(EditorGUI.GUIContents.titleSettingsIcon);
                var rect = GUILayoutUtility.GetRect(iconSize.x, iconSize.y);

                if (EditorGUI.DropdownButton(rect, EditorGUI.GUIContents.titleSettingsIcon, FocusType.Passive, EditorStyles.iconButton))
                {
                    if (mode == Mode.LightingSettings)
                        EditorUtility.DisplayCustomMenu(rect, new[] { EditorGUIUtility.TrTextContent("Reset") }, -1, ResetLightingSettings, null);
                    else if (mode == Mode.EnvironmentSettings)
                        EditorUtility.DisplayCustomMenu(rect, new[] { EditorGUIUtility.TrTextContent("Reset") }, -1, ResetEnvironmentSettings, null);
                }
            }
        }

        void ResetLightingSettings(object userData, string[] options, int selected)
        {
            if (Lightmapping.lightingSettingsInternal != null)
            {
                Undo.RecordObjects(new[] { Lightmapping.lightingSettingsInternal }, "Reset Lighting Settings");
                Unsupported.SmartReset(Lightmapping.lightingSettingsInternal);
            }
        }

        void ResetEnvironmentSettings(object userData, string[] options, int selected)
        {
            Undo.RecordObjects(new[] { RenderSettings.GetRenderSettings() }, "Reset Environment Settings");
            Unsupported.SmartReset(RenderSettings.GetRenderSettings());
        }

        void DrawToolbarRange(int start, int end)
        {
            var strings = new GUIContent[end - start];
            Array.Copy(m_ModeStrings, start, strings, 0, strings.Length);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            m_SelectedModeIndex = GUILayout.Toolbar(m_SelectedModeIndex - start, strings, Styles.buttonStyle, GUI.ToolbarButtonSize.FitToContents) + start;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        void DrawTopBarGUI(Mode selectedMode)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            if (selectedMode == Mode.LightingSettings || selectedMode == Mode.EnvironmentSettings)
                GUILayout.Space(EditorStyles.iconButton.CalcSize(EditorGUI.GUIContents.helpIcon).x);

            GUILayout.FlexibleSpace();

            if (m_Modes.Count > 1)
            {
                // Split the toolbar if it doesn't fit horizontally
                bool requireSplit = false;
                float width = position.width - 2 * 16;
                for (int i = 0; i < m_ModeStrings.Length; i++)
                {
                    width -= Styles.buttonStyle.CalcSize(m_ModeStrings[i]).x;
                    if (width <= 0)
                    {
                        requireSplit = true;
                        break;
                    }
                }

                if (requireSplit)
                {
                    // Simply split in two to avoid weird layouts
                    int half = Mathf.CeilToInt(m_ModeStrings.Length / 2.0f);

                    EditorGUILayout.BeginVertical();
                    DrawToolbarRange(0, half);
                    DrawToolbarRange(half, m_ModeStrings.Length);
                    EditorGUILayout.EndVertical();
                }
                else
                    m_SelectedModeIndex = GUILayout.Toolbar(m_SelectedModeIndex, m_ModeStrings, Styles.buttonStyle, GUI.ToolbarButtonSize.FitToContents);
            }

            GUILayout.FlexibleSpace();

            var customTab = m_Tabs[selectedMode] as LightingWindowTab;
            if (customTab != null)
                customTab.OnHeaderSettingsGUI();
            else
            {
                DrawHelpGUI();
                DrawSettingsGUI(selectedMode);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        internal static void FocusTab(LightingWindowTab tab)
        {
            EditorApplication.update += WaitForWindow;
            void WaitForWindow()
            {
                // Open window in case it's closed
                var window = GetWindow<LightingWindow>(utility: false, title: null, focus: false);
                if (window == null || window.m_Modes == null)
                    return;

                EditorApplication.update -= WaitForWindow;

                // Select active tab
                var newMode = (Mode)tab.GetType().Name.GetHashCode();
                if (window.m_Modes.Contains(newMode))
                {
                    window.m_SelectedModeIndex = window.m_Modes.IndexOf(newMode);
                    window.Repaint();
                }
            }
        }

        void BakeDropDownCallback(object data)
        {
            BakeMode mode = (BakeMode)data;

            switch (mode)
            {
                case BakeMode.Clear:
                    DoClear();
                    break;
                case BakeMode.BakeReflectionProbes:
                    DoBakeReflectionProbes();
                    break;
            }
        }

        void DrawGPUDeviceSelector()
        {
            // GPU lightmapper device selection.
            if (Lightmapping.GetLightingSettingsOrDefaultsFallback().lightmapper == LightingSettings.Lightmapper.ProgressiveGPU)
            {
                DeviceAndPlatform[] devicesAndPlatforms = Lightmapping.GetLightmappingGpuDevices();
                if (devicesAndPlatforms.Length > 0)
                {
                    int[] lightmappingDeviceIndices = Enumerable.Range(0, devicesAndPlatforms.Length).ToArray();
                    GUIContent[] lightmappingDeviceStrings = devicesAndPlatforms.Select(x => new GUIContent(x.name)).ToArray();

                    int bakingDeviceAndPlatform = -1;
                    string configDeviceAndPlatform = EditorUserSettings.GetConfigValue(m_LightmappingDeviceIndexKey);
                    if (configDeviceAndPlatform != null)
                    {
                        bakingDeviceAndPlatform = Int32.Parse(configDeviceAndPlatform);
                        bakingDeviceAndPlatform = Mathf.Clamp(bakingDeviceAndPlatform, 0, devicesAndPlatforms.Length - 1); // Removing a GPU and rebooting invalidates the saved value.
                    }
                    else
                        bakingDeviceAndPlatform = Lightmapping.GetLightmapBakeGPUDeviceIndex();

                    Debug.Assert(bakingDeviceAndPlatform != -1);

                    EditorGUI.BeginChangeCheck();
                    using (new EditorGUI.DisabledScope(devicesAndPlatforms.Length < 2))
                    {
                        bakingDeviceAndPlatform = EditorGUILayout.IntPopup(Styles.progressiveGPUBakingDevice, bakingDeviceAndPlatform, lightmappingDeviceStrings, lightmappingDeviceIndices);
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (EditorUtility.DisplayDialog("Warning", Styles.progressiveGPUChangeWarning.text, "OK", "Cancel"))
                        {
                            EditorUserSettings.SetConfigValue(m_LightmappingDeviceIndexKey, bakingDeviceAndPlatform.ToString());
                            DeviceAndPlatform selectedDeviceAndPlatform = devicesAndPlatforms[bakingDeviceAndPlatform];
                            EditorApplication.CloseAndRelaunch(new string[] { "-OpenCL-PlatformAndDeviceIndices", selectedDeviceAndPlatform.platformId.ToString(), selectedDeviceAndPlatform.deviceId.ToString() });
                        }
                    }
                }
                else
                {
                    // To show when we are still fetching info, so that the UI doesn't pop around too much for no reason
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.IntPopup(Styles.progressiveGPUBakingDevice, 0, Styles.progressiveGPUUnknownDeviceStrings, Styles.progressiveGPUUnknownDeviceValues);
                    }

                    EditorGUILayout.HelpBox(Styles.progressiveGPUUnknownDeviceInfo.text, MessageType.Info);
                }
            }
        }

        void DrawBakingProfileSelector()
        {
            // Handle the baking profile setting
            using (new EditorGUI.DisabledScope(Lightmapping.GetLightingSettingsOrDefaultsFallback().lightmapper != LightingSettings.Lightmapper.ProgressiveGPU))
            {
                int bakingProfile = Styles.bakingProfileDefault;
                string bakingProfileString = EditorUserSettings.GetConfigValue(m_BakingProfileKey);
                if (bakingProfileString != null)
                {
                    if (Int32.TryParse(bakingProfileString, out int bakingProfileStoredValue))
                    {
                        const Int32 maxBakingProfile = 4; // Keep in sync with kMaxBakingProfile (C++).
                        if (bakingProfileStoredValue >= 0 && bakingProfileStoredValue <= maxBakingProfile)
                            bakingProfile = bakingProfileStoredValue;
                    }
                }
                bakingProfile = EditorGUILayout.IntPopup(Styles.gpuBakingProfile, bakingProfile, Styles.bakingProfileStrings, Styles.bakingProfileValues);
                EditorUserSettings.SetConfigValue(m_BakingProfileKey, bakingProfile.ToString());
            }
        }

        void DrawBottomBarGUI(Mode selectedMode)
        {
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                // Preamble with warnings.
                if (Lightmapping.lightingDataAsset && !Lightmapping.lightingDataAsset.isValid)
                {
                    EditorGUILayout.HelpBox(Lightmapping.lightingDataAsset.validityErrorMessage, MessageType.Warning);
                }

                bool isEntitiesPackageUsed = PackageManager.PackageInfo.IsPackageRegistered("com.unity.entities");
                bool iterativeInLightingSettings = (m_WorkflowMode.intValue == (int)Lightmapping.GIWorkflowMode.Iterative);

                if (isEntitiesPackageUsed && iterativeInLightingSettings)
                    EditorGUILayout.HelpBox("Unity ignores Auto Generate mode when the Entities package is installed. When you generate lighting in a project where you have installed the Entities package, the Unity Editor opens all loaded subscenes. This may slow down Editor performance.", MessageType.Warning);

                // Auto Generate checkbox.
                EditorGUI.BeginChangeCheck();
                using (new EditorGUI.DisabledScope(m_LightingSettingsReadOnlyMode))
                {
                    iterativeInLightingSettings = EditorGUILayout.Toggle(Styles.continuousBakeLabel, iterativeInLightingSettings);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    m_WorkflowMode.intValue = (int)(iterativeInLightingSettings ? Lightmapping.GIWorkflowMode.Iterative : Lightmapping.GIWorkflowMode.OnDemand);
                }

                // Bake settings.
                DrawGPUDeviceSelector();
                DrawBakingProfileSelector();

                bool resultingIterative = iterativeInLightingSettings && !isEntitiesPackageUsed;
                using (new EditorGUI.DisabledScope(resultingIterative))
                {
                    // Bake button if we are not currently baking
                    bool showBakeButton = resultingIterative || !Lightmapping.isRunning;
                    if (showBakeButton)
                    {
                        bool anythingCompiling = ShaderUtil.anythingCompiling;
                        using (new EditorGUI.DisabledScope(anythingCompiling))
                        {
                            var customTab = m_Tabs[selectedMode] as LightingWindowTab;
                            if (customTab != null)
                                customTab.OnBakeButtonGUI();
                            else
                            {
                                GUIContent guiContent = anythingCompiling ? Styles.bakeLabelAnythingCompiling : Styles.bakeLabel;
                                if (EditorGUI.LargeSplitButtonWithDropdownList(guiContent, Styles.BakeModeStrings, BakeDropDownCallback))
                                {
                                    DoBake();

                                    // DoBake could've spawned a save scene dialog. This breaks GUI on mac (Case 490388).
                                    // We work around this with an ExitGUI here.
                                    GUIUtility.ExitGUI();
                                }
                            }
                        }
                    }
                    // Cancel button if we are currently baking
                    else
                    {
                        if (GUILayout.Button(Styles.cancelLabel, Styles.buttonStyle))
                        {
                            Lightmapping.Cancel();
                        }
                    }
                }

                // Per-tab summaries.
                if (m_Tabs.ContainsKey(selectedMode))
                {
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    m_Tabs[selectedMode].OnSummaryGUI();
                    GUILayout.EndVertical();
                }
            }
        }

        private void DoBake()
        {
            Lightmapping.BakeAsync();
        }

        private void DoClear()
        {
            Lightmapping.ClearLightingDataAsset();
            Lightmapping.Clear();
        }

        private void DoBakeReflectionProbes()
        {
            Lightmapping.BakeAllReflectionProbesSnapshots();
        }

        internal static void Summary()
        {
            bool autoGenerate = Lightmapping.GetLightingSettingsOrDefaultsFallback().autoGenerate;

            // Show the number of lightmaps:
            {
                int lightmapCount = 0;
                long totalMemorySize = 0;
                StringBuilder sizesString = new();
                var sizes = new Dictionary<LightmapSize, int>();
                if (!autoGenerate && Lightmapping.isRunning) // These are the lightmaps that will be baked or is being baked.
                {
                    RunningBakeInfo info = Lightmapping.GetRunningBakeInfo();
                    lightmapCount = info.lightmapSizes.Length;
                    var probesCount = info.probePositions;
                    string lightmapsPlural = lightmapCount != 1 ? "s" : string.Empty;
                    string probesPlural = probesCount != 1 ? "s" : string.Empty;
                    string probesString = probesCount != 0 ? $"{probesCount} probe{probesPlural} and " : string.Empty;
                    sizesString.Append($"Baking {probesString}{lightmapCount} lightmap{lightmapsPlural}");

                    foreach (var ld in info.lightmapSizes)
                        if (sizes.ContainsKey(ld))
                            sizes[ld]++;
                        else
                            sizes.Add(ld, 1);
                }
                else // These are the lightmaps that were baked last.
                {
                    bool shadowmaskMode = false;
                    foreach (LightmapData ld in LightmapSettings.lightmaps)
                    {
                        if (ld.lightmapColor == null)
                            continue;
                        lightmapCount++;

                        LightmapSize ls = new() { width = ld.lightmapColor.width, height = ld.lightmapColor.height};
                        if (sizes.ContainsKey(ls))
                            sizes[ls]++;
                        else
                            sizes.Add(ls, 1);

                        totalMemorySize += TextureUtil.GetStorageMemorySizeLong(ld.lightmapColor);
                        if (ld.lightmapDir)
                            totalMemorySize += TextureUtil.GetStorageMemorySizeLong(ld.lightmapDir);

                        if (ld.shadowMask)
                        {
                            totalMemorySize += TextureUtil.GetStorageMemorySizeLong(ld.shadowMask);
                            shadowmaskMode = true;
                        }
                    }

                    sizesString.Append(lightmapCount);
                    sizesString.Append(" lightmap");
                    if (lightmapCount != 1) sizesString.Append("s");
                    if (shadowmaskMode)
                    {
                        sizesString.Append(" with Shadowmask");
                        if (lightmapCount != 1) sizesString.Append("s");
                    }
                }

                bool first = true;
                foreach (KeyValuePair<LightmapSize, int> s in sizes)
                {
                    sizesString.Append(first ? ": " : ", ");
                    first = false;
                    if (s.Value > 1)
                    {
                        sizesString.Append(s.Value);
                        sizesString.Append("x");
                    }

                    sizesString.Append(s.Key.width.ToString(CultureInfo.InvariantCulture.NumberFormat));
                    sizesString.Append("x");
                    sizesString.Append(s.Key.height.ToString(CultureInfo.InvariantCulture.NumberFormat));
                    sizesString.Append("px");
                }

                sizesString.Append(" ");

                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical();
                GUILayout.Label(sizesString.ToString(), Styles.labelStyle);
                GUILayout.EndVertical();

                if (totalMemorySize != 0)
                {
                    GUILayout.BeginVertical();
                    GUILayout.Label(EditorUtility.FormatBytes(totalMemorySize), Styles.labelStyle);
                    GUILayout.Label((lightmapCount == 0 ? "No Lightmaps" : ""), Styles.labelStyle);
                    GUILayout.EndVertical();
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.BeginVertical();
            if (autoGenerate)
            {
                GUILayout.Label("Occupied Texels: " + InternalEditorUtility.CountToString(Lightmapping.occupiedTexelCount), Styles.labelStyle);
                if (Lightmapping.isRunning)
                {
                    int numLightmapsInView = 0;
                    int numConvergedLightmapsInView = 0;
                    int numNotConvergedLightmapsInView = 0;

                    int numLightmapsNotInView = 0;
                    int numConvergedLightmapsNotInView = 0;
                    int numNotConvergedLightmapsNotInView = 0;

                    int numInvalidConvergenceLightmaps = 0;
                    int numLightmaps = LightmapSettings.lightmaps.Length;
                    for (int i = 0; i < numLightmaps; ++i)
                    {
                        LightmapConvergence lc = Lightmapping.GetLightmapConvergence(i);
                        if (!lc.IsValid())
                        {
                            numInvalidConvergenceLightmaps++;
                            continue;
                        }

                        if (Lightmapping.GetVisibleTexelCount(i) > 0)
                        {
                            numLightmapsInView++;
                            if (lc.IsConverged())
                                numConvergedLightmapsInView++;
                            else
                                numNotConvergedLightmapsInView++;
                        }
                        else
                        {
                            numLightmapsNotInView++;
                            if (lc.IsConverged())
                                numConvergedLightmapsNotInView++;
                            else
                                numNotConvergedLightmapsNotInView++;
                        }
                    }
                    if (Lightmapping.atlasCount > 0)
                    {
                        int convergedMaps = numConvergedLightmapsInView + numConvergedLightmapsNotInView;
                        GUILayout.Label("Lightmap convergence: (" + convergedMaps + "/" + Lightmapping.atlasCount + ")", Styles.labelStyle);
                    }
                    EditorGUILayout.LabelField("Lightmaps in view: " + numLightmapsInView, Styles.labelStyle);
                    EditorGUI.indentLevel += 1;
                    EditorGUILayout.LabelField("Converged: " + numConvergedLightmapsInView, Styles.labelStyle);
                    EditorGUILayout.LabelField("Not Converged: " + numNotConvergedLightmapsInView, Styles.labelStyle);
                    EditorGUI.indentLevel -= 1;
                    EditorGUILayout.LabelField("Lightmaps not in view: " + numLightmapsNotInView, Styles.labelStyle);
                    EditorGUI.indentLevel += 1;
                    EditorGUILayout.LabelField("Converged: " + numConvergedLightmapsNotInView, Styles.labelStyle);
                    EditorGUILayout.LabelField("Not Converged: " + numNotConvergedLightmapsNotInView, Styles.labelStyle);
                    EditorGUI.indentLevel -= 1;

                    LightProbesConvergence lpc = Lightmapping.GetLightProbesConvergence();
                    if (lpc.IsValid() && lpc.probeSetCount > 0)
                        GUILayout.Label("Light Probes convergence: (" + lpc.convergedProbeSetCount + "/" + lpc.probeSetCount + ")", Styles.labelStyle);
                }
                float bakeTime = Lightmapping.GetLightmapBakeTimeTotal();
            }

            // We show baking device and performance even when not baking, so the user can see the information after a long bake:
            {
                string deviceName = Lightmapping.GetLightmapBakeGPUDeviceName();
                if (deviceName.Length > 0)
                    GUILayout.Label("Baking device: " + deviceName, Styles.labelStyle);

                float mraysPerSec = Lightmapping.GetLightmapBakePerformanceTotal();
                {
                    string text;
                    if (mraysPerSec >= 0.0)
                        text = @$"Bake Performance: {mraysPerSec.ToString("0.00", CultureInfo.InvariantCulture.NumberFormat)} mrays/sec";
                    else
                        text = "";
                    GUILayout.Label(text, Styles.labelStyle);
                }
            }

            if (!Lightmapping.isRunning)
            {
                float bakeTime = Lightmapping.GetLightmapBakeTimeTotal();
                if (bakeTime >= 0.0)
                {
                    int time = (int)bakeTime;
                    int timeH = time / 3600;
                    time -= 3600 * timeH;
                    int timeM = time / 60;
                    time -= 60 * timeM;
                    int timeS = time;
                    int decimalPart = (int)(bakeTime % 1 * 100);

                    GUILayout.Label($"Total Bake Time: {timeH:00}:{timeM:00}:{timeS:00}.{decimalPart:00}", Styles.labelStyle);
                }
            }
            GUILayout.EndVertical();
        }

        internal static LightingWindow s_Window;
        private static readonly double s_MraysPerSecRepaintThreshold = 0.01;
        internal static bool isShown => s_Window && !s_Window.docked;

        [MenuItem("Window/Rendering/Lighting %9", false, 1)]
        internal static void CreateLightingWindow()
        {
            LightingWindow window = EditorWindow.GetWindow<LightingWindow>();
            window.minSize = new Vector2(390, 390);
            window.Show();
            s_Window = window;
        }

        internal static void DestroyLightingWindow()
        {
            s_Window.Close();
            s_Window = null;
        }
    }
} // namespace
