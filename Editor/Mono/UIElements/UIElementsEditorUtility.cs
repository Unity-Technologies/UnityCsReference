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
        internal static readonly string s_DefaultCommonStyleSheetPath = "StyleSheets/DefaultCommon.uss";
        internal static readonly string s_DefaultCommonDarkStyleSheetPath = "StyleSheets/DefaultCommonDark.uss";
        internal static readonly string s_DefaultCommonLightStyleSheetPath = "StyleSheets/DefaultCommonLight.uss";

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

        internal static void AddDefaultEditorStyleSheets(VisualElement p)
        {
            if (p.styleSheets == null)
            {
                p.AddStyleSheetPath(s_DefaultCommonStyleSheetPath);
                if (EditorGUIUtility.isProSkin)
                {
                    p.AddStyleSheetPath(s_DefaultCommonDarkStyleSheetPath);
                }
                else
                {
                    p.AddStyleSheetPath(s_DefaultCommonLightStyleSheetPath);
                }
            }
        }

        internal static void ForceDarkStyleSheet(VisualElement ele)
        {
            if (!EditorGUIUtility.isProSkin)
            {
                var e = ele;
                while (e != null)
                {
                    if (e.HasStyleSheetPath(s_DefaultCommonLightStyleSheetPath))
                    {
                        e.ReplaceStyleSheetPath(s_DefaultCommonLightStyleSheetPath, s_DefaultCommonDarkStyleSheetPath);
                        break;
                    }
                    e = e.parent;
                }
            }
        }
    }
}
