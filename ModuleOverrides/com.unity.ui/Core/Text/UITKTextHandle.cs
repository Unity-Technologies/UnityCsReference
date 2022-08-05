// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    internal class UITKTextHandle
    {
        public UITKTextHandle(TextElement te)
        {
            m_CurrentGenerationSettings = new UnityEngine.TextCore.Text.TextGenerationSettings();
            textHandle = new TextHandle();
            m_TextElement = te;
        }

        public Vector2 MeasuredSizes { get; set; }
        public Vector2 RoundedSizes { get; set; }

        TextElement m_TextElement;
        internal TextHandle textHandle;
        int m_PreviousGenerationSettingsHash;
        UnityEngine.TextCore.Text.TextGenerationSettings m_CurrentGenerationSettings;

        //static instance cached to minimize allocation
        static TextCore.Text.TextGenerationSettings s_LayoutSettings = new TextCore.Text.TextGenerationSettings();

        private bool isDirty;
        public void SetDirty()
        {
            isDirty = true;
        }

        public bool IsDirty()
        {
            int hash = m_CurrentGenerationSettings.GetHashCode();
            if (m_PreviousGenerationSettingsHash == hash && !isDirty)
                return false;

            m_PreviousGenerationSettingsHash = hash;
            isDirty = false;
            return true;
        }

        public Vector2 GetCursorPositionFromIndexUsingLineHeight(int index)
        {
            return textHandle.GetCursorPositionFromStringIndexUsingLineHeight(index);
        }

        public Vector2 GetCursorPositionFromIndexUsingCharacterHeight(int index)
        {
            return textHandle.GetCursorPositionFromStringIndexUsingCharacterHeight(index);
        }

        public float ComputeTextWidth(string textToMeasure, bool wordWrap, float width, float height)
        {
            ConvertUssToTextGenerationSettings(s_LayoutSettings);
            s_LayoutSettings.text = textToMeasure;
            s_LayoutSettings.screenRect = new Rect(0, 0, width, height);
            s_LayoutSettings.wordWrap = wordWrap;
            return textHandle.ComputeTextWidth(s_LayoutSettings);
        }

        public float ComputeTextHeight(string textToMeasure, float width, float height)
        {
            ConvertUssToTextGenerationSettings(s_LayoutSettings);
            s_LayoutSettings.text = textToMeasure;
            s_LayoutSettings.screenRect = new Rect(0, 0, width, height);
            return textHandle.ComputeTextHeight(s_LayoutSettings);
        }

        public float GetLineHeight(int lineNumber)
        {
            return textHandle.GetLineHeight(lineNumber);
        }

        public float GetLineHeightFromCharacterIndex(int index)
        {
            return textHandle.GetLineHeightFromCharacterIndex(index);
        }

        public float GetCharacterHeightFromIndex(int index)
        {
            return textHandle.GetCharacterHeightFromIndex(index);
        }

        public TextInfo Update()
        {
            ConvertUssToTextGenerationSettings(m_CurrentGenerationSettings);

            // The screenRect in TextCore is not properly implemented with regards to the offset part, so zero it out for now and we will add it ourselves later
            var size = m_TextElement.contentRect.size;

            // If the size is the last rounded size, we use the cached size before the rounding that was calculated
            if (Mathf.Abs(size.x - RoundedSizes.x) < 0.01f && Mathf.Abs(size.y - RoundedSizes.y) < 0.01f)
            {
                size = MeasuredSizes;
            }
            else
            {
                //the size has change, we need to save that information
                RoundedSizes = size;
                MeasuredSizes = size;
            }

            m_CurrentGenerationSettings.screenRect = new Rect(Vector2.zero, size);
            if (!IsDirty())
            {
                return textHandle.textInfo;
            }

            return textHandle.Update(m_CurrentGenerationSettings);
        }

        internal TextOverflowMode GetTextOverflowMode()
        {
            var style = m_TextElement.computedStyle;
            if (style.textOverflow == TextOverflow.Clip)
                return TextOverflowMode.Masking;

            if (style.textOverflow != TextOverflow.Ellipsis)
                return TextOverflowMode.Overflow;

            //Disable the ellipsis in text core so that it does not affect the render later.
            if (!TextLibraryCanElide())
                return TextOverflowMode.Masking;

            var wordWrap = style.whiteSpace == WhiteSpace.Normal;
            if (!wordWrap && style.overflow == OverflowInternal.Hidden)
                return TextOverflowMode.Ellipsis;

            return TextOverflowMode.Overflow;
        }


        internal void ConvertUssToTextGenerationSettings(UnityEngine.TextCore.Text.TextGenerationSettings tgs)
        {
            var style = m_TextElement.computedStyle;

            if (tgs.textSettings == null)
            {
                tgs.textSettings = TextUtilities.GetTextSettingsFrom(m_TextElement);
                if (tgs.textSettings == null)
                    return;
            }

            tgs.fontAsset = TextUtilities.GetFontAsset(m_TextElement);
            if (tgs.fontAsset == null)
                return;

            tgs.material = tgs.fontAsset.material;
            // The screenRect in TextCore is not properly implemented with regards to the offset part, so zero it out for now and we will add it ourselves later
            tgs.screenRect = new Rect(0, 0, m_TextElement.contentRect.width, m_TextElement.contentRect.height);
            tgs.text = m_TextElement.isElided && !TextLibraryCanElide() ? m_TextElement.elidedText : m_TextElement.renderedText;

            tgs.fontSize = style.fontSize.value > 0
                ? style.fontSize.value
                : tgs.fontAsset.faceInfo.pointSize;

            tgs.fontStyle = TextGeneratorUtilities.LegacyStyleToNewStyle(style.unityFontStyleAndWeight);
            tgs.textAlignment = TextGeneratorUtilities.LegacyAlignmentToNewAlignment(style.unityTextAlign);
            tgs.wordWrap = style.whiteSpace == WhiteSpace.Normal;

            tgs.wordWrappingRatio = 0.4f;
            tgs.richText = m_TextElement.enableRichText;
            tgs.overflowMode = GetTextOverflowMode();
            tgs.characterSpacing = style.letterSpacing.value;
            tgs.wordSpacing = style.wordSpacing.value;
            tgs.paragraphSpacing = style.unityParagraphSpacing.value;

            tgs.color = style.color;

            if (m_TextElement.panel?.contextType == ContextType.Editor)
                tgs.color *= UIElementsUtility.editorPlayModeTintColor;

            tgs.inverseYAxis = true;
        }

        internal bool TextLibraryCanElide()
        {
            // TextCore can only elide at the end
            return m_TextElement.computedStyle.unityTextOverflowPosition == TextOverflowPosition.End;
        }

    }

    internal static class TextUtilities
    {
        internal static Vector2 MeasureVisualElementTextSize(TextElement te, string textToMeasure, float width, VisualElement.MeasureMode widthMode, float height, VisualElement.MeasureMode heightMode)
        {
            float measuredWidth = float.NaN;
            float measuredHeight = float.NaN;

            if (textToMeasure == null || !IsFontAssigned(te))
                return new Vector2(measuredWidth, measuredHeight);

            float pixelsPerPoint = te.scaledPixelsPerPoint;

            if ( pixelsPerPoint <= 0)
                return Vector2.zero;

            if (widthMode == VisualElement.MeasureMode.Exactly)
            {
                measuredWidth = width;
            }
            else
            {
                measuredWidth = te.uitkTextHandle.ComputeTextWidth(textToMeasure, false, width, height);

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
                measuredHeight = te.uitkTextHandle.ComputeTextHeight(textToMeasure, width, height);

                if (heightMode == VisualElement.MeasureMode.AtMost)
                {
                    measuredHeight = Mathf.Min(measuredHeight, height);
                }
            }

            // Case 1215962: round up as yoga could decide to round down and text would start wrapping
            float roundedWidth = AlignmentUtils.CeilToPixelGrid(measuredWidth, pixelsPerPoint);
            float roundedHeight = AlignmentUtils.CeilToPixelGrid(measuredHeight, pixelsPerPoint);
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
                return textSettings.GetCachedFontAsset(ve.computedStyle.unityFontDefinition.font);
            return textSettings.GetCachedFontAsset(ve.computedStyle.unityFont);
        }

        internal static Font GetFont(VisualElement ve)
        {
            var style = ve.computedStyle;
            if (style.unityFontDefinition.font != null)
                return style.unityFontDefinition.font;
            if (style.unityFont != null)
                return style.unityFont;

            return style.unityFontDefinition.fontAsset?.sourceFontFile;
        }

        internal static bool IsFontAssigned(VisualElement ve)
        {
            return ve.computedStyle.unityFont != null || !ve.computedStyle.unityFontDefinition.IsEmpty();
        }

        internal static PanelTextSettings GetTextSettingsFrom(VisualElement ve)
        {
            if (ve.panel is RuntimePanel runtimePanel)
                return runtimePanel.panelSettings.textSettings ?? PanelTextSettings.defaultPanelTextSettings;
            return PanelTextSettings.defaultPanelTextSettings;
        }

        internal static TextCoreSettings GetTextCoreSettingsForElement(VisualElement ve)
        {
            var fontAsset = GetFontAsset(ve);
            if (fontAsset == null)
                return new TextCoreSettings();

            var resolvedStyle = ve.resolvedStyle;
            var computedStyle = ve.computedStyle;

            // Convert the text settings pixel units to TextCore relative units
            float paddingPercent = 1.0f / fontAsset.atlasPadding;
            float pointSizeRatio = ((float)fontAsset.faceInfo.pointSize) / ve.computedStyle.fontSize.value;
            float factor = paddingPercent * pointSizeRatio;

            float outlineWidth = Mathf.Max(0.0f, resolvedStyle.unityTextOutlineWidth * factor);
            float underlaySoftness = Mathf.Max(0.0f, computedStyle.textShadow.blurRadius * factor);
            Vector2 underlayOffset = computedStyle.textShadow.offset * factor;

            var faceColor = resolvedStyle.color;
            var outlineColor = resolvedStyle.unityTextOutlineColor;
            if (outlineWidth < UIRUtility.k_Epsilon)
                outlineColor.a = 0.0f;

            return new TextCoreSettings() {
                faceColor = faceColor,
                outlineColor = outlineColor,
                outlineWidth = outlineWidth,
                underlayColor = computedStyle.textShadow.color,
                underlayOffset = underlayOffset,
                underlaySoftness = underlaySoftness
            };
        }
    }
}
