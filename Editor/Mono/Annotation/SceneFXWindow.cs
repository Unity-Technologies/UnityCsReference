// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    internal class SceneFXWindow : PopupWindowContent
    {
        private class Styles
        {
            public readonly GUIStyle menuItem = "MenuItem";
        }

        private static Styles s_Styles;
        private readonly SceneView m_SceneView;

        const float kFrameWidth = 1f;

        public override Vector2 GetWindowSize()
        {
            var windowHeight = 2f * kFrameWidth + EditorGUI.kSingleLineHeight * 6;
            var windowSize = new Vector2(160, windowHeight);
            return windowSize;
        }

        public SceneFXWindow(SceneView sceneView)
        {
            m_SceneView = sceneView;
        }

        public override void OnGUI(Rect rect)
        {
            if (m_SceneView == null || m_SceneView.sceneViewState == null)
                return;

            // We do not use the layout event
            if (Event.current.type == EventType.Layout)
                return;

            if (s_Styles == null)
                s_Styles = new Styles();

            // Content
            Draw(rect);

            // Use mouse move so we get hover state correctly in the menu item rows
            if (Event.current.type == EventType.MouseMove)
                Event.current.Use();

            // Escape closes the window
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }
        }

        private void Draw(Rect rect)
        {
            if (m_SceneView == null || m_SceneView.sceneViewState == null)
                return;

            var drawPos = new Rect(kFrameWidth, kFrameWidth, rect.width - 2 * kFrameWidth, EditorGUI.kSingleLineHeight);

            var state = m_SceneView.sceneViewState;

            //
            // scene view effect options

            DrawListElement(drawPos, "Skybox", state.showSkybox, value => state.showSkybox = value);
            drawPos.y += EditorGUI.kSingleLineHeight;

            DrawListElement(drawPos, "Fog", state.showFog, value => state.showFog = value);
            drawPos.y += EditorGUI.kSingleLineHeight;

            DrawListElement(drawPos, "Flares", state.showFlares, value => state.showFlares = value);
            drawPos.y += EditorGUI.kSingleLineHeight;

            DrawListElement(drawPos, "Animated Materials", state.showMaterialUpdate, value => state.showMaterialUpdate = value);
            drawPos.y += EditorGUI.kSingleLineHeight;

            DrawListElement(drawPos, "Image Effects", state.showImageEffects, value => state.showImageEffects = value);
            drawPos.y += EditorGUI.kSingleLineHeight;

            DrawListElement(drawPos, "Particle Systems", state.showParticleSystems, value => state.showParticleSystems = value);
            drawPos.y += EditorGUI.kSingleLineHeight;
        }

        void DrawListElement(Rect rect, string toggleName, bool value, Action<bool> setValue)
        {
            EditorGUI.BeginChangeCheck();
            bool result = GUI.Toggle(rect, value, EditorGUIUtility.TempContent(toggleName), s_Styles.menuItem);
            if (EditorGUI.EndChangeCheck())
            {
                setValue(result);
                m_SceneView.Repaint();
            }
        }
    }
}
