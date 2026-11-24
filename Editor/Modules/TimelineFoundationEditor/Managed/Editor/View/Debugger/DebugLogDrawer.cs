// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Unity.Timeline.Foundation.View.Debugger
{
    class DebugLogDrawer
    {
        static readonly GUILayoutOption k_StackViewHeight = GUILayout.Height(300);

        Vector2 m_Pos;
        int m_OpenedLog = -1;
        bool m_Reverse;

        public DebugLogDrawer(bool reverse = false)
        {
            m_Reverse = reverse;
        }

        public void DrawLog(IEnumerable<EventLog> logs)
        {
            m_Pos = GUILayout.BeginScrollView(m_Pos);
            if (m_Reverse)
                DrawEventLogReverse(logs, ref m_OpenedLog);
            else
            {
                DrawEventLog(logs, ref m_OpenedLog);
            }
            GUILayout.EndScrollView();
        }

        static void DrawEventLog(IEnumerable<EventLog> logs, ref int currentFoldout)
        {
            var count = 0;
            foreach (EventLog evt in logs)
            {
                bool isOpened = currentFoldout == count;

                bool show = EditorGUILayout.BeginFoldoutHeaderGroup(isOpened, $"{count + 1}- {evt}");
                {
                    if (show)
                    {
                        currentFoldout = count;
                        EditorGUILayout.TextField(evt.stack, k_StackViewHeight);
                    }
                    else if (isOpened)
                        currentFoldout = -1;
                }
                EditorGUI.EndFoldoutHeaderGroup();
                ++count;
            }
        }

        static void DrawEventLogReverse(IEnumerable<EventLog> logs, ref int currentFoldout)
        {
            var logsList = new List<EventLog>(logs);
            var count = logsList.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                var evt = logsList[i];
                bool isOpened = currentFoldout == i;

                bool show = EditorGUILayout.BeginFoldoutHeaderGroup(isOpened, $"{i}- {evt}");
                {
                    if (show)
                    {
                        currentFoldout = count;
                        EditorGUILayout.TextField(evt.stack, k_StackViewHeight);
                    }
                    else if (isOpened)
                        currentFoldout = -1;
                }
                EditorGUI.EndFoldoutHeaderGroup();
                count--;
            }
        }
    }
}
