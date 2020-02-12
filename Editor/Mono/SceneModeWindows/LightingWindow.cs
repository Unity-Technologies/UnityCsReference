// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;
using UnityEngineInternal;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using System.Text;
using System.Globalization;

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
            public static readonly GUIContent continuousBakeLabel = EditorGUIUtility.TrTextContent("Auto Generate", "Automatically generates lighting data in the Scene when any changes are made to the lighting systems.");
            public static readonly GUIContent buildLabel = EditorGUIUtility.TrTextContent("Generate Lighting", "Generates the lightmap data for the current master scene.  This lightmap data (for realtime and baked global illumination) is stored in the GI Cache. For GI Cache settings see the Preferences panel.");

            public static string[] BakeModeStrings =
            {
                "Bake Reflection Probes",
                "Clear Baked Data"
            };

            public static readonly float ButtonWidth = 90;
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

        const string kGlobalIlluminationUnityManualPage = "file:///unity/Manual/GlobalIllumination.html";

        int m_SelectedModeIndex = 0;
        List<Mode> m_Modes = null;
        GUIContent[] m_ModeStrings;

        LightingWindowLightingTab           m_LightingSettingsTab;
        LightingWindowEnvironmentTab        m_EnvironmentSettingsTab;
        LightingWindowLightmapPreviewTab    m_RealtimeLightmapsTab;
        LightingWindowLightmapPreviewTab    m_BakedLightmapsTab;

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

        void OnEnable()
        {
            titleContent = GetLocalizedTitleContent();

            m_LightingSettingsTab = new LightingWindowLightingTab();
            m_LightingSettingsTab.OnEnable();
            m_EnvironmentSettingsTab = new LightingWindowEnvironmentTab();
            m_EnvironmentSettingsTab.OnEnable();

            m_RealtimeLightmapsTab = new LightingWindowLightmapPreviewTab(LightmapType.DynamicLightmap);
            m_BakedLightmapsTab = new LightingWindowLightmapPreviewTab(LightmapType.StaticLightmap);

            Undo.undoRedoPerformed += Repaint;
            Lightmapping.lightingDataUpdated += Repaint;

            Repaint();
        }

        void OnDisable()
        {
            m_LightingSettingsTab.OnDisable();
            Undo.undoRedoPerformed -= Repaint;
            Lightmapping.lightingDataUpdated -= Repaint;
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
            if (m_RealtimeLightmapsTab == null || m_BakedLightmapsTab == null || m_Modes == null)
                return;

            if (m_Modes.Contains(Mode.RealtimeLightmaps))
                m_RealtimeLightmapsTab.UpdateActiveGameObjectSelection();

            if (m_Modes.Contains(Mode.BakedLightmaps))
                m_BakedLightmapsTab.UpdateActiveGameObjectSelection();

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

            switch (selectedMode)
            {
                case Mode.LightingSettings:
                    m_LightingSettingsTab.OnGUI();
                    break;

                case Mode.EnvironmentSettings:
                    m_EnvironmentSettingsTab.OnGUI();
                    break;

                case Mode.RealtimeLightmaps:
                    m_RealtimeLightmapsTab.OnGUI(position);
                    break;

                case Mode.BakedLightmaps:
                    m_BakedLightmapsTab.OnGUI(position);
                    break;
            }

            Buttons(selectedMode == Mode.LightingSettings);
            Summary();

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

            Debug.Assert(m_Modes.Count == modeStringList.Count);

            m_ModeStrings = modeStringList.ToArray();
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

        void DrawTopBarGUI(Mode selectedMode)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            if (selectedMode == Mode.LightingSettings || selectedMode == Mode.EnvironmentSettings)
                GUILayout.Space(EditorStyles.iconButton.CalcSize(EditorGUI.GUIContents.helpIcon).x);

            GUILayout.FlexibleSpace();

            if (m_Modes.Count > 1)
            {
                m_SelectedModeIndex = GUILayout.Toolbar(m_SelectedModeIndex, m_ModeStrings, Styles.buttonStyle, GUI.ToolbarButtonSize.FitToContents);
            }

            GUILayout.FlexibleSpace();

            DrawHelpGUI();
            DrawSettingsGUI(selectedMode);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
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

        void Buttons(bool showAutoToggle)
        {
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (Lightmapping.lightingDataAsset && !Lightmapping.lightingDataAsset.isValid)
                {
                    EditorGUILayout.HelpBox(Lightmapping.lightingDataAsset.validityErrorMessage, MessageType.Warning);
                }

                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                bool iterative = m_WorkflowMode.intValue == (int)Lightmapping.GIWorkflowMode.Iterative;
                Rect rect = GUILayoutUtility.GetRect(Styles.continuousBakeLabel, GUIStyle.none);

                if (showAutoToggle)
                {
                    EditorGUI.BeginProperty(rect, Styles.continuousBakeLabel, m_WorkflowMode);

                    // Continous mode checkbox
                    EditorGUI.BeginChangeCheck();
                    using (new EditorGUI.DisabledScope(m_LightingSettingsReadOnlyMode))
                    {
                        iterative = GUILayout.Toggle(iterative, Styles.continuousBakeLabel);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        m_WorkflowMode.intValue = (int)(iterative ? Lightmapping.GIWorkflowMode.Iterative : Lightmapping.GIWorkflowMode.OnDemand);
                    }

                    EditorGUI.EndProperty();
                }

                using (new EditorGUI.DisabledScope(iterative))
                {
                    // Bake button if we are not currently baking
                    bool showBakeButton = iterative || !Lightmapping.isRunning;
                    if (showBakeButton)
                    {
                        if (EditorGUI.ButtonWithDropdownList(Styles.buildLabel, Styles.BakeModeStrings, BakeDropDownCallback, GUILayout.Width(170)))
                        {
                            DoBake();

                            // DoBake could've spawned a save scene dialog. This breaks GUI on mac (Case 490388).
                            // We work around this with an ExitGUI here.
                            GUIUtility.ExitGUI();
                        }
                    }
                    // Cancel button if we are currently baking
                    else
                    {
                        var settings = Lightmapping.GetLightingSettingsOrDefaultsFallback();
                        // Only show Force Stop when using the PathTracer backend
                        if (settings.lightmapper != LightingSettings.Lightmapper.Enlighten && settings.bakedGI &&
                            GUILayout.Button("Force Stop", GUILayout.Width(Styles.ButtonWidth)))
                        {
                            Lightmapping.ForceStop();
                        }
                        if (GUILayout.Button("Cancel", GUILayout.Width(Styles.ButtonWidth)))
                        {
                            Lightmapping.Cancel();
                        }
                    }
                }

                GUILayout.EndHorizontal();
                EditorGUILayout.Space();
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

        void Summary()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);

            long totalMemorySize = 0;
            int lightmapCount = 0;
            Dictionary<Vector2, int> sizes = new Dictionary<Vector2, int>();
            bool directionalLightmapsMode = false;
            bool shadowmaskMode = false;
            foreach (LightmapData ld in LightmapSettings.lightmaps)
            {
                if (ld.lightmapColor == null)
                    continue;
                lightmapCount++;

                Vector2 texSize = new Vector2(ld.lightmapColor.width, ld.lightmapColor.height);
                if (sizes.ContainsKey(texSize))
                    sizes[texSize]++;
                else
                    sizes.Add(texSize, 1);

                totalMemorySize += TextureUtil.GetStorageMemorySizeLong(ld.lightmapColor);
                if (ld.lightmapDir)
                {
                    totalMemorySize += TextureUtil.GetStorageMemorySizeLong(ld.lightmapDir);
                    directionalLightmapsMode = true;
                }
                if (ld.shadowMask)
                {
                    totalMemorySize += TextureUtil.GetStorageMemorySizeLong(ld.shadowMask);
                    shadowmaskMode = true;
                }
            }
            StringBuilder sizesString = new StringBuilder();
            sizesString.Append(lightmapCount);
            sizesString.Append((directionalLightmapsMode ? " Directional" : " Non-Directional"));
            sizesString.Append(" Lightmap");
            if (lightmapCount != 1) sizesString.Append("s");
            if (shadowmaskMode)
            {
                sizesString.Append(" with Shadowmask");
                if (lightmapCount != 1) sizesString.Append("s");
            }

            bool first = true;
            foreach (var s in sizes)
            {
                sizesString.Append(first ? ": " : ", ");
                first = false;
                if (s.Value > 1)
                {
                    sizesString.Append(s.Value);
                    sizesString.Append("x");
                }
                sizesString.Append(s.Key.x.ToString(CultureInfo.InvariantCulture.NumberFormat));
                sizesString.Append("x");
                sizesString.Append(s.Key.y.ToString(CultureInfo.InvariantCulture.NumberFormat));
                sizesString.Append("px");
            }
            sizesString.Append(" ");

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label(sizesString.ToString(), Styles.labelStyle);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label(EditorUtility.FormatBytes(totalMemorySize), Styles.labelStyle);
            GUILayout.Label((lightmapCount == 0 ? "No Lightmaps" : ""), Styles.labelStyle);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            if (Lightmapping.GetLightingSettingsOrDefaultsFallback().lightmapper != LightingSettings.Lightmapper.Enlighten)
            {
                GUILayout.BeginVertical();
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
                float mraysPerSec = Lightmapping.GetLightmapBakePerformanceTotal();
                if (mraysPerSec >= 0.0)
                    GUILayout.Label("Bake Performance: " + mraysPerSec.ToString("0.00", CultureInfo.InvariantCulture.NumberFormat) + " mrays/sec", Styles.labelStyle);
                if (!Lightmapping.isRunning)
                {
                    float bakeTimeRaw = Lightmapping.GetLightmapBakeTimeRaw();
                    if (bakeTime >= 0.0)
                    {
                        int time = (int)bakeTime;
                        int timeH = time / 3600;
                        time -= 3600 * timeH;
                        int timeM = time / 60;
                        time -= 60 * timeM;
                        int timeS = time;

                        int timeRaw = (int)bakeTimeRaw;
                        int timeRawH = timeRaw / 3600;
                        timeRaw -= 3600 * timeRawH;
                        int timeRawM = timeRaw / 60;
                        timeRaw -= 60 * timeRawM;
                        int timeRawS = timeRaw;

                        int oHeadTime = Math.Max(0, (int)(bakeTime - bakeTimeRaw));
                        int oHeadTimeH = oHeadTime / 3600;
                        oHeadTime -= 3600 * oHeadTimeH;
                        int oHeadTimeM = oHeadTime / 60;
                        oHeadTime -= 60 * oHeadTimeM;
                        int oHeadTimeS = oHeadTime;


                        GUILayout.Label("Total Bake Time: " + timeH.ToString("0") + ":" + timeM.ToString("00") + ":" + timeS.ToString("00"), Styles.labelStyle);
                        if (Unsupported.IsDeveloperBuild())
                            GUILayout.Label("(Raw Bake Time: " + timeRawH.ToString("0") + ":" + timeRawM.ToString("00") + ":" + timeRawS.ToString("00") + ", Overhead: " + oHeadTimeH.ToString("0") + ":" + oHeadTimeM.ToString("00") + ":" + oHeadTimeS.ToString("00") + ")", Styles.labelStyle);
                    }
                }
                string deviceName = Lightmapping.GetLightmapBakeGPUDeviceName();
                if (deviceName.Length > 0)
                    GUILayout.Label("Baking device: " + deviceName, Styles.labelStyle);
                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();
        }

        [MenuItem("Window/Rendering/Lighting", false, 1)]
        internal static void CreateLightingWindow()
        {
            LightingWindow window = EditorWindow.GetWindow<LightingWindow>();
            window.minSize = new Vector2(390, 390);
            window.Show();
        }
    }
} // namespace
