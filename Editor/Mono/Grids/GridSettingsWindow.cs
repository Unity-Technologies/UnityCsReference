// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.Snap
{
    sealed class GridSettingsWindow : PopupWindowContent
    {
        static class Contents
        {
            public static readonly GUIContent axisHeader = EditorGUIUtility.TrTextContent("Grid Axis");
            public static readonly GUIContent axisX = EditorGUIUtility.TrTextContent("X");
            public static readonly GUIContent axisY = EditorGUIUtility.TrTextContent("Y");
            public static readonly GUIContent axisZ = EditorGUIUtility.TrTextContent("Z");

            public static readonly GUIContent settingsHeader = EditorGUIUtility.TrTextContent("Grid Settings");
            public static readonly GUIContent opacitySlider = EditorGUIUtility.TrTextContent("Opacity");

            public static readonly GUIContent reset = EditorGUIUtility.TrTextContent("Reset");
            public static readonly GUIContent editGridAndSnapSettings = EditorGUIUtility.TrTextContent("Edit Grid and Snap Settings...");
        }

        static class Styles
        {
            static bool s_Initialized;
            public static readonly GUIStyle menuItem = "MenuItem";
            public static readonly GUIStyle header = EditorStyles.boldLabel;
            public static readonly GUIStyle separator = "sv_iconselector_sep";
            public static GUIStyle settingsArea;
            public static readonly GUIStyle options = "PaneOptions";

            internal static void Init()
            {
                if (s_Initialized)
                    return;

                s_Initialized = true;

                settingsArea = new GUIStyle()
                {
                    padding = new RectOffset(k_SettingsAreaBorder, k_SettingsAreaBorder, k_SettingsAreaBorder, k_SettingsAreaBorder)
                };
            }
        }

        const float k_WindowWidth = 210;
        const int k_SettingsAreaBorder = 4;
        const float k_WindowHeight = EditorGUI.kSingleLineHeight * 6 + k_SettingsAreaBorder * 2;
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
            Styles.Init();
            GUILayout.BeginVertical(Styles.settingsArea);
            EditorGUI.BeginChangeCheck();
            DoGridAxes();
            DoSeparator();
            DoGridSettings();
            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();
            GUILayout.EndVertical();
        }

        void DoGridAxes()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Contents.axisHeader, Styles.header);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(GUIContent.none, Styles.options))
            {
                var menu = new GenericMenu();
                menu.AddItem(Contents.reset, false, Reset);
                menu.AddSeparator("");
                menu.AddItem(Contents.editGridAndSnapSettings, false, () =>
                {
                    EditorWindow.GetWindow<SnapSettingsWindow>(false, SnapSettingsWindow.k_WindowTitle, true);
                });
                menu.ShowAsContext();
            }
            GUILayout.EndHorizontal();

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

        void Reset()
        {
            m_SceneView.sceneViewGrids.Reset();
            m_SceneView.sceneViewGrids.ResetPivot(SceneViewGrid.GridRenderAxis.All);
        }
    }
}
