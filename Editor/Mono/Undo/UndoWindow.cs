// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace UnityEditor
{
    internal class UndoWindow : EditorWindow
    {
        private List<string> undos = new List<string>();
        private int undoCursorPos = -1;

        // Used for caching, this way lists won't be recreated every time
        private List<string> newUndos = new List<string>();
        int newUndoCursorPos = -1;

        private Vector2 undosScroll = Vector2.zero;

        internal static void Init()
        {
            EditorWindow wnd = GetWindow(typeof(UndoWindow));
            wnd.titleContent = EditorGUIUtility.TrTextContent("Undo");
        }

        private void Update()
        {
            Undo.GetRecords(newUndos, out newUndoCursorPos);

            if (undos.SequenceEqual(newUndos) && undoCursorPos == newUndoCursorPos)
                return;

            undos = new List<string>(newUndos);
            undoCursorPos = newUndoCursorPos;

            Repaint();
        }

        private void OnGUI()
        {
            GUILayout.Label("(Available only in Developer builds)", EditorStyles.boldLabel);
            float height = position.height - 60.0f;
            float width = position.width * 0.5f - 5.0f;
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label("Undos");
            undosScroll = GUILayout.BeginScrollView(undosScroll, EditorStyles.helpBox, new GUILayoutOption[]
            {
                GUILayout.MinHeight(height),
                GUILayout.MinWidth(width)
            });

            for (var i = 0; i < undos.Count; i++)
            {
                GUILayout.Label(string.Format(i + 1 == undoCursorPos ? "[{0}] - {1} <<<" : "[{0}] - {1}", i + 1, undos[i]));
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }
    }
}
