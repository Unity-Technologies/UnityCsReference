// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    internal class GridPaletteAddPopup : EditorWindow
    {
        static class Styles
        {
            public static readonly GUIContent nameLabel = EditorGUIUtility.TextContent("Name");
            public static readonly GUIContent ok = EditorGUIUtility.TextContent("Create");
            public static readonly GUIContent cancel = EditorGUIUtility.TextContent("Cancel");
            public static readonly GUIContent header = EditorGUIUtility.TextContent("Create New Palette");
            public static readonly GUIContent gridLabel = EditorGUIUtility.TextContent("Grid");
            public static readonly GUIContent sizeLabel = EditorGUIUtility.TextContent("Cell Size");
        }

#pragma warning disable 649
        private static long s_LastClosedTime;
        private string m_Name = "New Palette";
        private static GridPaletteAddPopup s_Instance;
        private GridPaintPaletteWindow m_Owner;
        private Grid.CellLayout m_Layout;
        private GridPalette.CellSizing m_CellSizing;
        private Vector3 m_CellSize;

        void Init(Rect buttonRect, GridPaintPaletteWindow owner)
        {
            m_Owner = owner;
            m_CellSize = new Vector3(1, 1, 0);
            buttonRect = GUIUtility.GUIToScreenRect(buttonRect);
            ShowAsDropDown(buttonRect, new Vector2(312, 140), null, ShowMode.PopupMenuWithKeyboardFocus);
        }

        internal void OnGUI()
        {
            GUI.Label(new Rect(0, 0, position.width, position.height), GUIContent.none, "grey_border");
            GUILayout.Space(3);

            GUILayout.Label(Styles.header, EditorStyles.boldLabel);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            GUILayout.Label(Styles.nameLabel, GUILayout.Width(90f));
            m_Name = EditorGUILayout.TextField(m_Name);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Styles.gridLabel, GUILayout.Width(90f));
            m_Layout = (Grid.CellLayout)EditorGUILayout.EnumPopup(m_Layout);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Styles.sizeLabel, GUILayout.Width(90f));
            m_CellSizing = (GridPalette.CellSizing)EditorGUILayout.EnumPopup(m_CellSizing);
            GUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(m_CellSizing == GridPalette.CellSizing.Automatic))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(GUIContent.none, GUILayout.Width(90f));
                m_CellSize = EditorGUILayout.Vector3Field(GUIContent.none, m_CellSize);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5f);

            // Cancel, Ok
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            if (GUILayout.Button(Styles.cancel))
            {
                Close();
            }

            using (new EditorGUI.DisabledScope(String.IsNullOrEmpty(m_Name)))
            {
                if (GUILayout.Button(Styles.ok))
                {
                    GameObject go = GridPaletteUtility.CreateNewPaletteNamed(m_Name, m_Layout, m_CellSizing, m_CellSize);
                    if (go != null)
                    {
                        m_Owner.palette = go;
                        m_Owner.Repaint();
                    }
                    Close();
                    GUIUtility.ExitGUI();
                }
            }

            GUILayout.Space(10);
            GUILayout.EndHorizontal();
        }

        internal static bool ShowAtPosition(Rect buttonRect, GridPaintPaletteWindow owner)
        {
            // We could not use realtimeSinceStartUp since it is set to 0 when entering/exitting playmode, we assume an increasing time when comparing time.
            long nowMilliSeconds = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            bool justClosed = nowMilliSeconds < s_LastClosedTime + 50;
            if (!justClosed)
            {
                Event.current.Use();
                if (s_Instance == null)
                    s_Instance = ScriptableObject.CreateInstance<GridPaletteAddPopup>();

                s_Instance.Init(buttonRect, owner);
                return true;
            }
            return false;
        }
    }
}

// namespace
