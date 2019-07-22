// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.Snap
{
    internal sealed class GridSettingsWindow : PopupWindowContent
    {
        static class Contents
        {
            public static readonly GUIContent axisHeader = EditorGUIUtility.TrTextContent("Grid Axis");
            public static readonly GUIContent axisX = EditorGUIUtility.TrTextContent("X");
            public static readonly GUIContent axisY = EditorGUIUtility.TrTextContent("Y");
            public static readonly GUIContent axisZ = EditorGUIUtility.TrTextContent("Z");

            public static readonly GUIContent settingsHeader = EditorGUIUtility.TrTextContent("Grid Settings");
            public static readonly GUIContent opacitySlider = EditorGUIUtility.TrTextContent("Opacity");
        }

        static class Styles
        {
            public static readonly GUIStyle menuItem = "MenuItem";
            public static readonly GUIStyle header = EditorStyles.boldLabel;
            public static readonly GUIStyle separator = "sv_iconselector_sep";
        }

        const float k_WindowWidth = 190;
        const float k_WindowHeight = 120;
        readonly SceneView m_SceneView;

        public GridSettingsWindow(SceneView sceneView)
        {
            m_SceneView = sceneView;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(k_WindowWidth, k_WindowHeight);
        }

        public override void OnGUI(Rect rect)
        {
            Draw();

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

        void Draw()
        {
            EditorGUI.BeginChangeCheck();
            DoGridAxes();
            DoSeparator();
            DoGridSettings();
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }

        void DoGridAxes()
        {
            GUILayout.Label(Contents.axisHeader, Styles.header);

            var axis = m_SceneView.sceneViewGrids.gridAxis;

            if (DrawListElement(Contents.axisX, axis == SceneViewGrid.GridRenderAxis.X))
                m_SceneView.sceneViewGrids.gridAxis = SceneViewGrid.GridRenderAxis.X;

            if (DrawListElement(Contents.axisY, axis == SceneViewGrid.GridRenderAxis.Y))
                m_SceneView.sceneViewGrids.gridAxis = SceneViewGrid.GridRenderAxis.Y;

            if (DrawListElement(Contents.axisZ, axis == SceneViewGrid.GridRenderAxis.Z))
                m_SceneView.sceneViewGrids.gridAxis = SceneViewGrid.GridRenderAxis.Z;
        }

        void DoGridSettings()
        {
            GUILayout.Label(Contents.settingsHeader, Styles.header);

            EditorGUIUtility.labelWidth = EditorGUI.CalcPrefixLabelWidth(Contents.opacitySlider, EditorStyles.label);
            m_SceneView.sceneViewGrids.gridOpacity = EditorGUILayout.Slider(Contents.opacitySlider, m_SceneView.sceneViewGrids.gridOpacity, 0, 1);
            EditorGUIUtility.labelWidth = 0;
        }

        void DoSeparator()
        {
            EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            GUILayout.Label(GUIContent.none, Styles.separator);
        }

        static bool DrawListElement(GUIContent content, bool selected)
        {
            EditorGUI.BeginChangeCheck();
            GUILayout.Toggle(selected, content, Styles.menuItem);
            return EditorGUI.EndChangeCheck();
        }
    }
}
