// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Experimental;
using UnityEngine;

namespace UnityEditor.StyleSheets
{
    internal static class GUIStyleExtensions
    {
        const string k_ImguiBlockPrefix = "imgui-style-";

        internal static void PopulateStyleState(StyleBlock styleBlock, GUIStyleState state, GUIStyleState defaultState)
        {
            var background = styleBlock.GetResource<Texture2D>(StyleCatalogKeyword.backgroundImage);
            if (background != null)
            {
                state.background = background;
            }
            var scaledBackground = styleBlock.GetResource<Texture2D>(StyleCatalogKeyword.scaledBackgroundImage);
            if (scaledBackground != null)
            {
                state.scaledBackgrounds = new[] { scaledBackground };
            }
            state.textColor = styleBlock.GetColor(StyleCatalogKeyword.color, (state.background != null || defaultState == null) ? state.textColor : defaultState.textColor);
        }

        internal static void PopulateStyle(StyleCatalog catalog, GUIStyle style, string blockName, bool useExtensionDefaultValues = true)
        {
            if (string.IsNullOrEmpty(blockName))
            {
                blockName = ConverterUtils.EscapeSelectorName(style.name);
            }

            var styleBlock = catalog.GetStyle(blockName);
            var rootBlock = catalog.GetStyle(StyleCatalogKeyword.root, StyleState.root);

            style.name = styleBlock.GetText(ConverterUtils.k_Name, style.name);
            if (string.IsNullOrEmpty(style.name))
            {
                style.name = BlockNameToStyleName(blockName);
            }
            style.fixedWidth = styleBlock.GetFloat(StyleCatalogKeyword.width, style.fixedWidth);
            style.fixedHeight = styleBlock.GetFloat(StyleCatalogKeyword.height, style.fixedHeight);
            GetStyleRectOffset(styleBlock, "margin", style.margin);
            GetStyleRectOffset(styleBlock, "padding", style.padding);

            style.stretchHeight = styleBlock.GetBool("-unity-stretch-height".GetHashCode(), style.stretchHeight);
            style.stretchWidth = styleBlock.GetBool("-unity-stretch-width".GetHashCode(), style.stretchWidth);

            GetStyleRectOffset(styleBlock, "-unity-slice", style.border);
            GetStyleRectOffset(styleBlock, "-unity-overflow", style.overflow);

            var contentOffsetKey = "-unity-content-offset".GetHashCode();
            if (styleBlock.HasValue(contentOffsetKey, StyleValue.Type.Rect))
            {
                var contentOffsetSize = styleBlock.GetRect(contentOffsetKey);
                style.contentOffset = new Vector2(contentOffsetSize.width, contentOffsetSize.height);
            }

            // Support both properties for font:
            style.font = styleBlock.GetResource<Font>("-unity-font".GetHashCode(), style.font);
            style.font = styleBlock.GetResource<Font>("font".GetHashCode(), style.font);

            if (style.fontSize == 0 || styleBlock.HasValue(StyleCatalogKeyword.fontSize, StyleValue.Type.Number))
                style.fontSize = styleBlock.GetInt(StyleCatalogKeyword.fontSize, style.fontSize);

            var fontStyleStr = styleBlock.GetText(ConverterUtils.k_FontStyle.GetHashCode());
            var fontWeightStr = styleBlock.GetText(ConverterUtils.k_FontWeight.GetHashCode());
            FontStyle fontStyle;
            if (ConverterUtils.TryGetFontStyle(fontStyleStr, fontWeightStr, out fontStyle))
            {
                style.fontStyle = fontStyle;
            }

            style.imagePosition = ConverterUtils.ToImagePosition(styleBlock.GetText("-unity-image-position".GetHashCode(), ConverterUtils.ToUssString(style.imagePosition)));
            style.clipping = ConverterUtils.ToTextClipping(styleBlock.GetText("-unity-clipping".GetHashCode(), ConverterUtils.ToUssString(style.clipping)));
            style.alignment = ConverterUtils.ToTextAnchor(styleBlock.GetText("-unity-text-align".GetHashCode(), ConverterUtils.ToUssString(style.alignment)));

            style.richText = styleBlock.GetBool("-unity-rich-text".GetHashCode(), style.richText);
            style.wordWrap = styleBlock.GetBool("-unity-word-wrap".GetHashCode(), style.wordWrap);

            var defaultStyleState = useExtensionDefaultValues ? new GUIStyleState() { textColor = styleBlock.GetColor(StyleCatalogKeyword.color, rootBlock.GetColor("--unity-text-color")) } : null;

            PopulateStyleState(styleBlock, style.normal, defaultStyleState);
            PopulateStyleState(catalog.GetStyle(blockName, StyleState.hover), style.hover, defaultStyleState);
            PopulateStyleState(catalog.GetStyle(blockName, StyleState.focus), style.focused, defaultStyleState);

            // Supports GUISkin Generation selector that assumes GUIStyle.active maps to :hover:active
            PopulateStyleState(catalog.GetStyle(blockName, StyleState.hover | StyleState.active), style.active, defaultStyleState);
            PopulateStyleState(catalog.GetStyle(blockName, StyleState.active), style.active, null);

            //// All "on" states uses their parent pseudo class (without :checked) as their default value
            PopulateStyleState(catalog.GetStyle(blockName, StyleState.@checked), style.onNormal, useExtensionDefaultValues ? style.normal : null);
            PopulateStyleState(catalog.GetStyle(blockName, StyleState.@checked | StyleState.hover), style.onHover, useExtensionDefaultValues ? style.hover : null);

            // Supports GUISkin Generation selector that assumes GUIStyle.onActive maps to :hover:active:checked
            PopulateStyleState(catalog.GetStyle(blockName, StyleState.@checked | StyleState.active), style.onActive, useExtensionDefaultValues ? style.active : null);
            PopulateStyleState(catalog.GetStyle(blockName, StyleState.@checked | StyleState.hover | StyleState.active), style.onActive, null);
            // Supports GUISkin Generation selector that assumes GUIStyle.onFocused maps to:hover:focus:checked
            PopulateStyleState(catalog.GetStyle(blockName, StyleState.@checked | StyleState.focus), style.onFocused, useExtensionDefaultValues ? style.focused : null);
            PopulateStyleState(catalog.GetStyle(blockName, StyleState.@checked | StyleState.hover | StyleState.focus), style.onFocused, null);
        }

        internal static void PopulateFromUSS(StyleCatalog catalog, GUIStyle style, string blockName, string ussInPlaceStyleOverride)
        {
            // Override with style in catalog
            PopulateStyle(catalog, style, blockName);
            if (!string.IsNullOrEmpty(ussInPlaceStyleOverride))
            {
                try
                {
                    if (!ussInPlaceStyleOverride.Contains("{"))
                    {
                        // A bit of sugar syntax in case the user doesn't provide the uss class declaration.
                        ussInPlaceStyleOverride = $".{ConverterUtils.EscapeSelectorName(style.name)} {{\n{ussInPlaceStyleOverride}\n}}";
                    }

                    var styleSheet = ConverterUtils.CompileStyleSheetContent(ussInPlaceStyleOverride);
                    var overrideCatalog = new StyleCatalog();
                    overrideCatalog.Load(new[] {styleSheet});
                    const bool useExtensionDefaultValues = false;
                    PopulateStyle(overrideCatalog, style, blockName, useExtensionDefaultValues);
                }
                catch (Exception e)
                {
                    Debug.LogError("Cannot compile style override:" + e);
                }
            }
        }

        internal static GUIStyle FromUSS(GUIStyle baseStyle, string ussStyleRuleName, string ussInPlaceStyleOverride = null, GUISkin srcSkin = null)
        {
            if (GUISkin.current == null)
                return null;

            // Check if the style already exists in skin
            var blockName = RuleNameToBlockName(ussStyleRuleName);
            var styleName = ConverterUtils.ToStyleName(ussStyleRuleName);
            var style = new GUIStyle(baseStyle) { name = styleName };
            PopulateFromUSS(EditorResources.styleCatalog, style, blockName, ussInPlaceStyleOverride);
            ConvertToExtendedStyle(style);
            return style;
        }

        internal static GUIStyle FromUSS(string ussStyleRuleName, string ussInPlaceStyleOverride = null, GUISkin srcSkin = null)
        {
            if (GUISkin.current == null)
                return null;

            // Check if the style already exists in skin
            var blockName = RuleNameToBlockName(ussStyleRuleName);
            var styleName = ConverterUtils.ToStyleName(ussStyleRuleName);
            var inSkin = (srcSkin ? srcSkin : GUISkin.current).FindStyle(styleName);
            var style = new GUIStyle() { name = styleName };
            if (inSkin != null)
            {
                style.Assign(inSkin);
            }

            PopulateFromUSS(EditorResources.styleCatalog, style, blockName, ussInPlaceStyleOverride);
            return style;
        }

        internal static GUIStyle ApplyUSS(GUIStyle style, string ussStyleRuleName, string ussInPlaceStyleOverride = null)
        {
            var blockName = RuleNameToBlockName(ussStyleRuleName);
            PopulateFromUSS(EditorResources.styleCatalog, style, blockName, ussInPlaceStyleOverride);
            return style;
        }

        internal static string RuleNameToBlockName(string ussStyleRuleName)
        {
            return ussStyleRuleName.Replace(".", "");
        }

        internal static string BlockNameToStyleName(string blockName)
        {
            var lowerSelector = blockName.ToLower();
            if (ConverterUtils.k_GuiStyleTypeNames.ContainsKey(lowerSelector))
            {
                return lowerSelector;
            }

            return blockName.Replace(k_ImguiBlockPrefix, "").Replace("-", " ").Replace(".", "");
        }

        internal static string StyleNameToBlockName(string guiStyleName, bool appendImguiPrefix = true)
        {
            if (ConverterUtils.k_GuiStyleTypeNames.ContainsKey(guiStyleName))
            {
                return ConverterUtils.EscapeSelectorName(ConverterUtils.k_GuiStyleTypeNames[guiStyleName]);
            }

            var blockName = appendImguiPrefix ? k_ImguiBlockPrefix : "";
            blockName += ConverterUtils.EscapeSelectorName(guiStyleName);
            return blockName;
        }

        internal static void GetStyleRectOffset(StyleBlock block, string propertyKey, RectOffset src)
        {
            src.left = block.GetInt(propertyKey + "-left", src.left);
            src.right = block.GetInt(propertyKey + "-right", src.right);
            src.top = block.GetInt(propertyKey + "-top", src.top);
            src.bottom = block.GetInt(propertyKey + "-bottom", src.bottom);
        }

        internal static T ParseEnum<T>(StyleBlock block, int key, T defaultValue)
        {
            try
            {
                if (block.HasValue(key, StyleValue.Type.Text))
                    return (T)Enum.Parse(typeof(T), block.GetText(key), true);
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        internal static void ResetDeprecatedBackgroundImage(GUIStyleState state)
        {
            state.background = null;
            if (state.scaledBackgrounds != null && state.scaledBackgrounds.Length >= 1)
                state.scaledBackgrounds[0] = null;
        }

        internal static GUIStyle ConvertToExtendedStyle(GUIStyle style)
        {
            // The new style extension do not support state backgrounds anymore.
            // Any background images needs to be defined in the uss data files.
            ResetDeprecatedBackgroundImage(style.normal);
            ResetDeprecatedBackgroundImage(style.hover);
            ResetDeprecatedBackgroundImage(style.active);
            ResetDeprecatedBackgroundImage(style.focused);
            ResetDeprecatedBackgroundImage(style.onNormal);
            ResetDeprecatedBackgroundImage(style.onHover);
            ResetDeprecatedBackgroundImage(style.onActive);
            ResetDeprecatedBackgroundImage(style.onFocused);

            return style;
        }
    }
}
