// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    internal class SceneHierarchySortingWindow : EditorWindow
    {
        public delegate void OnSelectCallback(InputData element);

        private class Styles
        {
            public GUIStyle background = "grey_border";
            public GUIStyle menuItem = "MenuItem";
        }

        public class InputData
        {
            public string m_TypeName;
            public string m_Name;
            public bool m_Selected;
        }

        private static SceneHierarchySortingWindow s_SceneHierarchySortingWindow;
        private static long s_LastClosedTime;
        private static Styles s_Styles;

        private List<InputData> m_Data;
        private OnSelectCallback m_Callback;

        const float kFrameWidth = 1f;

        private float GetHeight()
        {
            return EditorGUI.kSingleLineHeight * m_Data.Count;
        }

        private float GetWidth()
        {
            float width = 0f;

            foreach (InputData item in m_Data)
            {
                float itemWidth = 0;
                itemWidth = s_Styles.menuItem.CalcSize(GUIContent.Temp(item.m_Name)).x;

                if (itemWidth > width)
                    width = itemWidth;
            }
            return width;
        }

        private void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Close;
            hideFlags = HideFlags.DontSave;
            wantsMouseMove = true;
        }

        private void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= Close;
            s_LastClosedTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
        }

        internal static bool ShowAtPosition(Vector2 pos, List<InputData> data, OnSelectCallback callback)
        {
            // We could not use realtimeSinceStartUp since it is set to 0 when entering/exitting playmode, we assume an increasing time when comparing time.
            long nowMilliSeconds = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            bool justClosed = nowMilliSeconds < s_LastClosedTime + 50;
            if (!justClosed)
            {
                Event.current.Use();
                if (s_SceneHierarchySortingWindow == null)
                    s_SceneHierarchySortingWindow = CreateInstance<SceneHierarchySortingWindow>();
                s_SceneHierarchySortingWindow.Init(pos, data, callback);
                return true;
            }
            return false;
        }

        private void Init(Vector2 pos, List<InputData> data, OnSelectCallback callback)
        {
            // Has to be done before calling Show / ShowWithMode
            //pos = GUIUtility.GUIToScreenPoint(pos);


            Rect buttonRect = new Rect(pos.x, pos.y - 16, 16, 16); // fake a button: we know we are showing it below the bottonRect if possible
            buttonRect = GUIUtility.GUIToScreenRect(buttonRect);
            data.Sort(
                delegate(InputData lhs, InputData rhs)
                {
                    return lhs.m_Name.CompareTo(rhs.m_Name);
                });
            m_Data = data;
            m_Callback = callback;

            if (s_Styles == null)
                s_Styles = new Styles();

            var windowHeight = 2f * kFrameWidth + GetHeight();
            var windowWidth = 2f * kFrameWidth + GetWidth();
            var windowSize = new Vector2(windowWidth, windowHeight);

            ShowAsDropDown(buttonRect, windowSize);
        }

        internal void OnGUI()
        {
            // We do not use the layout event
            if (Event.current.type == EventType.Layout)
                return;

            if (Event.current.type == EventType.MouseMove)
                Event.current.Use();

            // Content
            Draw();

            // Background with 1 pixel border
            GUI.Label(new Rect(0, 0, position.width, position.height), GUIContent.none, s_Styles.background);
        }

        private void Draw()
        {
            var drawPos = new Rect(kFrameWidth, kFrameWidth, position.width - 2 * kFrameWidth, EditorGUI.kSingleLineHeight);

            foreach (InputData data in m_Data)
            {
                DrawListElement(drawPos, data);
                drawPos.y += EditorGUI.kSingleLineHeight;
            }
        }

        void DrawListElement(Rect rect, InputData data)
        {
            EditorGUI.BeginChangeCheck();
            GUI.Toggle(rect, data.m_Selected, EditorGUIUtility.TempContent(data.m_Name), s_Styles.menuItem);
            if (EditorGUI.EndChangeCheck())
            {
                m_Callback(data);
                Close();
            }
        }
    }
}
