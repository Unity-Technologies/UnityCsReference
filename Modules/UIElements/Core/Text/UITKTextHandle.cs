// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements
{
    internal class UITKTextHandle : TextHandle
    {
        public UITKTextHandle(TextElement te)
        {
            m_TextElement = te;
            m_TextEventHandler = new TextEventHandler(te);
        }

        public Vector2 MeasuredSizes { get; set; }
        public Vector2 RoundedSizes { get; set; }

        internal static Func<float, FontAsset, FontAsset> GetBlurryMapping;
        internal static Func<float, bool> CanGenerateFallbackFontAssets;
        internal TextEventHandler m_TextEventHandler;

        TextElement m_TextElement;
        static TextLib s_TextLib;

        static TextLib TextLib
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (s_TextLib == null)
                {
                    s_TextLib = new TextLib();
                }
                return s_TextLib;
            }
        }

        public Vector2 ComputeTextSize(in RenderedText textToMeasure, float width, float height)
        {
            ConvertUssToTextGenerationSettings();
            settings.renderedText = textToMeasure;
            settings.screenRect = new Rect(0, 0, width, height);
            UpdatePreferredValues(settings);
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

        public NativeTextInfo UpdateNative(ref bool success)
        {
            if (!ConvertUssToNativeTextGenerationSettings())
            {
                success = false;
                return new NativeTextInfo();
            }

            if (m_PreviousNativeGenerationSettingsHash != nativeSettings.GetHashCode())
                nativeTextInfo = TextLib.GenerateText(nativeSettings);

            success = true;
            return nativeTextInfo;
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

        internal bool ConvertUssToNativeTextGenerationSettings()
        {
            var fa = TextUtilities.GetFontAsset(m_TextElement);
            if (fa.atlasPopulationMode == AtlasPopulationMode.Static)
            {
                Debug.LogError($"Advanced text system cannot render using static font asset {fa.name}");
                return false;
            }
            var style = m_TextElement.computedStyle;
            var renderedText = m_TextElement.isElided && !TextLibraryCanElide() ?
                new RenderedText(m_TextElement.elidedText) : m_TextElement.renderedText;
            nativeSettings.text = renderedText.CreateString();
            nativeSettings.screenWidth = (int)(m_TextElement.contentRect.width * 64);
            nativeSettings.screenHeight = (int)(m_TextElement.contentRect.height * 64);
            nativeSettings.fontSize = style.fontSize.value > 0
                ? style.fontSize.value
                : fa.faceInfo.pointSize;
            nativeSettings.wordWrap = m_TextElement.computedStyle.whiteSpace.toTextCore();
            nativeSettings.horizontalAlignment = TextGeneratorUtilities.GetHorizontalAlignment(style.unityTextAlign);
            nativeSettings.verticalAlignment = TextGeneratorUtilities.GetVerticalAlignment(style.unityTextAlign);

            nativeSettings.color = m_TextElement.computedStyle.color;
            nativeSettings.fontAsset = fa.nativeFontAsset;
            nativeSettings.languageDirection = m_TextElement.localLanguageDirection.toTextCore();

            var textSettings = TextUtilities.GetTextSettingsFrom(m_TextElement);
            List<IntPtr> globalFontAssetFallbacks = new List<IntPtr>();
            if (textSettings != null && textSettings.fallbackFontAssets != null)
            {
                foreach (var fallback in textSettings.fallbackFontAssets)
                {
                    if (fallback == null)
                        continue;
                    if (fallback.atlasPopulationMode == AtlasPopulationMode.Static && fallback.characterTable.Count > 0)
                    {
                        Debug.LogWarning($"Advanced text system cannot use static font asset {fallback.name} as fallback.");
                        continue;
                    }
                    globalFontAssetFallbacks.Add(fallback.nativeFontAsset);
                }
            }

            if (textSettings != null && textSettings.emojiFallbackTextAssets != null)
            {
                foreach (FontAsset fallback in textSettings.emojiFallbackTextAssets)
                {
                    if (fallback == null)
                        continue;
                    if (fallback.atlasPopulationMode == AtlasPopulationMode.Static && fallback.characterTable.Count > 0)
                    {
                        Debug.LogWarning($"Advanced text system cannot use static font asset {fallback.name} as fallback.");
                        continue;
                    }
                    globalFontAssetFallbacks.Add(fallback.nativeFontAsset);
                }
            }
            nativeSettings.globalFontAssetFallbacks = globalFontAssetFallbacks.ToArray();
            nativeSettings.fontStyle = TextGeneratorUtilities.LegacyStyleToNewStyle(style.unityFontStyleAndWeight);
            return true;
        }

        internal virtual bool ConvertUssToTextGenerationSettings()
        {
            var style = m_TextElement.computedStyle;
            var tgs = settings;
            tgs.textSettings = TextUtilities.GetTextSettingsFrom(m_TextElement);
            if (tgs.textSettings == null)
                return true;

            tgs.fontAsset = TextUtilities.GetFontAsset(m_TextElement);
            if (tgs.fontAsset == null)
                return true;

            // The screenRect in TextCore is not properly implemented with regards to the offset part, so zero it out for now and we will add it ourselves later
            tgs.screenRect = new Rect(0, 0, m_TextElement.contentRect.width, m_TextElement.contentRect.height);
            tgs.extraPadding = GetTextEffectPadding(tgs.fontAsset);
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
            tgs.isIMGUI = false;

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
        internal float GetTextEffectPadding(FontAsset fontAsset)
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

            var factor = TextUtilities.ConvertPixelUnitsToTextCoreRelativeUnits(m_TextElement, fontAsset);
            var gradientScale = fontAsset.atlasPadding + 1;

            return Mathf.Min(padding * factor * gradientScale, gradientScale);
        }

    }
}
