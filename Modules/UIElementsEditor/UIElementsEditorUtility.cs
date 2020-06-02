// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.UIElements;

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
        internal static readonly string s_DefaultCommonDarkStyleSheetPath =
            Path.Combine(UIElementsPackageUtility.EditorResourcesBasePath, "StyleSheets/Generated/DefaultCommonDark.uss.asset");
        internal static readonly string s_DefaultCommonLightStyleSheetPath =
            Path.Combine(UIElementsPackageUtility.EditorResourcesBasePath, "StyleSheets/Generated/DefaultCommonLight.uss.asset");

        internal static readonly StyleSheet s_DefaultCommonDarkStyleSheet;
        internal static readonly StyleSheet s_DefaultCommonLightStyleSheet;

        internal static string GetStyleSheetPathForFont(string sheetPath, string fontName)
        {
            // Load the stylesheet of the current font
            if (LocalizationDatabase.currentEditorLanguage == SystemLanguage.English)
            {
                return sheetPath.Replace(".uss", "_" + fontName.ToLowerInvariant() + ".uss");
            }
            else
            {
                return sheetPath;
            }
        }

        internal static string GetStyleSheetPathForCurrentFont(string sheetPath)
        {
            return GetStyleSheetPathForFont(sheetPath, EditorResources.currentFontName);
        }

        internal static StyleSheet LoadSKinnedStyleSheetForFont(int skin, string fontName)
        {
            return EditorGUIUtility.Load(GetStyleSheetPathForFont(skin == EditorResources.darkSkinIndex ? s_DefaultCommonDarkStyleSheetPath : s_DefaultCommonLightStyleSheetPath, fontName)) as StyleSheet;
        }

        static UIElementsEditorUtility()
        {
            // Load the stylesheet of the current font
            s_DefaultCommonDarkStyleSheet = LoadSKinnedStyleSheetForFont(EditorResources.darkSkinIndex, EditorResources.currentFontName);
            s_DefaultCommonDarkStyleSheet.isUnityStyleSheet = true;

            s_DefaultCommonLightStyleSheet = LoadSKinnedStyleSheetForFont(EditorResources.normalSkinIndex, EditorResources.currentFontName);
            s_DefaultCommonLightStyleSheet.isUnityStyleSheet = true;
        }

        internal static int GetCursorId(StyleSheet sheet, StyleValueHandle handle)
        {
            var value = sheet.ReadEnum(handle);
            if (string.Equals(value, "arrow", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.Arrow;
            else if (string.Equals(value, "text", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.Text;
            else if (string.Equals(value, "resize-vertical", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.ResizeVertical;
            else if (string.Equals(value, "resize-horizontal", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.ResizeHorizontal;
            else if (string.Equals(value, "link", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.Link;
            else if (string.Equals(value, "slide-arrow", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.SlideArrow;
            else if (string.Equals(value, "resize-up-right", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.ResizeUpRight;
            else if (string.Equals(value, "resize-up-left", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.ResizeUpLeft;
            else if (string.Equals(value, "move-arrow", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.MoveArrow;
            else if (string.Equals(value, "rotate-arrow", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.RotateArrow;
            else if (string.Equals(value, "scale-arrow", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.ScaleArrow;
            else if (string.Equals(value, "arrow-plus", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.ArrowPlus;
            else if (string.Equals(value, "arrow-minus", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.ArrowMinus;
            else if (string.Equals(value, "pan", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.Pan;
            else if (string.Equals(value, "orbit", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.Orbit;
            else if (string.Equals(value, "zoom", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.Zoom;
            else if (string.Equals(value, "fps", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.FPS;
            else if (string.Equals(value, "split-resize-up-down", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.SplitResizeUpDown;
            else if (string.Equals(value, "split-resize-left-right", StringComparison.OrdinalIgnoreCase))
                return (int)MouseCursor.SplitResizeLeftRight;

            return (int)MouseCursor.Arrow;
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
