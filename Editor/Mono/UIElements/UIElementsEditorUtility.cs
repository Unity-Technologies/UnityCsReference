// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEditor.UIElements
{
    internal static class UIElementsEditorUtility
    {
        internal static readonly string s_DefaultCommonStyleSheetPath = "StyleSheets/DefaultCommon.uss";
        internal static readonly string s_DefaultCommonDarkStyleSheetPath = "StyleSheets/DefaultCommonDark.uss";
        internal static readonly string s_DefaultCommonLightStyleSheetPath = "StyleSheets/DefaultCommonLight.uss";

        internal static int GetCursorId(StyleSheet sheet, StyleValueHandle handle)
        {
            return StyleSheetCache.GetEnumValue<MouseCursor>(sheet, handle);
        }

        internal static void AddDefaultEditorStyleSheets(VisualElement p)
        {
            if (p.styleSheets.count == 0)
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
