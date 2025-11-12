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

        private static Vector2 PostProcessMeasuredSize(TextElement te, Vector2 measuredSize, float width, VisualElement.MeasureMode widthMode, float height, VisualElement.MeasureMode heightMode, float pixelsPerPoint)
        {
            float measuredWidth = measuredSize.x;
            float measuredHeight = measuredSize.y;

            if (widthMode == VisualElement.MeasureMode.Exactly)
            {
                measuredWidth = width;
            }
            else if (widthMode == VisualElement.MeasureMode.AtMost)
            {
                measuredWidth = Mathf.Min(measuredWidth, width);
            }

            if (heightMode == VisualElement.MeasureMode.Exactly)
            {
                measuredHeight = height;
            }
            else if (heightMode == VisualElement.MeasureMode.AtMost)
            {
                measuredHeight = Mathf.Min(measuredHeight, height);
            }

            float roundedWidth = AlignmentUtils.CeilToPixelGrid(measuredWidth, pixelsPerPoint, 0.0f);
            float roundedHeight = AlignmentUtils.CeilToPixelGrid(measuredHeight, pixelsPerPoint, 0.0f);
            var roundedValues = new Vector2(roundedWidth, roundedHeight);

            if (IsAdvancedTextEnabledForElement(te))
            {
                te.uitkTextHandle.ATGMeasuredWidth = measuredWidth;
                te.uitkTextHandle.ATGRoundedWidth = roundedWidth;
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

        internal static Vector2 MeasureVisualElementTextSize(TextElement te, string textToMeasure, float width, VisualElement.MeasureMode widthMode, float height, VisualElement.MeasureMode heightMode, float? fontsize = null)
        {
            if (!IsFontAssigned(te))
                return new Vector2(float.NaN, float.NaN);

            float pixelsPerPoint = te.panel?.scaledPixelsPerPoint ?? 1.0f;
            if (pixelsPerPoint <= 0)
                return Vector2.zero;

            Vector2 measuredSize = Vector2.zero;
            if (widthMode != VisualElement.MeasureMode.Exactly || heightMode != VisualElement.MeasureMode.Exactly)
            {
                measuredSize = te.uitkTextHandle.ComputeTextSize(textToMeasure, width, widthMode, height, heightMode, fontsize);
            }

            return PostProcessMeasuredSize(te, measuredSize, width, widthMode, height, heightMode, pixelsPerPoint);
        }


        internal static Vector2 MeasureVisualElementTextSize(TextElement te, in RenderedText textToMeasure, float width, VisualElement.MeasureMode widthMode, float height, VisualElement.MeasureMode heightMode, float? fontsize = null)
        {
            if (!IsFontAssigned(te))
                return new Vector2(float.NaN, float.NaN);

            float pixelsPerPoint = te.panel?.scaledPixelsPerPoint ?? 1.0f;
            if (pixelsPerPoint <= 0)
                return Vector2.zero;

            Vector2 measuredSize = Vector2.zero;
            if (widthMode != VisualElement.MeasureMode.Exactly || heightMode != VisualElement.MeasureMode.Exactly)
            {
                measuredSize = te.uitkTextHandle.ComputeTextSize(textToMeasure, width, height, fontsize);
            }

            return PostProcessMeasuredSize(te, measuredSize, width, widthMode, height, heightMode, pixelsPerPoint);
        }

        internal static FontAsset GetFontAsset(VisualElement ve)
        {
            if (ve.computedStyle.unityFontDefinition.fontAsset != null)
                return ve.computedStyle.unityFontDefinition.fontAsset as FontAsset;

            var textSettings = GetTextSettingsFrom(ve);
            if (!Equals(ve.computedStyle.unityFontDefinition.font, null))
                return textSettings.GetCachedFontAsset(ve.computedStyle.unityFontDefinition.font);
            else if (!Equals(ve.computedStyle.unityFont, null))
                return textSettings.GetCachedFontAsset(ve.computedStyle.unityFont);
            else if (!Equals(textSettings, null))
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

        internal static bool IsAdvancedTextEnabledForPanel(IPanel panel)
        {
            var isAdvancedTextGeneratorEnabledOnProject = false;
            if (panel is RuntimePanel runtimePanel && !runtimePanel.panelSettings.disableNoThemeWarning)
                isAdvancedTextGeneratorEnabledOnProject = IsAdvancedTextEnabled?.Invoke() ?? false;
            else
                isAdvancedTextGeneratorEnabledOnProject = true;
            return isAdvancedTextGeneratorEnabledOnProject;
        }

        internal static bool IsAdvancedTextEnabledForElement(VisualElement ve)
        {
            if (ve == null)
                return false;
            var isAdvancedTextGeneratorEnabledOnTextElement = ve.computedStyle.unityTextGenerator == TextGeneratorType.Advanced;
            var isAdvancedTextGeneratorEnabledOnProject = isAdvancedTextGeneratorEnabledOnTextElement && IsAdvancedTextEnabledForPanel(ve.panel);
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

        public static TextWrappingMode toTextWrappingMode(this WhiteSpace whiteSpace, bool isSingleLineInputField)
        {
            if (isSingleLineInputField)
            {
                return whiteSpace switch
                {
                    WhiteSpace.Normal or WhiteSpace.NoWrap => TextWrappingMode.NoWrap,
                    WhiteSpace.Pre or WhiteSpace.PreWrap => TextWrappingMode.PreserveWhitespaceNoWrap,
                    _ => TextWrappingMode.NoWrap
                };
            }
            return whiteSpace switch
            {
                WhiteSpace.Normal => TextWrappingMode.Normal,
                WhiteSpace.NoWrap => TextWrappingMode.NoWrap,
                WhiteSpace.PreWrap => TextWrappingMode.PreserveWhitespace,
                WhiteSpace.Pre => TextWrappingMode.PreserveWhitespaceNoWrap,
                _ => TextWrappingMode.Normal
            };
        }

        public static TextCore.TextOverflow toTextCore(this TextOverflow textOverflow, OverflowInternal overflow, TextOverflowPosition position)
        {
            // TextCore and ATG do not support the middle and left ellipsis. In thoses case the TextElement will take care of the ellipsi.
            if (position != TextOverflowPosition.End)
                return TextCore.TextOverflow.Clip;

            return textOverflow switch
            {
                TextOverflow.Ellipsis when overflow == OverflowInternal.Hidden => TextCore.TextOverflow.Ellipsis,
                _ => TextCore.TextOverflow.Clip
            };
        }
    }
}
