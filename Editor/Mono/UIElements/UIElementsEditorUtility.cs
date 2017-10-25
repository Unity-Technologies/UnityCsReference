// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.StyleSheets;
using StyleSheet = UnityEngine.StyleSheets.StyleSheet;

namespace UnityEditor.Experimental.UIElements
{
    public static class UIElementsEditorUtility
    {
        public static CursorStyle CreateDefaultCursorStyle(MouseCursor mouseCursor)
        {
            return new CursorStyle() { texture = null, hotspot = Vector2.zero, defaultCursorId = (int)mouseCursor };
        }

        internal static CursorStyle CreateDefaultCursorStyle(StyleSheet sheet, StyleValueHandle handle)
        {
            int type = StyleSheetCache.GetEnumValue<MouseCursor>(sheet, handle);
            CursorStyle cursor = new CursorStyle() { texture = null, hotspot = Vector2.zero, defaultCursorId = type };
            return cursor;
        }
    }
}
