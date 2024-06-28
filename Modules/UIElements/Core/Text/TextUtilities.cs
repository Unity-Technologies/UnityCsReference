// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    internal static class TextUtilities
    {
        public static Func<TextSettings> getEditorTextSettings;
        internal static Func<bool> IsAdvancedTextEnabled;
        private static TextSettings s_TextSettings;

        public static TextSettings textSettings
        {
            get
            {
                if (s_TextSettings == null)
                {
                    s_TextSettings = getEditorTextSettings();
                }

                return s_TextSettings;
            }
        }

        internal static Vector2 MeasureVisualElementTextSize(TextElement te, in RenderedText textToMeasure, float width, VisualElement.MeasureMode widthMode, float height, VisualElement.MeasureMode heightMode)
        {
            float measuredWidth = float.NaN;
            float measuredHeight = float.NaN;

            if (!IsFontAssigned(te))
                return new Vector2(measuredWidth, measuredHeight);

            float pixelsPerPoint = 1.0f;
            if (te.panel != null)
                pixelsPerPoint = te.scaledPixelsPerPoint;

            if (pixelsPerPoint <= 0)
                return Vector2.zero;

            if (widthMode != VisualElement.MeasureMode.Exactly || heightMode != VisualElement.MeasureMode.Exactly)
            {
                var size = te.uitkTextHandle.ComputeTextSize(textToMeasure, width, height);
                measuredWidth = size.x;
                measuredHeight = size.y;
            }

            if (widthMode == VisualElement.MeasureMode.Exactly)
            {
                measuredWidth = width;
            }
            else
            {
                if (widthMode == VisualElement.MeasureMode.AtMost)
                {
                    measuredWidth = Mathf.Min(measuredWidth, width);
                }
            }

            if (heightMode == VisualElement.MeasureMode.Exactly)
            {
                measuredHeight = height;
            }
            else
            {
                if (heightMode == VisualElement.MeasureMode.AtMost)
                {
                    measuredHeight = Mathf.Min(measuredHeight, height);
                }
            }

            // Case 1215962: round up as yoga could decide to round down and text would start wrapping
            float roundedWidth = AlignmentUtils.CeilToPixelGrid(measuredWidth, pixelsPerPoint, 0.0f);
            float roundedHeight = AlignmentUtils.CeilToPixelGrid(measuredHeight, pixelsPerPoint, 0.0f);
            var roundedValues = new Vector2(roundedWidth, roundedHeight);

            te.uitkTextHandle.MeasuredSizes = new Vector2(measuredWidth, measuredHeight);
            te.uitkTextHandle.RoundedSizes = roundedValues;

            return roundedValues;
        }

        internal static FontAsset GetFontAsset(VisualElement ve)
        {
            if (ve.computedStyle.unityFontDefinition.fontAsset != null)
                return ve.computedStyle.unityFontDefinition.fontAsset as FontAsset;

            var textSettings = GetTextSettingsFrom(ve);
            if (ve.computedStyle.unityFontDefinition.font != null)
                return textSettings.GetCachedFontAsset(ve.computedStyle.unityFontDefinition.font, TextShaderUtilities.ShaderRef_MobileSDF);
            else if (ve.computedStyle.unityFont != null)
                return textSettings.GetCachedFontAsset(ve.computedStyle.unityFont, TextShaderUtilities.ShaderRef_MobileSDF);
            else if (textSettings != null)
                return textSettings.defaultFontAsset;
            return null;
        }

        internal static bool IsFontAssigned(VisualElement ve)
        {
            return ve.computedStyle.unityFont != null || !ve.computedStyle.unityFontDefinition.IsEmpty();
        }

        internal static TextSettings GetTextSettingsFrom(VisualElement ve)
        {
            if (ve.panel is RuntimePanel runtimePanel)
                return runtimePanel.panelSettings.textSettings ?? PanelTextSettings.defaultPanelTextSettings;
            return getEditorTextSettings();
        }

        static bool s_HasAdvancedTextSystemErrorBeenShown = false;

        internal static bool IsAdvancedTextEnabledForElement(TextElement te)
        {
            var isAdvancedTextGeneratorEnabledOnTextElement = te.computedStyle.unityTextGenerator == TextGeneratorType.Advanced;
            var isAdvancedTextGeneratorEnabledOnProject = false;
            isAdvancedTextGeneratorEnabledOnProject = IsAdvancedTextEnabled?.Invoke() ?? false;
            if (!s_HasAdvancedTextSystemErrorBeenShown && !isAdvancedTextGeneratorEnabledOnProject && isAdvancedTextGeneratorEnabledOnTextElement)
            {
                s_HasAdvancedTextSystemErrorBeenShown = true;
                Debug.LogError("Advanced Text Generator is disabled but the API is still called. Please enable it in the UI Toolkit Project Settings if you want to use it, or refrain from calling the API.");
            }

            return isAdvancedTextGeneratorEnabledOnTextElement && isAdvancedTextGeneratorEnabledOnProject;
        }

        internal static float ConvertPixelUnitsToTextCoreRelativeUnits(VisualElement ve, FontAsset fontAsset)
        {
            // Convert the text settings pixel units to TextCore relative units
            float paddingPercent = 1.0f / fontAsset.atlasPadding;
            float pointSizeRatio = ((float)fontAsset.faceInfo.pointSize) / ve.computedStyle.fontSize.value;
            return paddingPercent * pointSizeRatio;
        }

        internal static TextCoreSettings GetTextCoreSettingsForElement(VisualElement ve)
        {
            var fontAsset = GetFontAsset(ve);
            if (fontAsset == null)
                return new TextCoreSettings();

            var resolvedStyle = ve.resolvedStyle;
            var computedStyle = ve.computedStyle;

            float factor = ConvertPixelUnitsToTextCoreRelativeUnits(ve, fontAsset);

            float outlineWidth = Mathf.Clamp(resolvedStyle.unityTextOutlineWidth * factor, 0.0f, 1.0f);
            float underlaySoftness = Mathf.Clamp(computedStyle.textShadow.blurRadius * factor, 0.0f, 1.0f);

            float underlayOffsetX = computedStyle.textShadow.offset.x < 0 ? Mathf.Max(computedStyle.textShadow.offset.x * factor, -1.0f) : Mathf.Min(computedStyle.textShadow.offset.x * factor, 1.0f);
            float underlayOffsetY = computedStyle.textShadow.offset.y < 0 ? Mathf.Max(computedStyle.textShadow.offset.y * factor, -1.0f) : Mathf.Min(computedStyle.textShadow.offset.y * factor, 1.0f);
            Vector2 underlayOffset = new Vector2(underlayOffsetX, underlayOffsetY);

            var faceColor = resolvedStyle.color;
            var outlineColor = resolvedStyle.unityTextOutlineColor;
            if (outlineWidth < UIRUtility.k_Epsilon)
                outlineColor.a = 0.0f;

            return new TextCoreSettings()
            {
                faceColor = faceColor,
                outlineColor = outlineColor,
                outlineWidth = outlineWidth,
                underlayColor = computedStyle.textShadow.color,
                underlayOffset = underlayOffset,
                underlaySoftness = underlaySoftness
            };
        }

        public static TextWrappingMode toTextWrappingMode(this WhiteSpace whiteSpace)
        {
            return whiteSpace switch
            {
                WhiteSpace.Normal => TextWrappingMode.Normal,
                WhiteSpace.NoWrap => TextWrappingMode.NoWrap,
                WhiteSpace.PreWrap => TextWrappingMode.PreserveWhitespace,
                WhiteSpace.Pre => TextWrappingMode.PreserveWhitespaceNoWrap,
                _ => TextWrappingMode.Normal
            };
        }

        public static TextCore.WhiteSpace toTextCore(this WhiteSpace whiteSpace)
        {
            return whiteSpace switch
            {
                WhiteSpace.Normal => TextCore.WhiteSpace.Normal,
                WhiteSpace.NoWrap => TextCore.WhiteSpace.NoWrap,
                WhiteSpace.PreWrap => TextCore.WhiteSpace.PreWrap,
                WhiteSpace.Pre => TextCore.WhiteSpace.Pre,
                _ => TextCore.WhiteSpace.Normal
            };
        }
    }
}
