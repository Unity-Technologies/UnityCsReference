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

        internal static readonly StyleSheet s_DefaultCommonStyleSheet;
        internal static readonly StyleSheet s_DefaultCommonDarkStyleSheet;
        internal static readonly StyleSheet s_DefaultCommonLightStyleSheet;

        static UIElementsEditorUtility()
        {
            s_DefaultCommonStyleSheet = EditorGUIUtility.Load(s_DefaultCommonStyleSheetPath) as StyleSheet;
            s_DefaultCommonStyleSheet.isUnityStyleSheet = true;

            s_DefaultCommonDarkStyleSheet = EditorGUIUtility.Load(s_DefaultCommonDarkStyleSheetPath) as StyleSheet;
            s_DefaultCommonDarkStyleSheet.isUnityStyleSheet = true;

            s_DefaultCommonLightStyleSheet = EditorGUIUtility.Load(s_DefaultCommonLightStyleSheetPath) as StyleSheet;
            s_DefaultCommonLightStyleSheet.isUnityStyleSheet = true;
        }

        internal static int GetCursorId(StyleSheet sheet, StyleValueHandle handle)
        {
            return StyleSheetCache.GetEnumValue<MouseCursor>(sheet, handle);
        }

        internal static void AddDefaultEditorStyleSheets(VisualElement ve)
        {
            if (ve.styleSheets.count == 0)
            {
                ve.styleSheets.Add(s_DefaultCommonStyleSheet);
                if (EditorGUIUtility.isProSkin)
                {
                    ve.styleSheets.Add(s_DefaultCommonDarkStyleSheet);
                }
                else
                {
                    ve.styleSheets.Add(s_DefaultCommonLightStyleSheet);
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
                    if (e.styleSheets.Contains(s_DefaultCommonLightStyleSheet))
                    {
                        e.styleSheets.Swap(s_DefaultCommonLightStyleSheet, s_DefaultCommonDarkStyleSheet);
                        break;
                    }
                    e = e.parent;
                }
            }
        }
    }
}
