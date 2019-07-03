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
                EditorGUIUtility.TrTextContent("Realtime Lightmaps"),
                EditorGUIUtility.TrTextContent("Baked Lightmaps")
            };

            public static readonly GUIStyle buttonStyle = "LargeButton";
        }

        enum Mode
        {
            LightingSettings = 0,
            RealtimeLightmaps,
            BakedLightmaps
        }

        const string kGlobalIlluminationUnityManualPage = "file:///unity/Manual/GlobalIllumination.html";

        int m_SelectedModeIndex = 0;
        List<Mode> m_Modes = null;
        GUIContent[] m_ModeStrings;

        LightingWindowLightingTab           m_LightingSettingsTab;
        LightingWindowLightmapPreviewTab    m_RealtimeLightmapsTab;
        LightingWindowLightmapPreviewTab    m_BakedLightmapsTab;

        bool m_IsRealtimeSupported = false;
        bool m_IsBakedSupported = false;

        void OnEnable()
        {
            titleContent = GetLocalizedTitleContent();

            m_LightingSettingsTab = new LightingWindowLightingTab();
            m_LightingSettingsTab.OnEnable();
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
            PreviewEditorWindow.RepaintAll();
        }

        void OnGUI()
        {
            // This is done so that we can adjust the UI when the user swiches SRP
            SetupModes();

            // reset index to settings page if one of the tabs went away
            if (m_SelectedModeIndex >= m_Modes.Count)
                m_SelectedModeIndex = 0;

            Mode selectedMode = m_Modes[m_SelectedModeIndex];

            DrawTopBarGUI(selectedMode);

            switch (selectedMode)
            {
                case Mode.LightingSettings:
                    m_LightingSettingsTab.OnGUI();
                    break;

                case Mode.RealtimeLightmaps:
                    m_RealtimeLightmapsTab.OnGUI(position);
                    break;

                case Mode.BakedLightmaps:
                    m_BakedLightmapsTab.OnGUI(position);
                    break;
            }
        }

        void SetupModes()
        {
            if (m_Modes == null)
            {
                m_Modes = new List<Mode>();
            }

            bool isRealtimeSupported = SupportedRenderingFeatures.IsLightmapBakeTypeSupported(LightmapBakeType.Realtime);
            bool isBakedSupported = SupportedRenderingFeatures.IsLightmapBakeTypeSupported(LightmapBakeType.Baked);

            if (m_IsRealtimeSupported != isRealtimeSupported || m_IsBakedSupported != isBakedSupported)
            {
                m_Modes.Clear();

                m_IsBakedSupported = isBakedSupported;
                m_IsRealtimeSupported = isRealtimeSupported;
            }

            // if nothing has changed since last time and we have data, we return
            if (m_Modes.Count > 0)
                return;

            List<GUIContent> modeStringList = new List<GUIContent>();

            m_Modes.Add(Mode.LightingSettings);
            modeStringList.Add(Styles.modeStrings[(int)Mode.LightingSettings]);

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

        void DrawSettingsGUI()
        {
            var iconSize = EditorStyles.iconButton.CalcSize(EditorGUI.GUIContents.titleSettingsIcon);
            var rect = GUILayoutUtility.GetRect(iconSize.x, iconSize.y);

            if (EditorGUI.DropdownButton(rect, EditorGUI.GUIContents.titleSettingsIcon, FocusType.Passive, EditorStyles.iconButton))
            {
                EditorUtility.DisplayCustomMenu(rect, new[] { EditorGUIUtility.TrTextContent("Reset") }, -1, ResetSettings, null);
            }
        }

        void ResetSettings(object userData, string[] options, int selected)
        {
            Undo.RecordObjects(new[] {RenderSettings.GetRenderSettings(), LightmapEditorSettings.GetLightmapSettings()}, "Reset Lighting Settings");
            Unsupported.SmartReset(RenderSettings.GetRenderSettings());
            Unsupported.SmartReset(LightmapEditorSettings.GetLightmapSettings());
        }

        void DrawTopBarGUI(Mode selectedMode)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            if (selectedMode == Mode.LightingSettings)
                GUILayout.Space(EditorStyles.iconButton.CalcSize(EditorGUI.GUIContents.helpIcon).x);

            GUILayout.FlexibleSpace();

            if (m_Modes.Count > 1)
            {
                m_SelectedModeIndex = GUILayout.Toolbar(m_SelectedModeIndex, m_ModeStrings, Styles.buttonStyle, GUI.ToolbarButtonSize.FitToContents);
            }

            GUILayout.FlexibleSpace();

            DrawHelpGUI();

            if (selectedMode == Mode.LightingSettings)
                DrawSettingsGUI();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        [MenuItem("Window/Rendering/Lighting Settings", false, 1)]
        internal static void CreateLightingWindow()
        {
            LightingWindow window = EditorWindow.GetWindow<LightingWindow>();
            window.minSize = new Vector2(390, 390);
            window.Show();
        }
    }
} // namespace
