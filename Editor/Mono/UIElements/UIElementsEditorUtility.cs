// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEditor.Experimental;
using UnityEditor.StyleSheets;
using UnityEngine;
using UnityEngine.UIElements;
using StyleSheetCache = UnityEngine.UIElements.StyleSheets.StyleSheetCache;

namespace UnityEditor.UIElements
{
    // This is the required interface to UIElementsEditorUtility for Runtime game components.
    internal static class UIElementsEditorRuntimeUtility
    {
        public static void CreateRuntimePanelDebug(IPanel panel)
        {
            var panelDebug = new PanelDebug(panel);
            (panel as Panel).panelDebug = panelDebug;
        }
    }

    internal static class UIElementsEditorUtility
    {
        internal static readonly string s_DefaultCommonDarkStyleSheetPath = "StyleSheets/Generated/DefaultCommonDark.uss.asset";
        internal static readonly string s_DefaultCommonLightStyleSheetPath = "StyleSheets/Generated/DefaultCommonLight.uss.asset";

        internal static readonly StyleSheet s_DefaultCommonDarkStyleSheet;
        internal static readonly StyleSheet s_DefaultCommonLightStyleSheet;

        internal static string GetStyleSheetPathForCurrentFont(string sheetPath)
        {
            // Load the stylesheet of the current font
            if (LocalizationDatabase.currentEditorLanguage == SystemLanguage.English)
            {
                return sheetPath.Replace(".uss", "_" + EditorResources.currentFontName.ToLower() + ".uss");
            }
            else
            {
                return sheetPath;
            }
        }

        static UIElementsEditorUtility()
        {
            // Load the stylesheet of the current font
            s_DefaultCommonDarkStyleSheet = EditorGUIUtility.Load(GetStyleSheetPathForCurrentFont(s_DefaultCommonDarkStyleSheetPath)) as StyleSheet;
            s_DefaultCommonDarkStyleSheet.isUnityStyleSheet = true;

            s_DefaultCommonLightStyleSheet = EditorGUIUtility.Load(GetStyleSheetPathForCurrentFont(s_DefaultCommonLightStyleSheetPath)) as StyleSheet;
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
