// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;
using UnityEditor.Scripting;

namespace UnityEditor
{
    internal class PragmaFixingWindow : EditorWindow
    {
        public static void ShowWindow(string[] paths)
        {
            PragmaFixingWindow win = EditorWindow.GetWindow<PragmaFixingWindow>(true);
            win.SetPaths(paths);
            win.ShowModal();
        }

        class Styles
        {
            public GUIStyle selected = "OL SelectedRow";
            public GUIStyle box = "OL Box";
            public GUIStyle button = "LargeButton";
        }

        static Styles s_Styles = null;

        ListViewState m_LV = new ListViewState();
        string[] m_Paths;

        public PragmaFixingWindow()
        {
            titleContent = new GUIContent("Unity - #pragma fixing");
        }

        public void SetPaths(string[] paths)
        {
            m_Paths = paths;
            m_LV.totalRows = paths.Length;
        }

        void OnGUI()
        {
            if (s_Styles == null)
            {
                s_Styles = new Styles();
                minSize = new Vector2(450, 300);
                position = new Rect(position.x, position.y, minSize.x, minSize.y);
            }

            GUILayout.Space(10);
            GUILayout.Label("#pragma implicit and #pragma downcast need to be added to following files\nfor backwards compatibility");
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            foreach (ListViewElement el in ListViewGUILayout.ListView(m_LV, s_Styles.box))
            {
                if (el.row == m_LV.row && Event.current.type == EventType.Repaint)
                    s_Styles.selected.Draw(el.position, false, false, false, false);

                GUILayout.Label(m_Paths[el.row]);
            }
            GUILayout.Space(10);
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Fix now", s_Styles.button))
            {
                Close();
                PragmaFixing30.FixFiles(m_Paths);
                // bugfix (377429): do not call AssetDatabase.Refresh here as that screws up project upgrading.
                // When this script is invoked from Application::InitializeProject, the assets will be refreshed anyway.
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("Ignore", s_Styles.button))
            {
                Close();
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("Quit", s_Styles.button))
            {
                EditorApplication.Exit(0);
                GUIUtility.ExitGUI();
            }

            GUILayout.Space(10);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
        }
    }
}
