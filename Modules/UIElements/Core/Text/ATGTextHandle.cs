// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.TextCore;
using UnityEngine.TextCore.Text;
using static UnityEngine.TextCore.RichTextTagParser;

namespace UnityEngine.UIElements
{
    internal partial class UITKTextHandle
    {
        internal ATGTextEventHandler m_ATGTextEventHandler;
        // int LinkID: The identifier for the link.
        // TagType: Specifies the type of tag (either Hyperlink or Link).
        // string Attribute: For Hyperlink, this is the 'href' attribute; for Link, it's the associated attribute.
        List<(int, TagType, string)> m_Links;//Not clearing links would result in a leak of strings and enum, but no class, so no consideration for clearing the list at the moment
        private List<(int, TagType, string)> Links => m_Links ??= new();
        internal Color atgHyperlinkColor = Color.blue;
        bool uvsAreGenerated = false;

        void ComputeNativeTextSize(in string textToMeasure, float width, VisualElement.MeasureMode widthMode, float height, VisualElement.MeasureMode heightMode, float? fontsize = null)
        {
            if (!ConvertUssToNativeTextGenerationSettings(textToMeasure, fontsize))
                return;

            // Insert zero width space to avoid TextField from collapsing when empty. UUM-90538
            if (string.IsNullOrEmpty(nativeSettings.text) && m_TextElement.isInputField)
                nativeSettings.text = "\u200B";

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

        public (NativeTextInfo, bool) UpdateNative(bool generateNativeSettings = true)
        {
            if (generateNativeSettings && !ConvertUssToNativeTextGenerationSettings())
                return (default, false);

            if (nativeSettings.hasLink)
            {
                m_TextElement.uitkTextHandle.CacheTextGenerationInfo();
                m_ATGTextEventHandler ??= new ATGTextEventHandler(m_TextElement);
            }

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

            m_IsElided = textInfo.isElided;
            return (textInfo, true);
        }

        public void CacheTextGenerationInfo()
        {
            if (!useAdvancedText)
            {
                Debug.LogError("CacheTextGenerationInfo should only be called for ATG.");
                return;

            }

            bool isCacheATG = m_TextHandleFlags.HasFlag(TextHandleFlags.IsCachedPermanentATG);
            if (isCacheATG)
                return;

            // We need to recreate it with the good memory labels
            if (textGenerationInfo != IntPtr.Zero)
            {
                TextGenerationInfo.Destroy(textGenerationInfo);
                textGenerationInfo = IntPtr.Zero;
            }

            IsCachedPermanentATG = true;
            textGenerationInfo = TextGenerationInfo.Create(IsCachedPermanent);
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

        public void ProcessMeshInfos(NativeTextInfo textInfo, ref List<List<List<int>>> textElementIndicesByMesh, ref List<bool> hasMultipleColorsByMesh)
        {
            textLib.ProcessMeshInfos(textInfo, nativeSettings, ref textElementIndicesByMesh, ref hasMultipleColorsByMesh, uvsAreGenerated);
            uvsAreGenerated = true;
        }

        public bool HasMissingGlyphs(NativeTextInfo textInfo, ref Dictionary<int, HashSet<uint>> missingGlyphsPerFontAsset)
        {
            return textLib.HasMissingGlyphs(textInfo, ref missingGlyphsPerFontAsset);
        }

        private (bool, bool) hasLinkAndHyperlink()
        {
            bool hasLink = false;
            bool hasHyperlink = false;

            if (m_Links != null) // Using member variable to not allocate if unused
            {
                foreach (var (_, type, _) in Links)
                {
                    hasLink = hasLink || type == TagType.Link;
                    hasHyperlink = hasHyperlink || type == TagType.Hyperlink;

                    if (hasLink && hasHyperlink)
                        break;
                }
            }
            return (hasLink, hasHyperlink);
        }

        internal (TagType, string) ATGFindIntersectingLink(Vector2 point)
        {

            //This should probably be public, but it would require exposing TagType
            Debug.Assert(useAdvancedText);
            if (textGenerationInfo == IntPtr.Zero)
            {
                Debug.LogError("TextGenerationInfo pointer is null.");
                return(TagType.Unknown, null);
            }

            int id = TextLib.FindIntersectingLink(point * GetPixelsPerPoint(), textGenerationInfo);

            if (id == -1)
                return (TagType.Unknown, null);

            return (m_Links[id].Item2,   m_Links[id].Item3);
        }

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
            var fa = TextUtilities.GetFontAsset(m_TextElement);

            if (fa == null)
                return;

            var style = m_TextElement.computedStyle;
            if (style.unityEditorTextRenderingMode == EditorTextRenderingMode.Bitmap)
            {
                var effectiveFontsize = (int)Math.Round((style.fontSize.value) * GetPixelsPerPoint(), MidpointRounding.AwayFromZero);
                nativeSettings.fontSize = effectiveFontsize * 64;
                fa = GetCorrespondingBitmapFontAsset(fa, effectiveFontsize);
            }
               
            TextUtilities.GetTextSettingsFrom(m_TextElement).UpdateNativeTextSettings();
            fa.EnsureNativeFontAssetIsCreated();
        }

#nullable enable
        internal bool ConvertUssToNativeTextGenerationSettings(string? textToMeasure = null, float? fontsize = null)
        {
            var scale = GetPixelsPerPoint();
            var style = m_TextElement.computedStyle;

            nativeSettings.preProcessFlags = PreProcessFlags.None;
            nativeSettings.text = m_TextElement.isElided && !TextLibraryCanElide() ? m_TextElement.elidedText : m_TextElement.renderedTextString;
            if (textToMeasure != null)
                nativeSettings.text = textToMeasure;
            if (nativeSettings.text == null)
                nativeSettings.text = "";

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

            var fa = TextUtilities.GetFontAsset(m_TextElement);
            if (fa == null)
                return false;

            if (style.unityEditorTextRenderingMode == EditorTextRenderingMode.Bitmap)
                fa = GetCorrespondingBitmapFontAsset(fa, (int)Math.Round(effectiveFontSize, MidpointRounding.AwayFromZero));

            if (fa.atlasPopulationMode == AtlasPopulationMode.Static)
            {
                Debug.LogError($"Advanced text system cannot render using static font asset {fa.faceInfo.familyName}");
                return false;
            }

            nativeSettings.vertexPadding = (int)(GetVertexPadding(fa) * 64.0f);
            nativeSettings.fontAsset = fa.nativeFontAsset;
            if (fa.nativeFontAsset == IntPtr.Zero)
                return false;
            nativeSettings.textSettings = TextUtilities.GetTextSettingsFrom(m_TextElement).nativeTextSettings;

            if (m_TextElement.enableRichText && RichTextTagParser.MayNeedParsing(nativeSettings.text))
            {
                // If we're not using richTextTags, we're doing this on the native side to avoid allocations.
                TextPreprocessor.PreProcessString(ref nativeSettings.text, nativeSettings.preProcessFlags);
                nativeSettings.preProcessFlags = PreProcessFlags.None;
                //TODO GetBlurryFontAssetMapping for other fonts in the rich text tags
                CreateTextGenerationSettingsArray(ref nativeSettings, Links, atgHyperlinkColor, GetPixelsPerPoint(), TextUtilities.GetTextSettingsFrom(m_TextElement));
            }
            else
                nativeSettings.textSpans = null;

            return true;
        }
#nullable restore

        internal void EnsureFontAssetsAreCreatedOnTheMainThread()
        {
            var fa = TextUtilities.GetFontAsset(m_TextElement);
            fa.EnsureNativeFontAssetIsCreated();
            var style = m_TextElement.computedStyle;

            if (fa == null || fa.atlasPopulationMode == AtlasPopulationMode.Static || style.unityEditorTextRenderingMode != EditorTextRenderingMode.Bitmap)
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

        TextAsset GetICUAsset()
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

        //This method uses the asset in the editor if avialable, or try to find any asset that would be included in the resource folder for builds
        internal static TextAsset GetICUAssetStaticFalback()
        {
            if (TextLib.GetICUAssetEditorDelegate != null)
            {
                //Editor will load the ICU library before the scene, so we need to check in the asset database as the asset may not be loaded yet.
                var asset = TextLib.GetICUAssetEditorDelegate();
                if (asset != null)
                    return asset;
            }
            else
            {
                Debug.LogError("GetICUAssetEditorDelegate is null");
            }
            // Dont know about the panelSettings class existence here so we must filter by name
            foreach (var t in Resources.FindObjectsOfTypeAll<UnityEngine.TextAsset>())
            {
                if (t.name == "icudt73l")
                    return t;
            }

            return null;
        }

        static TextLib s_TextLib;


        internal protected TextLib textLib
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                InitTextLib();
                return s_TextLib;
            }
        }

        internal protected void InitTextLib()
        {
            s_TextLib ??= new TextLib(GetICUAsset().bytes);
        }
    }


}
