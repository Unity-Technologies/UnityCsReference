// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Experimental;
using UnityEditor.StyleSheets;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.StyleSheets;
using Object = UnityEngine.Object;

namespace UnityEditor.StyleSheets
{
    internal class GUISkinToStyleSheet
    {
        #region Implementation
        private static void AddProperty(StyleSheetBuilderHelper helper, FontStyle style, string comment)
        {
            helper.AddProperty(ConverterUtils.k_FontStyle, style == FontStyle.Italic || style == FontStyle.BoldAndItalic ? "italic" : "normal", comment);
            helper.AddProperty(ConverterUtils.k_FontWeight, style == FontStyle.Bold || style == FontStyle.BoldAndItalic ? "bold" : "normal");
        }

        private static void AddProperty(StyleSheetBuilderHelper helper, string name, string suffix, RectOffset offset, RectOffset defaultValue, string comment = "")
        {
            // Note: Same order as CSS which is NOT the same order as the RectOffset constructor

            if (helper.options.exportDefaultValues || offset.left != defaultValue.left)
                helper.AddProperty(ConverterUtils.ToUssPropertyName(name, "left", suffix), offset.left, comment);

            if (helper.options.exportDefaultValues || offset.right != defaultValue.right)
                helper.AddProperty(ConverterUtils.ToUssPropertyName(name, "right", suffix), offset.right);

            if (helper.options.exportDefaultValues || offset.top != defaultValue.top)
                helper.AddProperty(ConverterUtils.ToUssPropertyName(name, "top", suffix), offset.top);

            if (helper.options.exportDefaultValues || offset.bottom != defaultValue.bottom)
                helper.AddProperty(ConverterUtils.ToUssPropertyName(name, "bottom", suffix), offset.bottom);
        }

        private static void AddPropertyResource(StyleSheetBuilderHelper helper, string name, Object resource, string comment = "")
        {
            if (resource != null)
            {
                var resourcePath = EditorResources.GetAssetPath(resource);
                helper.AddPropertyResource(name, resourcePath, comment);
            }
            else
            {
                helper.AddProperty(name, StyleValueKeyword.None);
            }
        }

        private static void AddState(StyleSheetBuilderHelper helper, GUIStyleState state, GUIStyleState defaultStyle)
        {
            if (helper.options.exportDefaultValues || !GUISkinCompare.CompareTo(state.textColor, defaultStyle.textColor))
                helper.AddProperty("color", state.textColor, "GUIState.textColor");

            if (helper.options.exportDefaultValues || state.background != defaultStyle.background)
                AddPropertyResource(helper, ConverterUtils.k_BackgroundImage, state.background, "GUIState.background");

            var scaledBackground = state.scaledBackgrounds.Length > 0 ? state.scaledBackgrounds[0] : null;
            var defaultScaledBackground = defaultStyle.scaledBackgrounds.Length > 0 ? defaultStyle.scaledBackgrounds[0] : null;
            if (helper.options.exportDefaultValues || scaledBackground != defaultScaledBackground)
            {
                AddPropertyResource(helper, ConverterUtils.k_ScaledBackground, scaledBackground, "GUIState.scaledBackgrounds");
            }
        }

        private static void AddState(StyleSheetBuilderHelper helper, string stateRuleSelector, GUIStyleState state, string stateId, GUIStyleState defaultStyle)
        {
            // All common Style property
            helper.BeginRule("GUIStyle." + stateId);

            using (helper.builder.BeginComplexSelector(0))
            {
                // Construct rule according to the GUIStyle -> is it custom? is it bound on a type?
                helper.builder.AddSimpleSelector(ConverterUtils.GetStateRuleSelectorParts(stateRuleSelector, stateId), StyleSelectorRelationship.None);
            }

            AddState(helper, state, defaultStyle);

            helper.EndRule();
        }

        #endregion

        #region ConversionAPI
        // Public API to serialize GUIStyle and GUISkin
        public static void AddStyle(StyleSheetBuilderHelper helper, string name, GUIStyle style, GUIStyle defaultStyle = null, string extendName = null)
        {
            defaultStyle = defaultStyle ?? GUIStyle.none;
            // All common Style property
            helper.BeginRule();

            using (helper.builder.BeginComplexSelector(0))
            {
                // Construct rule according to the GUIStyle -> is it custom? is it bound on a type?
                helper.builder.AddSimpleSelector(new[] { StyleSelectorPart.CreateType(name) }, StyleSelectorRelationship.None);
            }

            if (!string.IsNullOrEmpty(extendName))
            {
                helper.AddPropertyString(ConverterUtils.k_Extend, extendName);
            }

            // Loop for each GUIStyle property
            if (helper.options.exportDefaultValues || style.alignment != defaultStyle.alignment)
                helper.AddProperty(ConverterUtils.k_TextAlignment, ConverterUtils.ToUssString(style.alignment), "GUIStyle.alignment");

            if (helper.options.exportDefaultValues || !GUISkinCompare.CompareTo(style.border, defaultStyle.border))
                AddProperty(helper, ConverterUtils.k_Border, "", style.border, defaultStyle.border, "GUIStyle.border");

            if (helper.options.exportDefaultValues || style.clipping != defaultStyle.clipping)
                helper.AddProperty(ConverterUtils.k_Clipping, ConverterUtils.ToUssString(style.clipping), "GUIStyle.clipping");

            if (helper.options.exportDefaultValues || style.contentOffset != defaultStyle.contentOffset)
                helper.AddProperty(ConverterUtils.k_ContentOffset, style.contentOffset, "GUIStyle.contentOffset");

            if (helper.options.exportDefaultValues || !GUISkinCompare.CompareTo(style.fixedHeight, defaultStyle.fixedHeight))
                helper.AddProperty(ConverterUtils.k_Height, style.fixedHeight, "GUIStyle.fixedHeight");

            if (helper.options.exportDefaultValues || !GUISkinCompare.CompareTo(style.fixedWidth, defaultStyle.fixedWidth))
                helper.AddProperty(ConverterUtils.k_Width, style.fixedWidth, "GUIStyle.fixedWidth");

            if (helper.options.exportDefaultValues || style.font != defaultStyle.font)
                AddPropertyResource(helper, ConverterUtils.k_Font, style.font, "GUIStyle.font");

            if (helper.options.exportDefaultValues || style.fontSize != defaultStyle.fontSize)
                helper.AddProperty(ConverterUtils.k_FontSize, style.fontSize, "GUIStyle.fontSize");

            if (helper.options.exportDefaultValues || style.fontStyle != defaultStyle.fontStyle)
                AddProperty(helper, style.fontStyle, "GUIStyle.fontSize");

            if (helper.options.exportDefaultValues || style.imagePosition != defaultStyle.imagePosition)
                helper.AddProperty(ConverterUtils.k_ImagePosition, ConverterUtils.ToUssString(style.imagePosition), "GUIStyle.imagePosition");

            if (helper.options.exportDefaultValues || !GUISkinCompare.CompareTo(style.margin, defaultStyle.margin))
                AddProperty(helper, ConverterUtils.k_Margin, null, style.margin, defaultStyle.margin, "GUIStyle.margin");

            // Always export name:
            helper.AddPropertyString(ConverterUtils.k_Name, style.name, "GUIStyle.name");

            if (helper.options.exportDefaultValues || !GUISkinCompare.CompareTo(style.overflow, defaultStyle.overflow))
                AddProperty(helper, ConverterUtils.k_Overflow, null, style.overflow, defaultStyle.overflow, "GUIStyle.overflow");

            if (helper.options.exportDefaultValues || !GUISkinCompare.CompareTo(style.padding, defaultStyle.padding))
                AddProperty(helper, ConverterUtils.k_Padding, null, style.padding, defaultStyle.padding, "GUIStyle.padding");

            if (helper.options.exportDefaultValues || style.richText != defaultStyle.richText)
                helper.AddProperty(ConverterUtils.k_RichText, style.richText, "GUIStyle.richText");

            if (helper.options.exportDefaultValues || style.stretchHeight != defaultStyle.stretchHeight)
                helper.AddProperty(ConverterUtils.k_StretchHeight, style.stretchHeight, "GUIStyle.stretchHeight");

            if (helper.options.exportDefaultValues || style.stretchWidth != defaultStyle.stretchWidth)
                helper.AddProperty(ConverterUtils.k_StretchWidth, style.stretchWidth, "GUIStyle.stretchWidth");

            if (helper.options.exportDefaultValues || style.wordWrap != defaultStyle.wordWrap)
                helper.AddProperty(ConverterUtils.k_WordWrap, style.wordWrap, "GUIStyle.wordWrap");

            // Add Normal state properties
            if (helper.options.exportDefaultValues || !GUISkinCompare.CompareTo(style.normal, defaultStyle.normal))
                AddState(helper, style.normal, defaultStyle.normal);

            helper.EndRule();

            // Add one rule for each GUIStyleState (other than normal)
            if (helper.options.exportDefaultValues || !GUISkinCompare.CompareTo(style.active, defaultStyle.active))
                AddState(helper, name, style.active, "active", defaultStyle.active);

            if (helper.options.exportDefaultValues || !GUISkinCompare.CompareTo(style.focused, defaultStyle.focused))
                AddState(helper, name, style.focused, "focused", defaultStyle.focused);

            if (helper.options.exportDefaultValues || !GUISkinCompare.CompareTo(style.hover, defaultStyle.hover))
                AddState(helper, name, style.hover, "hover", defaultStyle.hover);

            if (helper.options.exportDefaultValues || !GUISkinCompare.CompareTo(style.onActive, defaultStyle.onActive))
                AddState(helper, name, style.onActive, "onActive", defaultStyle.onActive);

            if (helper.options.exportDefaultValues || !GUISkinCompare.CompareTo(style.onFocused, defaultStyle.onFocused))
                AddState(helper, name, style.onFocused, "onFocused", defaultStyle.onFocused);

            if (helper.options.exportDefaultValues || !GUISkinCompare.CompareTo(style.onHover, defaultStyle.onHover))
                AddState(helper, name, style.onHover, "onHover", defaultStyle.onHover);

            if (helper.options.exportDefaultValues || !GUISkinCompare.CompareTo(style.onNormal, defaultStyle.onNormal))
                AddState(helper, name, style.onNormal, "onNormal", defaultStyle.onNormal);
        }

        public static StyleSheet ToStyleSheet(GUIStyle style, string name, UssExportOptions options = null)
        {
            var builder = new StyleSheetBuilderHelper(options);
            AddStyle(builder, name, style);
            builder.PopulateSheet();
            return builder.sheet;
        }

        public static void AddSkin(StyleSheetBuilderHelper helper, GUISkin skin)
        {
            AddGlobalFont(helper, skin.font);

            // Builtin GUIStyle
            skin.ForEachGUIStyleProperty((name, style) =>
            {
                AddStyle(helper, name.Capitalize(), style);
            });

            // Custom Styles
            foreach (var style in skin.customStyles)
            {
                // GUISkin when instantiated have a null customStyle
                if (style == null) continue;

                var customStyleName = ConverterUtils.ToGUIStyleSelectorName(style.name);
                AddStyle(helper, customStyleName, style);
            }

            AddSettings(helper, skin.settings);
        }

        public static void AddGlobalFont(StyleSheetBuilderHelper helper, Font font)
        {
            // Global Font
            helper.BeginRule("GUISkin globals");
            using (helper.builder.BeginComplexSelector(0))
            {
                helper.builder.AddSimpleSelector(new[] { StyleSelectorPart.CreateWildCard() }, StyleSelectorRelationship.None);
            }
            if (font != null)
                AddPropertyResource(helper, ConverterUtils.k_Font, font, "GUISkin.font");
            helper.EndRule();
        }

        public static void AddSettings(StyleSheetBuilderHelper helper, GUISettings settings)
        {
            // Settings
            helper.BeginRule("GUISkin.settings - GUISettings");
            using (helper.builder.BeginComplexSelector(0))
            {
                helper.builder.AddSimpleSelector(new[] { StyleSelectorPart.CreateClass(ConverterUtils.k_GUISettingsSelector.Replace(".", "")) }, StyleSelectorRelationship.None);
            }

            helper.AddProperty(ConverterUtils.k_SelectionColor, settings.selectionColor, "GUISettings.selectionColor");
            helper.AddProperty(ConverterUtils.k_CursorColor, settings.cursorColor, "GUISettings.cursorColor");
            helper.AddProperty(ConverterUtils.k_CursorFlashSpeed, settings.cursorFlashSpeed, "GUISettings.cursorFlashSpeed");
            helper.AddProperty(ConverterUtils.k_DoubleClickSelectsWord, settings.doubleClickSelectsWord, "GUISettings.doubleClickSelectsWord");
            helper.AddProperty(ConverterUtils.k_TripleClickSelectsLine, settings.tripleClickSelectsLine, "GUISettings.tripleClickSelectsLine");

            helper.EndRule();
        }

        public static StyleSheet ToStyleSheet(GUISkin skin, UssExportOptions options = null)
        {
            var builder = new StyleSheetBuilderHelper(options);
            AddSkin(builder, skin);
            builder.PopulateSheet();
            return builder.sheet;
        }

        #endregion
    }
}
