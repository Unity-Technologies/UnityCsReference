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
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Lighting", icon = "Lighting")]
    internal class LightingWindow : EditorWindow
    {
        public const float kButtonWidth = 90;

        enum Mode
        {
            LightingSettings,
            RealtimeLightmaps,
            BakedLightmaps
        }

        const string kGlobalIlluminationUnityManualPage = "file:///unity/Manual/GlobalIllumination.html";
        Mode m_SelectedMode = Mode.LightingSettings;

        LightingWindowLightingTab           m_LightingSettingsTab;
        LightingWindowLightmapPreviewTab    m_RealtimeLightmapsTab;
        LightingWindowLightmapPreviewTab    m_BakedLightmapsTab;

        void OnEnable()
        {
            titleContent = GetLocalizedTitleContent();

            m_LightingSettingsTab = new LightingWindowLightingTab();
            m_LightingSettingsTab.OnEnable();
            m_RealtimeLightmapsTab = new LightingWindowLightmapPreviewTab(LightmapType.DynamicLightmap);
            m_BakedLightmapsTab = new LightingWindowLightmapPreviewTab(LightmapType.StaticLightmap);


            autoRepaintOnSceneChange = false;
            Undo.undoRedoPerformed += Repaint;
            Repaint();
        }

        void OnDisable()
        {
            m_LightingSettingsTab.OnDisable();
            Undo.undoRedoPerformed -= Repaint;
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
            if (m_RealtimeLightmapsTab == null || m_BakedLightmapsTab == null)
                return;

            m_RealtimeLightmapsTab.UpdateActiveGameObjectSelection();
            m_BakedLightmapsTab.UpdateActiveGameObjectSelection();

            Repaint();
        }

        static internal void RepaintSceneAndGameViews()
        {
            SceneView.RepaintAll();
            GameView.RepaintAll();
        }

        void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            ModeToggle();
            DrawHelpGUI();
            if (m_SelectedMode == Mode.LightingSettings)
                DrawSettingsGUI();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            switch (m_SelectedMode)
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
                EditorUtility.DisplayCustomMenu(rect, new[] { EditorGUIUtility.TrTextContent("Reset") }, -1, ResetSettings, null);
            }
        }

        void ResetSettings(object userData, string[] options, int selected)
        {
            Undo.RecordObjects(new[] {RenderSettings.GetRenderSettings(), LightmapEditorSettings.GetLightmapSettings()}, "Reset Lighting Settings");
            Unsupported.SmartReset(RenderSettings.GetRenderSettings());
            Unsupported.SmartReset(LightmapEditorSettings.GetLightmapSettings());
        }

        void ModeToggle()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();
            m_SelectedMode = (Mode)GUILayout.Toolbar((int)m_SelectedMode, Styles.ModeToggles, Styles.ButtonStyle, GUI.ToolbarButtonSize.FitToContents);
            if (EditorGUI.EndChangeCheck())
                GUIUtility.keyboardControl = 0;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        [MenuItem("Window/Rendering/Lighting Settings", false, 1)]
        static void CreateLightingWindow()
        {
            LightingWindow window = EditorWindow.GetWindow<LightingWindow>();
            window.minSize = new Vector2(390, 390);
            window.Show();
        }

        static class Styles
        {
            public static readonly GUIContent[] ModeToggles =
            {
                EditorGUIUtility.TrTextContent("Scene"),
                EditorGUIUtility.TrTextContent("Realtime Lightmaps"),
                EditorGUIUtility.TrTextContent("Baked Lightmaps")
            };

            public static readonly GUIStyle ButtonStyle = "LargeButton";
        }
    }
} // namespace
