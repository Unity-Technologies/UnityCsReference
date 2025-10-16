// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolsAuthoringFramework.InternalEditorBridge
{
    static class EditorGUIUtilityBridge
    {
        public static Texture2D LoadIcon(string path)
        {
            return EditorGUIUtility.LoadIcon(path);
        }

        public static void SetCursor(MouseCursor cursorId)
        {
            if (GUIView.current == null)
            {
                // Cannot set the cursor if the current view is null.
                return;
            }

            // The public API to change the cursor would be Cursor.SetCursor,
            // but it does not support cursor ids.

            if (cursorId == MouseCursor.Arrow)
            {
                // If it's the default cursor reset the cursor state
                // so that editor cursor rects can be processed
                EditorGUIUtility.ClearCurrentViewCursor();
            }
            else
            {
                EditorGUIUtility.SetCurrentViewCursor(null, Vector2.zero, cursorId);
            }
        }
    }
}
