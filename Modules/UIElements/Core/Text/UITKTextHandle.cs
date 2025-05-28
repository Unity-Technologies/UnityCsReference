// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements.Internal;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    internal partial class UITKTextHandle : TextHandle
    {
        public UITKTextHandle(TextElement te)
        {
            m_TextElement = te;
            m_TextEventHandler = new TextEventHandler(te);
        }

        protected override float GetPixelsPerPoint()
        {
            return m_TextElement?.scaledPixelsPerPoint ?? 1;
        }

        // Should be set every time we set the measured/rounded sizes
        internal float LastPixelPerPoint { get; set; }
        public override void SetDirty()
        {
            // This is called for live reload, but not for property changes or when the editor text is change from sdf to bitmap
            MeasuredWidth = null;
            base.SetDirty();
        }
        /// <summary>
        /// Stored in Scaled/GUI pixels size
        /// Nullable to distinguish explicitly cases where we don't want to store the generation size (or discard the previous one).
        /// </summary>
        internal float? MeasuredWidth { get; set; }

        /// <summary>
        /// Stored in Scaled/GUI pixels size
        /// </summary>
        internal float RoundedWidth { get; set; }

        /// <summary>
        /// Stored in Scaled/GUI pixels size
        /// </summary>
        internal Vector2 ATGMeasuredSizes { get; set; }

        /// <summary>
        /// Stored in Scaled/GUI pixels size
        /// </summary>
        internal Vector2 ATGRoundedSizes { get; set; }


        internal static Func<int, FontAsset, bool, FontAsset> GetBlurryFontAssetMapping;
        internal static Func<int, bool, bool> CanGenerateFallbackFontAssets;
        internal TextEventHandler m_TextEventHandler;

        protected TextElement m_TextElement;

        public Vector2 ComputeTextSize(in RenderedText textToMeasure, float width, float height)
        {
            var scale = GetPixelsPerPoint();
            width = Mathf.Round(width * scale);
            height = Mathf.Round(height * scale);

            if (TextUtilities.IsAdvancedTextEnabledForElement(m_TextElement))
            {
                ComputeNativeTextSize(textToMeasure, width, height);
            }
            else
            {
                ConvertUssToTextGenerationSettings(populateScreenRect:false);
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
            m_TextEventHandler?.HandleATag();
        }

        public void HandleLinkTag()
        {
            m_TextEventHandler?.HandleLinkTag();
        }

        public void HandleLinkAndATagCallbacks()
        {
            m_TextEventHandler?.HandleLinkAndATagCallbacks();
        }

        public void UpdateMesh()
        {
            ConvertUssToTextGenerationSettings(populateScreenRect: true);
            var hashCode = settings.GetHashCode();

            // this should be a dynamic asset
            if (m_PreviousGenerationSettingsHash == hashCode && !isDirty)
                AddTextInfoToTemporaryCache(hashCode);
            else
            {
                RemoveTextInfoFromTemporaryCache();
                UpdateWithHash(hashCode);
            }
        }

        public override void AddTextInfoToPermanentCache()
        {
            if (useAdvancedText)
            {
                if (textGenerationInfo == IntPtr.Zero)
                    textGenerationInfo = TextGenerationInfo.Create();
                UpdateNative();
                UpdateATGTextEventHandler();
                return;
            }

            if (ConvertUssToTextGenerationSettings(populateScreenRect: true))
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

        internal virtual bool ConvertUssToTextGenerationSettings(bool populateScreenRect)
        {
            var style = m_TextElement.computedStyle;
            var tgs = settings;
            tgs.text = string.Empty;
            tgs.isIMGUI = false;
            tgs.textSettings = TextUtilities.GetTextSettingsFrom(m_TextElement);
            if (tgs.textSettings == null)
                return false;

            tgs.fontAsset = TextUtilities.GetFontAsset(m_TextElement);
            if (tgs.fontAsset == null)
                return false;

            tgs.extraPadding = GetVertexPadding(tgs.fontAsset);
            tgs.renderedText = m_TextElement.isElided && !TextLibraryCanElide() ?
                new RenderedText(m_TextElement.elidedText) : m_TextElement.renderedText;
            tgs.isPlaceholder = m_TextElement.showPlaceholderText;

            var uiScale = GetPixelsPerPoint();
            //this rounding should be moved to the resolved style so user could get the result...
            tgs.fontSize = (int)Math.Round(((style.fontSize.value * uiScale)), MidpointRounding.AwayFromZero);

            tgs.fontStyle = TextGeneratorUtilities.LegacyStyleToNewStyle(style.unityFontStyleAndWeight);
            
            // When we render in bitmap mode, we need provide proper coordinates to textCore so that the alignment is done properly
            // The output of freetype is in pixels corrdinate on screen, unlike UIToolkit.
            var shouldRenderBitmap = TextCore.Text.TextGenerationSettings.IsEditorTextRenderingModeBitmap() && style.unityEditorTextRenderingMode == EditorTextRenderingMode.Bitmap && tgs.fontAsset.IsEditorFont;
            if (shouldRenderBitmap)
            {
                
                // ScalePixelsPerPoint is invalid if the VisualElement is not in a panel
                FontAsset fa = GetBlurryFontAssetMapping(tgs.fontSize, tgs.fontAsset, TextCore.Text.TextGenerationSettings.IsEditorTextRenderingModeRaster());

                // Fallbacks also need to be generated on the Main Thread
                var canGenerateFallbacks = CanGenerateFallbackFontAssets(tgs.fontSize, TextCore.Text.TextGenerationSettings.IsEditorTextRenderingModeRaster());
                if (!canGenerateFallbacks || !fa)
                    return false;


                tgs.fontAsset = fa;
               
            }

            tgs.textAlignment = TextGeneratorUtilities.LegacyAlignmentToNewAlignment(style.unityTextAlign);

            tgs.textWrappingMode = style.whiteSpace.toTextWrappingMode();
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
            tgs.emojiFallbackSupport = m_TextElement.emojiFallbackSupport;


            if (populateScreenRect)
            {
                var size = m_TextElement.contentRect.size;

                // If the size is the last rounded size, we use the cached size before the rounding that was calculated
                if (MeasuredWidth.HasValue && Mathf.Abs(size.x - RoundedWidth) < 0.01f && LastPixelPerPoint == uiScale)
                {
                    size.x = MeasuredWidth.Value;
                }
                else
                {
                    //the size has changed, we need to discard previous measurement
                    RoundedWidth = size.x;
                    MeasuredWidth = null; 
                    LastPixelPerPoint = uiScale;
                }

                size.x *= uiScale;
                size.y *= uiScale;

                if (tgs.fontAsset.IsBitmap())
                {
                    size.x = Mathf.Round(size.x);
                    size.y = Mathf.Round(size.y);
                }

                tgs.screenRect = new Rect(Vector2.zero, size);
            }

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

        private bool wasAdvancedTextEnabledForElement;
        internal override bool IsAdvancedTextEnabledForElement()
        {
            return TextUtilities.IsAdvancedTextEnabledForElement(m_TextElement);
        }

        internal void ReleaseResourcesIfPossible()
        {
            bool usesATG = TextUtilities.IsAdvancedTextEnabledForElement(m_TextElement);
            if (wasAdvancedTextEnabledForElement && !usesATG && textGenerationInfo != IntPtr.Zero)
            {
                TextGenerationInfo.Destroy(textGenerationInfo);
                textGenerationInfo = IntPtr.Zero;
                m_ATGTextEventHandler?.OnDestroy();
                m_ATGTextEventHandler = null;
                m_TextEventHandler = new TextEventHandler(m_TextElement);
            }
            else if (!wasAdvancedTextEnabledForElement && usesATG)
            {
                s_PermanentCache.RemoveTextInfoFromCache(this);
                s_TemporaryCache.RemoveTextInfoFromCache(this);
                m_TextEventHandler?.OnDestroy();
                m_TextEventHandler = null;
                m_ATGTextEventHandler = new ATGTextEventHandler(m_TextElement);
            }
            wasAdvancedTextEnabledForElement = usesATG;
        }

        public override bool IsPlaceholder
        {
            get => useAdvancedText ? m_TextElement.showPlaceholderText : base.IsPlaceholder;
        }

        public bool IsElided()
        {
            if (string.IsNullOrEmpty(m_TextElement.text)) // impossible to differentiate between an empty string and a fully truncated string.
                return true;

            return m_IsEllided;
        }
    }
}
