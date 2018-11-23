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

            style.fixedWidth = styleBlock.GetFloat(StyleCatalogKeyword.width, style.fixedWidth);
            style.fixedHeight = styleBlock.GetFloat(StyleCatalogKeyword.height, style.fixedHeight);
            style.margin = GetStyleRectOffset(styleBlock, "margin", style.margin);
            style.padding = GetStyleRectOffset(styleBlock, "padding", style.padding);

            style.stretchHeight = styleBlock.GetBool("-unity-stretch-height".GetHashCode(), style.stretchHeight);
            style.stretchWidth = styleBlock.GetBool("-unity-stretch-width".GetHashCode(), style.stretchWidth);

            style.border = GetStyleRectOffset(styleBlock, "-unity-slice", style.border);
            style.overflow = GetStyleRectOffset(styleBlock, "-unity-overflow", style.overflow);

            var contentOffsetKey = "-unity-content-offset".GetHashCode();
            if (styleBlock.HasValue(contentOffsetKey, StyleValue.Type.Rect))
            {
                var contentOffsetSize = styleBlock.GetRect(contentOffsetKey);
                style.contentOffset = new Vector2(contentOffsetSize.width, contentOffsetSize.height);
            }

            style.font = styleBlock.GetResource("-unity-font".GetHashCode(), style.font);
            style.font = styleBlock.GetResource("font".GetHashCode(), style.font);

            if (style.fontSize == 0 || styleBlock.HasValue(StyleCatalogKeyword.fontSize, StyleValue.Type.Number))
            {
                var defaultFontSize = rootBlock.GetInt("--unity-font-size", style.fontSize);
                style.fontSize = styleBlock.GetInt(StyleCatalogKeyword.fontSize, useExtensionDefaultValues ? defaultFontSize : style.fontSize);
            }
            style.fontStyle = ParseEnum(styleBlock, "font-style".GetHashCode(), style.fontStyle);

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
                    var importer = new StyleSheetImporterImpl();
                    var styleSheet = ScriptableObject.CreateInstance<UnityEngine.UIElements.StyleSheet>();
                    if (!ussInPlaceStyleOverride.Contains("{"))
                    {
                        // A bit of sugar syntax in case the user doesn't provide the uss class declaration.
                        ussInPlaceStyleOverride = $".{ConverterUtils.EscapeSelectorName(style.name)} {{\n{ussInPlaceStyleOverride}\n}}";
                    }
                    importer.Import(styleSheet, ussInPlaceStyleOverride);
                    var overrideCatalog = new StyleCatalog();
                    overrideCatalog.Refresh(styleSheet);
                    const bool useExtensionDefaultValues = false;
                    PopulateStyle(overrideCatalog, style, blockName, useExtensionDefaultValues);
                }
                catch (Exception e)
                {
                    Debug.LogError("Cannot compile style override:" + e);
                }
            }
        }

        internal static GUIStyle FromUSS(string ussStyleRuleName, string ussInPlaceStyleOverride = null, GUISkin srcSkin = null)
        {
            // Check if the style already exists in skin
            var blockName = ussStyleRuleName.Replace(".", "");
            var styleName = ConverterUtils.ToStyleName(ussStyleRuleName);
            var inSkin = (srcSkin ?? GUISkin.current).FindStyle(styleName);
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
            var blockName = ussStyleRuleName.Replace(".", "");
            PopulateFromUSS(EditorResources.styleCatalog, style, blockName, ussInPlaceStyleOverride);
            return style;
        }

        internal static RectOffset GetStyleRectOffset(StyleBlock block, int propertyKey, RectOffset src)
        {
            var rect = block.GetRect(propertyKey, new StyleRect(src));
            return new RectOffset(Mathf.RoundToInt(rect.left), Mathf.RoundToInt(rect.right), Mathf.RoundToInt(rect.top), Mathf.RoundToInt(rect.bottom));
        }

        internal static RectOffset GetStyleRectOffset(StyleBlock block, string propertyKey, RectOffset src)
        {
            return GetStyleRectOffset(block, propertyKey.GetHashCode(), src);
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
    }
}
