using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

namespace UnityEditor.UIElements
{
    internal class EditorCursorManager : ICursorManager
    {
        public void SetCursor(Cursor cursor)
        {
            if (GUIView.current == null)
            {
                // Cannot set the cursor if the current view is null.
                return;
            }

            if (cursor.texture != null)
            {
                EditorGUIUtility.SetCurrentViewCursor(cursor.texture, cursor.hotspot, MouseCursor.CustomCursor);
            }
            else
            {
                var mouseCursor = (MouseCursor)cursor.defaultCursorId;
                if (mouseCursor == MouseCursor.Arrow)
                {
                    // If it's the default cursor reset the cursor state
                    // so that editor cursor rects can be processed
                    EditorGUIUtility.ClearCurrentViewCursor();
                }
                else
                {
                    EditorGUIUtility.SetCurrentViewCursor(null, Vector2.zero, mouseCursor);
                }
            }
        }

        public void ResetCursor()
        {
            if (GUIView.current == null)
            {
                // Cannot clear the cursor if the current view is null.
                return;
            }
            EditorGUIUtility.ClearCurrentViewCursor();
        }
    }
}
