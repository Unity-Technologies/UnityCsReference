// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class BumpMapSettingsFixingWindow : EditorWindow
    {
        public static void ShowWindow(string[] paths)
        {
            BumpMapSettingsFixingWindow win = EditorWindow.GetWindow<BumpMapSettingsFixingWindow>(true);
            win.SetPaths(paths);
            win.ShowUtility();
        }

        class Styles
        {
            public GUIStyle selected = "OL SelectedRow";
            public GUIStyle box = "OL Box";
            public GUIStyle button = "LargeButton";
            public GUIContent overviewText = EditorGUIUtility.TextContent("A Material is using the texture as a normal map.\nThe texture must be marked as a normal map in the import settings.");
        }

        static Styles s_Styles = null;

        ListViewState m_LV = new ListViewState();
        string[] m_Paths;

        public BumpMapSettingsFixingWindow()
        {
            titleContent = new GUIContent("NormalMap settings");
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
                minSize = new Vector2(400, 300);
                position = new Rect(position.x, position.y, minSize.x, minSize.y);
            }

            GUILayout.Space(5);
            GUILayout.Label(s_Styles.overviewText);
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
                InternalEditorUtility.BumpMapSettingsFixingWindowReportResult(1);
                Close();
            }

            if (GUILayout.Button("Ignore", s_Styles.button))
            {
                InternalEditorUtility.BumpMapSettingsFixingWindowReportResult(0);
                Close();
            }
            GUILayout.Space(10);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
        }

        void OnDestroy()
        {
            InternalEditorUtility.BumpMapSettingsFixingWindowReportResult(0);
        }
    }
}
