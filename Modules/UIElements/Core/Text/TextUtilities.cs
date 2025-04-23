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

            if (IsAdvancedTextEnabledForElement(te))
            {
                te.uitkTextHandle.ATGMeasuredSizes = new Vector2(measuredWidth, measuredHeight);
                te.uitkTextHandle.ATGRoundedSizes = roundedValues;
                te.uitkTextHandle.LastPixelPerPoint = pixelsPerPoint;

            }
            else
            {
                te.uitkTextHandle.MeasuredWidth = measuredWidth;
                te.uitkTextHandle.RoundedWidth = roundedWidth;
                te.uitkTextHandle.LastPixelPerPoint = pixelsPerPoint;
            }

            return roundedValues;
        }

        internal static FontAsset GetFontAsset(VisualElement ve)
        {
            if (ve.computedStyle.unityFontDefinition.fontAsset != null)
                return ve.computedStyle.unityFontDefinition.fontAsset as FontAsset;

            var textSettings = GetTextSettingsFrom(ve);
            if (ve.computedStyle.unityFontDefinition.font != null)
                return textSettings.GetCachedFontAsset(ve.computedStyle.unityFontDefinition.font);
            else if (ve.computedStyle.unityFont != null)
                return textSettings.GetCachedFontAsset(ve.computedStyle.unityFont);
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

        internal static bool IsAdvancedTextEnabledForElement(VisualElement ve)
        {
            if (ve == null)
                return false;
            var isAdvancedTextGeneratorEnabledOnTextElement = ve.computedStyle.unityTextGenerator == TextGeneratorType.Advanced;
            var isAdvancedTextGeneratorEnabledOnProject = false;
            isAdvancedTextGeneratorEnabledOnProject = IsAdvancedTextEnabled?.Invoke() ?? false;
            if (!s_HasAdvancedTextSystemErrorBeenShown && !isAdvancedTextGeneratorEnabledOnProject && isAdvancedTextGeneratorEnabledOnTextElement)
            {
                s_HasAdvancedTextSystemErrorBeenShown = true;
                Debug.LogError("Advanced Text Generator is disabled but the API is still called. Please enable it in the UI Toolkit Project Settings if you want to use it, or refrain from calling the API.");
            }

            return isAdvancedTextGeneratorEnabledOnTextElement && isAdvancedTextGeneratorEnabledOnProject;
        }

        internal static TextCoreSettings GetTextCoreSettingsForElement(VisualElement ve, bool ignoreColors)
        {
            var fontAsset = GetFontAsset(ve);
            if (fontAsset == null)
                return new TextCoreSettings();

            var resolvedStyle = ve.resolvedStyle;
            var computedStyle = ve.computedStyle;
            TextShadow textShadow = computedStyle.textShadow;

            float factor = TextHandle.ConvertPixelUnitsToTextCoreRelativeUnits(computedStyle.fontSize.value, fontAsset);

            float outlineWidth = Mathf.Clamp(resolvedStyle.unityTextOutlineWidth * factor, 0.0f, 1.0f);
            float underlaySoftness = Mathf.Clamp(textShadow.blurRadius * factor, 0.0f, 1.0f);

            float underlayOffsetX = textShadow.offset.x < 0 ? Mathf.Max(textShadow.offset.x * factor, -1.0f) : Mathf.Min(textShadow.offset.x * factor, 1.0f);
            float underlayOffsetY = textShadow.offset.y < 0 ? Mathf.Max(textShadow.offset.y * factor, -1.0f) : Mathf.Min(textShadow.offset.y * factor, 1.0f);
            Vector2 underlayOffset = new Vector2(underlayOffsetX, underlayOffsetY);

            Color faceColor, underlayColor, outlineColor;
            if (ignoreColors)
            {
                faceColor = Color.white;
                underlayColor = Color.white;
                outlineColor = Color.white;
            }
            else
            {
                bool isMultiChannel = ((Texture2D)fontAsset.material.mainTexture).format != TextureFormat.Alpha8;
                faceColor = resolvedStyle.color;
                outlineColor = resolvedStyle.unityTextOutlineColor;
                if (outlineWidth < UIRUtility.k_Epsilon)
                    outlineColor.a = 0.0f;
                underlayColor = textShadow.color;

                if (isMultiChannel)
                {
                    // "Textured" Shader Path
                    // With colored emojis, the render type is "textured"
                    // The TextCore data will be used if the DynamicColor usage hint is set
                    // We don't premultiply but we use the alpha only
                    faceColor = new Color(1, 1, 1, faceColor.a);
                }
                else
                {
                    // "Text" Shader Path
                    // These are only used for SDF, independently of DynamicColor
                    underlayColor.r *= faceColor.a;
                    underlayColor.g *= faceColor.a;
                    underlayColor.b *= faceColor.a;
                    outlineColor.r *= outlineColor.a;
                    outlineColor.g *= outlineColor.a;
                    outlineColor.b *= outlineColor.a;
                }
            }

            return new TextCoreSettings()
            {
                faceColor = faceColor,
                outlineColor = outlineColor,
                outlineWidth = outlineWidth,
                underlayColor = textShadow.color,
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

        public static TextCore.WhiteSpace toTextCore(this WhiteSpace whiteSpace, bool isInputField)
        {
            if (isInputField)
            {
                return whiteSpace switch
                {
                    WhiteSpace.Normal or WhiteSpace.PreWrap => TextCore.WhiteSpace.PreWrap,
                    WhiteSpace.NoWrap or WhiteSpace.Pre => TextCore.WhiteSpace.Pre,
                    _ => TextCore.WhiteSpace.Pre
                };
            }
            return whiteSpace switch
            {
                WhiteSpace.Normal => TextCore.WhiteSpace.Normal,
                WhiteSpace.NoWrap => TextCore.WhiteSpace.NoWrap,
                WhiteSpace.PreWrap => TextCore.WhiteSpace.PreWrap,
                WhiteSpace.Pre => TextCore.WhiteSpace.Pre,
                _ => TextCore.WhiteSpace.Normal
            };
        }

        public static TextCore.TextOverflow toTextCore(this TextOverflow textOverflow, OverflowInternal overflow)
        {
            return textOverflow switch
            {
                TextOverflow.Ellipsis when overflow == OverflowInternal.Hidden => TextCore.TextOverflow.Ellipsis,
                _ => TextCore.TextOverflow.Clip
            };
        }
    }
}
