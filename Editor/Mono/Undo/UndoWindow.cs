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
        private List<string> redos = new List<string>();

        // Used for caching, this way lists won't be recreated every time
        private List<string> newUndos = new List<string>();
        private List<string> newRedos = new List<string>();

        private Vector2 undosScroll = Vector2.zero;
        private Vector2 redosScroll = Vector2.zero;

        internal static void Init()
        {
            EditorWindow wnd = GetWindow(typeof(UndoWindow));
            wnd.titleContent =  new GUIContent("Undo");
        }

        private void Update()
        {
            Undo.GetRecords(newUndos, newRedos);
            bool equal = undos.SequenceEqual(newUndos) && redos.SequenceEqual(newRedos);

            if (equal)
                return;

            undos = new List<string>(newUndos);
            redos = new List<string>(newRedos);

            Repaint();
        }

        private void OnGUI()
        {
            GUILayout.Label("(Available only in Developer builds)", EditorStyles.boldLabel);
            float height = position.height - 60.0f;
            float width = position.width * 0.5f - 5.0f;
            int i;
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label("Undos");
            undosScroll = GUILayout.BeginScrollView(undosScroll, EditorStyles.helpBox, new GUILayoutOption[]
            {
                GUILayout.MinHeight(height),
                GUILayout.MinWidth(width)
            });

            i = 0;
            foreach (var undo in undos)
            {
                GUILayout.Label(string.Format("[{0}] - {1}", i++, undo));
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label("Redos");
            redosScroll = GUILayout.BeginScrollView(redosScroll, EditorStyles.helpBox, new GUILayoutOption[]
            {
                GUILayout.MinHeight(height),
                GUILayout.MinWidth(width)
            });

            i = 0;
            foreach (var redo in redos)
            {
                GUILayout.Label(string.Format("[{0}] - {1}", i++, redo));
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }
    }
}
