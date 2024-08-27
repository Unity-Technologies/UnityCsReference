// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements
{
    internal partial class UITKTextHandle : TextHandle
    {
        public UITKTextHandle(TextElement te)
        {
            m_TextElement = te;
            m_TextEventHandler = new TextEventHandler(te);
        }

        public Vector2 MeasuredSizes { get; set; }
        public Vector2 RoundedSizes { get; set; }
        public Vector2 ATGMeasuredSizes { get; set; }
        public Vector2 ATGRoundedSizes { get; set; }

        internal static Func<float, FontAsset, FontAsset> GetBlurryMapping;
        internal static Func<float, bool> CanGenerateFallbackFontAssets;
        internal TextEventHandler m_TextEventHandler;

        protected TextElement m_TextElement;

        public Vector2 ComputeTextSize(in RenderedText textToMeasure, float width, float height)
        {
            if (TextUtilities.IsAdvancedTextEnabledForElement(m_TextElement))
            {
                ComputeNativeTextSize(textToMeasure, width, height);
            }
            else
            {
                ConvertUssToTextGenerationSettings();
                settings.renderedText = textToMeasure;
                settings.screenRect = new Rect(0, 0, width, height);
                UpdatePreferredValues(settings);
            }
            return preferredSize;
        }

        public void ComputeSettingsAndUpdate()
        {
            UpdateMesh();

            HandleATag();
            HandleLinkTag();
            HandleLinkAndATagCallbacks();
        }

        public void HandleATag()
        {
            m_TextEventHandler.HandleATag();
        }

        public void HandleLinkTag()
        {
            m_TextEventHandler.HandleLinkTag();
        }

        public void HandleLinkAndATagCallbacks()
        {
            m_TextEventHandler.HandleLinkAndATagCallbacks();
        }

        public void UpdateMesh()
        {
            ConvertUssToTextGenerationSettings();
            var hashCode = settings.GetHashCode();

            // this should be a dynamic asset
            if (m_PreviousGenerationSettingsHash == hashCode)
                AddTextInfoToTemporaryCache(hashCode);
            else
            {
                RemoveTextInfoFromTemporaryCache();
                UpdateWithHash(hashCode);
            }
        }

        public override void AddTextInfoToPermanentCache()
        {
            ConvertUssToTextGenerationSettings();
            base.AddTextInfoToPermanentCache();
        }

        TextOverflowMode GetTextOverflowMode()
        {
            var style = m_TextElement.computedStyle;
            if (style.textOverflow == TextOverflow.Clip)
                return TextOverflowMode.Masking;

            if (style.textOverflow != TextOverflow.Ellipsis)
                return TextOverflowMode.Overflow;

            //Disable the ellipsis in text core so that it does not affect the render later.
            if (!TextLibraryCanElide())
                return TextOverflowMode.Masking;

            if (style.overflow == OverflowInternal.Hidden)
                return TextOverflowMode.Ellipsis;

            return TextOverflowMode.Overflow;
        }

        internal virtual bool ConvertUssToTextGenerationSettings()
        {
            var style = m_TextElement.computedStyle;
            var tgs = settings;
            tgs.text = string.Empty;
            tgs.isIMGUI = false;
            tgs.textSettings = TextUtilities.GetTextSettingsFrom(m_TextElement);
            if (tgs.textSettings == null)
                return true;

            tgs.fontAsset = TextUtilities.GetFontAsset(m_TextElement);
            if (tgs.fontAsset == null)
                return true;

            // The screenRect in TextCore is not properly implemented with regards to the offset part, so zero it out for now and we will add it ourselves later
            tgs.screenRect = new Rect(0, 0, m_TextElement.contentRect.width, m_TextElement.contentRect.height);
            tgs.extraPadding = GetVertexPadding(tgs.fontAsset);
            tgs.renderedText = m_TextElement.isElided && !TextLibraryCanElide() ?
                new RenderedText(m_TextElement.elidedText) : m_TextElement.renderedText;
            tgs.isPlaceholder = m_TextElement.showPlaceholderText;

            tgs.fontSize = style.fontSize.value > 0
                ? style.fontSize.value
                : tgs.fontAsset.faceInfo.pointSize;

            tgs.fontStyle = TextGeneratorUtilities.LegacyStyleToNewStyle(style.unityFontStyleAndWeight);


            var shouldRenderBitmap = TextCore.Text.TextGenerationSettings.IsEditorTextRenderingModeBitmap() && style.unityEditorTextRenderingMode == EditorTextRenderingMode.Bitmap && tgs.fontAsset.IsEditorFont;
            if (shouldRenderBitmap)
            {
                // ScalePixelsPerPoint is invalid if the VisualElement is not in a panel
                if (m_TextElement.panel != null)
                    settings.pixelsPerPoint = m_TextElement.scaledPixelsPerPoint;

                var pixelSize = tgs.fontSize * settings.pixelsPerPoint;
                FontAsset fa = GetBlurryMapping(pixelSize, tgs.fontAsset);

                // Fallbacks also need to be generated on the Main Thread
                var canGenerateFallbacks = CanGenerateFallbackFontAssets(pixelSize);
                if (!canGenerateFallbacks || !fa)
                    return false;

                settings.fontAsset = fa;
            }
            settings.isEditorRenderingModeBitmap = shouldRenderBitmap;

            tgs.material = tgs.fontAsset.material;

            tgs.textAlignment = TextGeneratorUtilities.LegacyAlignmentToNewAlignment(style.unityTextAlign);

            tgs.textWrappingMode = style.whiteSpace.toTextWrappingMode();
            tgs.wordWrappingRatio = 0.4f;
            tgs.richText = m_TextElement.enableRichText;
            tgs.overflowMode = GetTextOverflowMode();
            tgs.characterSpacing = style.letterSpacing.value;
            tgs.wordSpacing = style.wordSpacing.value;
            tgs.paragraphSpacing = style.unityParagraphSpacing.value;
            tgs.color = style.color;
            tgs.color *= m_TextElement.playModeTintColor;
            tgs.shouldConvertToLinearSpace = false;
            tgs.parseControlCharacters = m_TextElement.parseEscapeSequences;
            tgs.isRightToLeft = m_TextElement.localLanguageDirection == LanguageDirection.RTL;
            tgs.inverseYAxis = true;
            tgs.fontFeatures = m_ActiveFontFeatures;
            tgs.emojiFallbackSupport = m_TextElement.emojiFallbackSupport;

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

            tgs.screenRect = new Rect(Vector2.zero, size);
            return true;
        }

        internal bool TextLibraryCanElide()
        {
            // TextCore can only elide at the end
            return m_TextElement.computedStyle.unityTextOverflowPosition == TextOverflowPosition.End;
        }

        internal static readonly float k_MinPadding = 6.0f;

        // Function to determine how much extra padding is required as a result of text effect like outline thickness, shadow etc... (see UUM-9524).
        internal float GetVertexPadding(FontAsset fontAsset)
        {
            float horizontalPadding;
            float verticalPadding;

            var style = m_TextElement.computedStyle;

            // Grow half inside and half outside
            float outlineThickness = style.unityTextOutlineWidth / 2.0f;

            // Text Shadow is not additive to outline thickness
            float offsetX = Mathf.Abs(style.textShadow.offset.x);
            float offsetY = Mathf.Abs(style.textShadow.offset.y);
            float blurRadius = Mathf.Abs(style.textShadow.blurRadius);

            if (outlineThickness <= 0.0f && offsetX <= 0.0f && offsetY <= 0.0f && blurRadius <= 0.0f)
                return k_MinPadding;

            horizontalPadding = Mathf.Max(offsetX + blurRadius, outlineThickness);
            verticalPadding = Mathf.Max(offsetY + blurRadius, outlineThickness);

            var padding = Mathf.Max(horizontalPadding, verticalPadding) + k_MinPadding;

            var factor = ConvertPixelUnitsToTextCoreRelativeUnits(style.fontSize.value, fontAsset);
            var gradientScale = fontAsset.atlasPadding + 1;

            return Mathf.Min(padding * factor * gradientScale, gradientScale);
        }

    }
}
