// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.TextCore;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements
{
    internal partial class UITKTextHandle
    {
        internal ATGTextEventHandler m_ATGTextEventHandler;
        bool uvsAreGenerated = false;

        // Buffer for processed text that differs from TextElement.textBuffer
        // (password-masked, placeholder, elided)
        NativeTextBuffer m_ProcessedTextBuffer;

        internal unsafe bool TryGetSourceTextPointer(out IntPtr ptr, out int length)
        {
            if (m_TextElement.showPlaceholderText
                || m_TextElement.edition.isPassword
                || (m_TextElement.isElided && !TextLibraryCanElide()))
            {
                ptr = IntPtr.Zero;
                length = 0;
                return false;
            }

            ref var buffer = ref m_TextElement.textBuffer;
            if (!buffer.isCreated || buffer.length == 0)
            {
                ptr = IntPtr.Zero;
                length = 0;
                return true;
            }

            ptr = (IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(buffer.buffer);
            length = buffer.length;
            return true;
        }

        void ComputeNativeTextSize(in string textToMeasure, float width, VisualElement.MeasureMode widthMode, float height, VisualElement.MeasureMode heightMode, float? fontsize = null)
        {
            if (!ConvertUssToNativeTextGenerationSettings(textToMeasure, fontsize))
                return;

            // Insert zero width space to avoid TextField from collapsing when empty. UUM-90538
            if (nativeSettings.textBufferLength == 0 && m_TextElement.isInputField)
            {
                m_ProcessedTextBuffer.CopyFrom("\u200B");
                nativeSettings.SetTextBuffer(m_ProcessedTextBuffer.buffer, m_ProcessedTextBuffer.length);
            }

            if (widthMode == VisualElement.MeasureMode.Undefined || float.IsNaN(width) || float.IsNegative(width))
                nativeSettings.screenWidth = TextLib.k_unconstrainedScreenSize;
            else
                nativeSettings.screenWidth = (int)(width * 64.0f);

            if (heightMode == VisualElement.MeasureMode.Undefined || float.IsNaN(height) || float.IsNegative(height))
                nativeSettings.screenHeight = TextLib.k_unconstrainedScreenSize;
            else
                nativeSettings.screenHeight = (int)(height * 64.0f);

            if (textGenerationInfo == IntPtr.Zero)
            {
                textGenerationInfo = TextGenerationInfo.Create(IsCachedPermanent);
            }

            pixelPreferedSize = textLib.MeasureText(nativeSettings, textGenerationInfo);
        }

        public (NativeTextInfo, bool) UpdateNative()
        {
            if (!ConvertUssToNativeTextGenerationSettings())
                return (default, false);

            // This needs to be set for each textElement because we might need to come back on the main thread and reuse the informations after.
            // We clear it as soon as possible to avoid persistent native allocations.
            if (textGenerationInfo == IntPtr.Zero)
            {
                textGenerationInfo = TextGenerationInfo.Create(IsCachedPermanent);
            }

            bool wasCached = false;
            var textInfo = textLib.GenerateText(nativeSettings, textGenerationInfo, ref wasCached);
            if (!wasCached)
                uvsAreGenerated = false;

            // If there are links, we need to generate the text again and cache it
            if (m_TextElement.enableRichText)
            {
                SyncLinksFromNative();

                if (m_Links is { Length: > 0 } && !IsCachedPermanentATG)
                {
                    m_TextElement.uitkTextHandle.CacheTextGenerationInfo();
                    m_ATGTextEventHandler ??= new ATGTextEventHandler(m_TextElement);
                    textInfo = textLib.GenerateText(nativeSettings, textGenerationInfo, ref wasCached);
                }
            }
            else
            {
                m_Links = null;
            }

            m_IsElided = textInfo.isElided;
            return (textInfo, true);
        }

        public void ShapeText()
        {
            if (!ConvertUssToNativeTextGenerationSettings())
                return;

            if (textGenerationInfo == IntPtr.Zero)
            {
                textGenerationInfo = TextGenerationInfo.Create(IsCachedPermanent);
            }

            textLib.ShapeText(nativeSettings, textGenerationInfo);
        }

        public void ProcessMeshInfos(NativeTextInfo textInfo, ref List<List<List<int>>> textElementIndicesByMesh)
        {
            textLib.ProcessMeshInfos(textInfo, nativeSettings, ref textElementIndicesByMesh, uvsAreGenerated);
            uvsAreGenerated = true;
        }

        public bool HasMissingGlyphs(NativeTextInfo textInfo, ref Dictionary<EntityId, HashSet<uint>> missingGlyphsPerFontAsset)
        {
            return textLib.HasMissingGlyphs(textInfo, ref missingGlyphsPerFontAsset);
        }

        private (bool, bool) hasLinkAndHyperlink()
        {
            bool hasLink = false;
            bool hasHyperlink = false;

            var links = m_Links;
            if (links != null)
            {
                for (int i = 0; i < links.Length; i++)
                {
                    if (links[i].isHyperlink)
                        hasHyperlink = true;
                    else
                        hasLink = true;

                    if (hasLink && hasHyperlink)
                        break;
                }
            }
            return (hasLink, hasHyperlink);
        }

        // Test-only convenience: did the most recent rich-text parse expose an <a> (hyperlink) tag?
        internal bool hasHyperlinkTag => hasLinkAndHyperlink().Item2;

        // Needs to be called on the main thread
        internal void UpdateATGTextEventHandler()
        {
            if (m_ATGTextEventHandler == null)
                return;

            var (hasLink, hasHyperlink) = hasLinkAndHyperlink();
            if (hasLink)
                m_ATGTextEventHandler.RegisterLinkTagCallbacks();
            else
                m_ATGTextEventHandler.UnRegisterLinkTagCallbacks();

            if (hasHyperlink)
                m_ATGTextEventHandler.RegisterHyperlinkCallbacks();
            else
                m_ATGTextEventHandler.UnRegisterHyperlinkCallbacks();
        }

        internal void EnsureIsReadyForJobs()
        {
            InitTextLib();
            var fa = m_TextElement.cachedFontAsset;
            var textSettings = TextUtilities.GetTextSettingsFrom(m_TextElement);

            if (fa == null)
                fa = textSettings.GetFontAsset();

            if (fa == null)
                return;

            ref var style = ref m_TextElement.computedStyle;
            if (style.unityEditorTextRenderingMode == EditorTextRenderingMode.Bitmap)
            {
                var effectiveFontsize = (int)Math.Round((style.fontSize.value) * GetPixelsPerPoint(), MidpointRounding.AwayFromZero);
                nativeSettings.fontSize = effectiveFontsize * 64;
                fa = GetCorrespondingBitmapFontAsset(fa, effectiveFontsize);
            }
            textSettings.UpdateNativeTextSettings();
            fa.EnsureNativeFontAssetIsCreated();

            // Pre-allocate on the main thread; jobs cannot allocate Persistent memory.
            // Account for placeholder text which may be longer than the text buffer.
            int preAllocLength = m_TextElement.textBuffer.length;
            if (m_TextElement.showPlaceholderText)
                preAllocLength = Math.Max(preAllocLength, m_TextElement.edition.placeholder?.Length ?? 0);

            // When the backing NativeArray is not created (element attached with empty
            // text, or buffer disposed during a lifecycle transition) the direct-buffer
            // path in ConvertUssToNativeTextGenerationSettings falls through and copies
            // renderedTextString into m_ProcessedTextBuffer instead.  Pre-allocate
            // enough for that text so the Job never triggers a Persistent allocation.
            if (!m_TextElement.textBuffer.isCreated)
                preAllocLength = Math.Max(preAllocLength, m_TextElement.renderedTextString?.Length ?? 0);

            m_ProcessedTextBuffer.EnsureCapacity(preAllocLength);
        }

#nullable enable
        internal bool ConvertUssToNativeTextGenerationSettings(string? textToMeasure = null, float? fontsize = null)
        {
            var scale = GetPixelsPerPoint();
            ref var style = ref m_TextElement.computedStyle;
            var textSettings = TextUtilities.GetTextSettingsFrom(m_TextElement);

            nativeSettings.preProcessFlags = PreProcessFlags.None;

            bool useDirectBuffer = textToMeasure == null
                && !(m_TextElement.isElided && !TextLibraryCanElide())
                && !m_TextElement.showPlaceholderText
                && !m_TextElement.edition.isPassword;

            string? text = useDirectBuffer
                ? null
                : (textToMeasure
                    ?? (m_TextElement.isElided && !TextLibraryCanElide() ? m_TextElement.elidedText : m_TextElement.renderedTextString)
                    ?? "");

            var effectiveFontSize = (fontsize ?? style.fontSize.value) * scale;
            nativeSettings.fontSize = (int)Math.Round(effectiveFontSize * 64.0f, MidpointRounding.AwayFromZero);
            nativeSettings.bestFit = style.unityTextAutoSize.mode == TextAutoSizeMode.BestFit;
            nativeSettings.maxFontSize = (int)(style.unityTextAutoSize.maxSize.value * 64.0f * scale);
            nativeSettings.minFontSize = (int)(style.unityTextAutoSize.minSize.value * 64.0f * scale);

            nativeSettings.wordWrapEnabled = style.whiteSpace == WhiteSpace.Normal || style.whiteSpace == WhiteSpace.PreWrap;
            if (!m_TextElement.isInputField && (style.whiteSpace == WhiteSpace.NoWrap || style.whiteSpace == WhiteSpace.Normal))
                nativeSettings.preProcessFlags |= PreProcessFlags.CollapseWhiteSpaces;
            if (m_TextElement.parseEscapeSequences)
                nativeSettings.preProcessFlags |= PreProcessFlags.ParseEscapeSequences;
            nativeSettings.overflow = style.textOverflow.toTextCore(style.overflow, style.unityTextOverflowPosition);
            nativeSettings.horizontalAlignment = TextGeneratorUtilities.GetHorizontalAlignment(style.unityTextAlign);
            nativeSettings.verticalAlignment = TextGeneratorUtilities.GetVerticalAlignment(style.unityTextAlign);
            nativeSettings.characterSpacing = (int)(style.letterSpacing.value * 64.0f);
            nativeSettings.wordSpacing = (int)(style.wordSpacing.value * 64.0f);
            nativeSettings.paragraphSpacing = (int)(style.unityParagraphSpacing.value * 64.0f);

            nativeSettings.color = style.color;
            nativeSettings.color *= m_TextElement.playModeTintColor;

            nativeSettings.languageDirection = m_TextElement.localLanguageDirection.toTextCore();

            //Bold is not part of the font style in css and in text native, but it is in textCore/Uitk
            var sourcefontStyle = TextGeneratorUtilities.LegacyStyleToNewStyle(style.unityFontStyleAndWeight);
            nativeSettings.fontStyle = sourcefontStyle & ~FontStyles.Bold;
            //Backward compatibility with text core
            nativeSettings.fontWeight = (sourcefontStyle & FontStyles.Bold) == FontStyles.Bold ? TextFontWeight.Bold : TextFontWeight.Regular;

            // The screenRect in TextCore is not properly implemented with regards to the offset part, so zero it out for now and we will add it ourselves later
            var size = m_TextElement.contentRect.size;

            // If the size is the last rounded size, we use the cached size before the rounding that was calculated
            if (ATGMeasuredWidth.HasValue && Mathf.Abs(size.x - ATGRoundedWidth) < 0.01f && LastPixelPerPoint == scale)
            {
                size.x = ATGMeasuredWidth.Value;
            }
            else
            {
                //the size has change, we need to save that information
                ATGRoundedWidth = size.x;
                ATGMeasuredWidth = null;
            }

            // The Value should already be aligned (to the nearest pixel if the value is from the layout, to 1/64 pixels if
            // it is from a previous text measurement) but the float representation might be inexact with some scale factor.
            // Doing an extra round prevent problems unlike truncating.
            nativeSettings.screenWidth = Mathf.RoundToInt(size.x * 64.0f * scale);
            nativeSettings.screenHeight =  Mathf.RoundToInt(size.y * 64.0f * scale);

            var fa = m_TextElement.cachedFontAsset;
            if (fa == null)
            {
                fa = textSettings.GetFontAsset();
            }

            if (fa ==  null)
                return false;

            if (style.unityEditorTextRenderingMode == EditorTextRenderingMode.Bitmap)
                fa = GetCorrespondingBitmapFontAsset(fa, (int)Math.Round(effectiveFontSize, MidpointRounding.AwayFromZero));

            #pragma warning disable CS0618 // Type or member is obsolete
            if (fa.atlasPopulationMode == AtlasPopulationMode.Static)
            #pragma warning restore CS0618
            {
                Debug.LogError($"Advanced text system cannot render using static font asset {fa.faceInfo.familyName}. See <a href=\"https://docs.unity3d.com/Manual/ui-systems/migrate-static-font-assets.html\">migration guidance</a>.");
                return false;
            }

            nativeSettings.vertexPadding = (int)(GetVertexPadding(fa) * 64.0f);
            nativeSettings.fontAsset = fa.nativeFontAsset;
            if (fa.nativeFontAsset == IntPtr.Zero)
                return false;
            nativeSettings.textSettings = textSettings.nativeTextSettings;
            // TODO: We should expose this to user. Possibly disable it by default.
            nativeSettings.disableAdvancedFontFeatures = false;
            nativeSettings.richTextEnabled = m_TextElement.enableRichText;
            nativeSettings.hoveredTag = (HoveredTag)m_HoveredTag;
            nativeSettings.pixelsPerPointFixed64 = (int)Math.Round(GetPixelsPerPoint() * 64.0f);

            if (useDirectBuffer
                && TryGetSourceTextPointer(out IntPtr srcPtr, out int srcLen)
                && srcPtr != IntPtr.Zero)
            {
                nativeSettings.SetTextBuffer(m_TextElement.textBuffer.buffer, m_TextElement.textBuffer.length);
                return true;
            }

            // Fallback for placeholder / password / library-incompatible
            // elision / explicit textToMeasure: materialize into a managed
            // string and copy into the processed-text buffer.
            if (useDirectBuffer)
                text = m_TextElement.renderedTextString ?? "";

            text ??= string.Empty;

            m_ProcessedTextBuffer.CopyFrom(text);
            nativeSettings.SetTextBuffer(m_ProcessedTextBuffer.buffer, m_ProcessedTextBuffer.length);
            return true;
        }

        internal void SyncLinksFromNative()
        {
            if (textGenerationInfo == IntPtr.Zero)
                return;

            var links = NativeRichTextParser.GetAllLinks(textGenerationInfo);
            m_Links = (links != null && links.Length > 0) ? links : null;
        }
#nullable restore

        internal void EnsureFontAssetsAreCreatedOnTheMainThread()
        {
            var fa = m_TextElement.cachedFontAsset;
            if (fa == null)
            {
                var textSettings = TextUtilities.GetTextSettingsFrom(m_TextElement);
                fa = textSettings.GetFontAsset();
            }
            fa.EnsureNativeFontAssetIsCreated();
            ref var style = ref m_TextElement.computedStyle;

            #pragma warning disable CS0618 // Type or member is obsolete
            if (fa == null || fa.atlasPopulationMode == AtlasPopulationMode.Static || style.unityEditorTextRenderingMode != EditorTextRenderingMode.Bitmap)
            #pragma warning restore CS0618
                return;

            var scale = GetPixelsPerPoint();
            var effectiveFontsize = (int)Math.Round((style.fontSize.value) * scale, MidpointRounding.AwayFromZero);

            GetCorrespondingBitmapFontAsset(fa, effectiveFontsize);
            GenerateBitmapFallbackFontAssets(effectiveFontsize, TextCore.Text.TextGenerationSettings.IsEditorTextRenderingModeRaster());
        }

        FontAsset GetCorrespondingBitmapFontAsset(FontAsset fa, int effectiveFontsize)
        {
            if (fa == null)
                return null;

            if (TextCore.Text.TextGenerationSettings.IsEditorTextRenderingModeBitmap() && fa.IsEditorFont)
                fa = GetBlurryFontAssetMapping(effectiveFontsize, fa, TextCore.Text.TextGenerationSettings.IsEditorTextRenderingModeRaster());

            return fa;
        }

        internal override TextAsset GetICUAsset()
        {
            if (m_TextElement.panel is null)
                throw new InvalidOperationException("Text cannot be processed on elements not in a panel");

            if (m_TextElement.panel.contextType == ContextType.Editor)
                return GetICUAssetStaticFalback();
            var asset = ((PanelSettings)(((RuntimePanel)m_TextElement.panel).ownerObject)).m_ICUDataAsset;

            if (asset != null)
                return asset;

            asset = GetICUAssetStaticFalback();

            if (asset != null)
                return asset;

            Debug.LogError("ICU Data not available. The data should be automatically assigned to the PanelSettings in the editor if the advanced text option is enable in the project settings. It will not be present on PanelSettings created at runtime, so make sure the build contains at least one PanelSettings asset");
            return null;
        }

        public override void RemoveFromPermanentCacheATG()
        {
            if (IsCachedPermanentATG)
            {
                m_ATGTextEventHandler?.UnRegisterHyperlinkCallbacks();
            }
            m_ProcessedTextBuffer.Dispose();
            base.RemoveFromPermanentCacheATG();
        }

    }
}
