// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;
using UnityEngineInternal;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Lighting", icon = "Lighting")]
    internal class LightingWindow : EditorWindow
    {
        public const float kButtonWidth = 150;

        enum Mode
        {
            LightingSettings,
            OutputMaps,
            ObjectSettings,
        }

        enum BakeMode
        {
            BakeReflectionProbes = 0,
            Clear = 1
        }

        static string[] s_BakeModeOptions =
        {
            "Bake Reflection Probes",
            "Clear Baked Data"
        };

        const string kGlobalIlluminationUnityManualPage = "file:///unity/Manual/GlobalIllumination.html";

        Mode m_Mode = Mode.LightingSettings;
        Vector2 m_ScrollPositionLighting = Vector2.zero;
        Vector2 m_ScrollPositionOutputMaps = Vector2.zero;

        LightingWindowObjectTab             m_ObjectTab;
        public LightingWindowLightingTab    m_LightingTab;
        LightingWindowLightmapPreviewTab    m_LightmapPreviewTab;

        PreviewResizer  m_PreviewResizer = new PreviewResizer();

        static bool s_IsVisible = false;
        float m_ToolbarPadding = -1;

        private float toolbarPadding
        {
            get
            {
                if (m_ToolbarPadding == -1)
                {
                    var iconsSize = EditorStyles.iconButton.CalcSize(EditorGUI.GUIContents.helpIcon);
                    m_ToolbarPadding = (iconsSize.x * 2) + (EditorGUI.kControlVerticalSpacing * 3);
                }
                return m_ToolbarPadding;
            }
        }

        void OnEnable()
        {
            titleContent = GetLocalizedTitleContent();

            m_LightingTab = new LightingWindowLightingTab();
            m_LightingTab.OnEnable();
            m_LightmapPreviewTab = new LightingWindowLightmapPreviewTab();
            m_ObjectTab = new LightingWindowObjectTab();
            m_ObjectTab.OnEnable(this);

            autoRepaintOnSceneChange = false;
            m_PreviewResizer.Init("LightmappingPreview");
            EditorApplication.searchChanged += Repaint;
            Repaint();
        }

        void OnDisable()
        {
            m_LightingTab.OnDisable();
            m_ObjectTab.OnDisable();
            EditorApplication.searchChanged -= Repaint;
        }

        void OnBecameVisible()
        {
            if (s_IsVisible == true) return;
            s_IsVisible = true;
            RepaintSceneAndGameViews();
        }

        void OnBecameInvisible()
        {
            s_IsVisible = false;
            RepaintSceneAndGameViews();
        }

        void OnSelectionChange()
        {
            m_LightmapPreviewTab.UpdateLightmapSelection();

            Repaint();
        }

        static internal void RepaintSceneAndGameViews()
        {
            SceneView.RepaintAll();
            GameView.RepaintAll();
        }

        void OnGUI()
        {
            LightModeUtil.Get().Load();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(toolbarPadding);
            ModeToggle();
            DrawHelpGUI();
            if (m_Mode == Mode.LightingSettings)
                DrawSettingsGUI();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            switch (m_Mode)
            {
                case Mode.LightingSettings:
                    m_ScrollPositionLighting = EditorGUILayout.BeginScrollView(m_ScrollPositionLighting);
                    m_LightingTab.OnGUI();
                    EditorGUILayout.EndScrollView();
                    EditorGUILayout.Space();
                    break;
                case Mode.OutputMaps:
                    m_ScrollPositionOutputMaps = EditorGUILayout.BeginScrollView(m_ScrollPositionOutputMaps);
                    m_LightmapPreviewTab.Maps();
                    EditorGUILayout.EndScrollView();
                    EditorGUILayout.Space();
                    break;

                case Mode.ObjectSettings:
                    break;
            }

            Buttons();
            Summary();
            PreviewSection();

            if (LightModeUtil.Get().Flush())
                InspectorWindow.RepaintAllInspectors();
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

        void DrawSettingsGUI()
        {
            var iconSize = EditorStyles.iconButton.CalcSize(EditorGUI.GUIContents.titleSettingsIcon);
            var rect = GUILayoutUtility.GetRect(iconSize.x, iconSize.y);
            if (EditorGUI.DropdownButton(rect, EditorGUI.GUIContents.titleSettingsIcon, FocusType.Passive, EditorStyles.iconButton))
            {
                //@TODO: Split...
                EditorUtility.DisplayCustomMenu(rect, new[] { new GUIContent("Reset") }, -1, ResetSettings, null);
            }
        }

        void ResetSettings(object userData, string[] options, int selected)
        {
            RenderSettings.Reset();
            LightmapEditorSettings.Reset();
            LightmapSettings.Reset();
        }

        void PreviewSection()
        {
            if (m_Mode == Mode.OutputMaps)
            {
                EditorGUILayout.BeginHorizontal(GUIContent.none, Styles.ToolbarStyle, GUILayout.Height(17));
                {
                    GUILayout.FlexibleSpace();
                    GUI.Label(GUILayoutUtility.GetLastRect(), "Preview", Styles.ToolbarTitleStyle);
                }
                EditorGUILayout.EndHorizontal();
            }

            switch (m_Mode)
            {
                case Mode.OutputMaps:
                {
                    float previewSize = m_PreviewResizer.ResizeHandle(position, 100, 250, 17);
                    Rect previewRect = new Rect(0, position.height - previewSize, position.width, previewSize);

                    if (previewSize > 0)
                        m_LightmapPreviewTab.LightmapPreview(previewRect);
                } break;
                case Mode.ObjectSettings:
                {
                    //@TODO use gui layout to display ObjectPreview or deduct rect from previous window.
                    int height = LightmapEditorSettings.lightmapper == LightmapEditorSettings.Lightmapper.ProgressiveCPU ? 185 : 115;

                    Rect fullRect = new Rect(0, height, position.width, position.height - height);
                    if (Selection.activeGameObject)
                        m_ObjectTab.ObjectPreview(fullRect);
                } break;
            }
        }

        void ModeToggle()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            m_Mode = (Mode)GUILayout.Toolbar((int)m_Mode, Styles.ModeToggles, Styles.ButtonStyle, GUI.ToolbarButtonSize.FitToContents);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
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

        void Buttons()
        {
            bool wasGUIEnabled = GUI.enabled;
            GUI.enabled &= !EditorApplication.isPlayingOrWillChangePlaymode;

            bool isContinuous = LightModeUtil.Get().IsWorkflowAuto();

            if (Lightmapping.lightingDataAsset && !Lightmapping.lightingDataAsset.isValid)
            {
                EditorGUILayout.HelpBox(Lightmapping.lightingDataAsset.validityErrorMessage, MessageType.Warning);
            }

            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // Continous mode checkbox
            EditorGUI.BeginChangeCheck();
            isContinuous = GUILayout.Toggle(isContinuous, Styles.ContinuousBakeLabel);
            if (EditorGUI.EndChangeCheck())
            {
                LightModeUtil.Get().SetWorkflow(isContinuous);
            }

            using (new EditorGUI.DisabledScope(isContinuous))
            {
                // Bake button if we are not currently baking
                bool showBakeButton = isContinuous || !Lightmapping.isRunning;
                if (showBakeButton)
                {
                    if (EditorGUI.ButtonWithDropdownList(Styles.BuildLabel, s_BakeModeOptions, BakeDropDownCallback, GUILayout.Width(170)))
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
                    // Only show Force Stop when using the PathTracer backend
                    if (LightmapEditorSettings.lightmapper == LightmapEditorSettings.Lightmapper.ProgressiveCPU &&
                        LightModeUtil.Get().AreBakedLightmapsEnabled() &&
                        GUILayout.Button("Force Stop", GUILayout.Width(kButtonWidth)))
                    {
                        Lightmapping.ForceStop();
                    }
                    if (GUILayout.Button("Cancel", GUILayout.Width(kButtonWidth)))
                    {
                        Lightmapping.Cancel();
                        UsabilityAnalytics.Track("/LightMapper/Cancel");
                    }
                }
            }

            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
            GUI.enabled = wasGUIEnabled;
        }

        private void DoBake()
        {
            UsabilityAnalytics.Track("/LightMapper/Start");
            UsabilityAnalytics.Event("LightMapper", "Mode", LightmapSettings.lightmapsMode.ToString(), 1);

            UsabilityAnalytics.Event("LightMapper", "Button", "BakeScene", 1);
            Lightmapping.BakeAsync();
        }

        private void DoClear()
        {
            Lightmapping.ClearLightingDataAsset();
            Lightmapping.Clear();
            UsabilityAnalytics.Track("/LightMapper/Clear");
        }

        private void DoBakeReflectionProbes()
        {
            Lightmapping.BakeAllReflectionProbesSnapshots();
            UsabilityAnalytics.Track("/LightMapper/BakeAllReflectionProbesSnapshots");
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
                sizesString.Append(s.Key.x);
                sizesString.Append("x");
                sizesString.Append(s.Key.y);
                sizesString.Append("px");
            }
            sizesString.Append(" ");

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label(sizesString.ToString(), Styles.LabelStyle);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label(EditorUtility.FormatBytes(totalMemorySize), Styles.LabelStyle);
            GUILayout.Label((lightmapCount == 0 ? "No Lightmaps" : ""), Styles.LabelStyle);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            if (LightmapEditorSettings.lightmapper == LightmapEditorSettings.Lightmapper.ProgressiveCPU)
            {
                GUILayout.BeginVertical();
                GUILayout.Label("Occupied Texels: " + InternalEditorUtility.CountToString(Lightmapping.occupiedTexelCount), Styles.LabelStyle);
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
                    EditorGUILayout.LabelField("Lightmaps in view: " + numLightmapsInView, Styles.LabelStyle);
                    EditorGUI.indentLevel += 1;
                    EditorGUILayout.LabelField("Converged: " + numConvergedLightmapsInView, Styles.LabelStyle);
                    EditorGUILayout.LabelField("Not Converged: " + numNotConvergedLightmapsInView, Styles.LabelStyle);
                    EditorGUI.indentLevel -= 1;
                    EditorGUILayout.LabelField("Lightmaps not in view: " + numLightmapsNotInView, Styles.LabelStyle);
                    EditorGUI.indentLevel += 1;
                    EditorGUILayout.LabelField("Converged: " + numConvergedLightmapsNotInView, Styles.LabelStyle);
                    EditorGUILayout.LabelField("Not Converged: " + numNotConvergedLightmapsNotInView, Styles.LabelStyle);
                    EditorGUI.indentLevel -= 1;
                }
                float bakeTime = Lightmapping.GetLightmapBakeTimeTotal();
                float mraysPerSec = Lightmapping.GetLightmapBakePerformanceTotal();
                if (mraysPerSec >= 0.0)
                    GUILayout.Label("Bake Performance: " + mraysPerSec.ToString("0.00") + " mrays/sec", Styles.LabelStyle);
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


                        GUILayout.Label("Total Bake Time: " + timeH.ToString("0") + ":" + timeM.ToString("00") + ":" + timeS.ToString("00"), Styles.LabelStyle);
                        if (Unsupported.IsDeveloperBuild())
                            GUILayout.Label("(Raw Bake Time: " + timeRawH.ToString("0") + ":" + timeRawM.ToString("00") + ":" + timeRawS.ToString("00") + ", Overhead: " + oHeadTimeH.ToString("0") + ":" + oHeadTimeM.ToString("00") + ":" + oHeadTimeS.ToString("00") + ")", Styles.LabelStyle);
                    }
                }
                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();
        }

        [MenuItem("Window/Lighting/Settings", false, 2098)]
        static void CreateLightingWindow()
        {
            LightingWindow window = EditorWindow.GetWindow<LightingWindow>();
            window.minSize = new Vector2(360, 390);
            window.Show();
        }

        static class Styles
        {
            public static readonly GUIContent[] ModeToggles =
            {
                EditorGUIUtility.TextContent("Scene"),
                EditorGUIUtility.TextContent("Global Maps"),
                EditorGUIUtility.TextContent("Object Maps")
            };

            public static readonly GUIContent ContinuousBakeLabel = EditorGUIUtility.TextContent("Auto Generate|Automatically generates lighting data in the Scene when any changes are made to the lighting systems.");
            public static readonly GUIContent BuildLabel = EditorGUIUtility.TextContent("Generate Lighting|Generates the lightmap data for the current master scene.  This lightmap data (for realtime and baked global illumination) is stored in the GI Cache. For GI Cache settings see the Preferences panel.");

            public static readonly GUIStyle LabelStyle = EditorStyles.wordWrappedMiniLabel;
            public static readonly GUIStyle ToolbarStyle = "preToolbar";
            public static readonly GUIStyle ToolbarTitleStyle = "preToolbar";
            public static readonly GUIStyle ButtonStyle = "LargeButton";
        }
    }
} // namespace
