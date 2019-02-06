// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.StyleSheets;
using UnityEngine;
using UnityEngine.UIElements;
using StyleSheetCache = UnityEngine.UIElements.StyleSheets.StyleSheetCache;

namespace UnityEditor.UIElements
{
    internal static class UIElementsEditorUtility
    {
        internal static readonly string s_DefaultCommonStyleSheetPath = "StyleSheets/DefaultCommon.uss";
        internal static readonly string s_DefaultCommonDarkStyleSheetPath = "StyleSheets/DefaultCommonDark.uss";
        internal static readonly string s_DefaultCommonLightStyleSheetPath = "StyleSheets/DefaultCommonLight.uss";

        internal static readonly StyleSheet s_DefaultCommonDarkStyleSheet;
        internal static readonly StyleSheet s_DefaultCommonLightStyleSheet;

        static UIElementsEditorUtility()
        {
            s_DefaultCommonDarkStyleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            s_DefaultCommonDarkStyleSheet.hideFlags = HideFlags.HideAndDontSave;
            s_DefaultCommonDarkStyleSheet.isUnityStyleSheet = true;

            s_DefaultCommonLightStyleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            s_DefaultCommonLightStyleSheet.hideFlags = HideFlags.HideAndDontSave;
            s_DefaultCommonLightStyleSheet.isUnityStyleSheet = true;

            ReloadDefaultEditorStyleSheets();
        }

        internal static void ReloadDefaultEditorStyleSheets()
        {
            var defaultCommonSheet = EditorGUIUtility.Load(s_DefaultCommonStyleSheetPath) as StyleSheet;
            var defaultDarkSheet = EditorGUIUtility.Load(s_DefaultCommonDarkStyleSheetPath) as StyleSheet;
            var defaultLighSheet = EditorGUIUtility.Load(s_DefaultCommonLightStyleSheetPath) as StyleSheet;

            ResolveStyleSheets(s_DefaultCommonDarkStyleSheet, defaultCommonSheet, defaultDarkSheet);
            ResolveStyleSheets(s_DefaultCommonLightStyleSheet, defaultCommonSheet, defaultLighSheet);

            Toolbar.ReloadStyleSheets();
        }

        internal static void ResolveStyleSheets(StyleSheet dest, params StyleSheet[] sheetsToResolve)
        {
            var resolver = new StyleSheetResolver();
            resolver.AddStyleSheets(sheetsToResolve);
            resolver.ResolveTo(dest);
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
