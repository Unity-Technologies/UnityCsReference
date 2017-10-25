// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    internal class EditorCursorManager : ICursorManager
    {
        public void SetCursor(CursorStyle cursor)
        {
            if (cursor.texture != null)
            {
                EditorGUIUtility.SetCurrentViewCursor(cursor.texture, cursor.hotspot, MouseCursor.CustomCursor);
            }
            else
            {
                EditorGUIUtility.SetCurrentViewCursor(null, Vector2.zero, (MouseCursor)cursor.defaultCursorId);
            }
        }

        public void ResetCursor()
        {
            EditorGUIUtility.ClearCurrentViewCursor();
        }
    }
}
