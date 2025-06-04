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

        void ComputeNativeTextSize(in RenderedText textToMeasure, float width, float height)
        {
            if (!ConvertUssToNativeTextGenerationSettings())
                return;

            // Insert zero width space to avoid TextField from collapsing when empty. UUM-90538
            nativeSettings.text = textToMeasure.valueLength > 0 ? textToMeasure.CreateString() : "\u200B";
            nativeSettings.screenWidth = float.IsNaN(width) ? TextLib.k_unconstrainedScreenSize : (int)(width * 64.0f);
            nativeSettings.screenHeight = float.IsNaN(height) ? TextLib.k_unconstrainedScreenSize : (int)(height * 64.0f);

            if (m_TextElement.enableRichText && !String.IsNullOrEmpty(nativeSettings.text))
            {
                CreateTextGenerationSettingsArray(ref nativeSettings, Links, atgHyperlinkColor);
            }
            else
                nativeSettings.textSpans = null;

            // Passing a zero pointer instead of the cached textGenerationInfo because it is possible that calling the measure will not
            // change the size, there will therefore be no full layout of the glyph and the textGenerationInfo will not have the final
            // glyph position populated and it breaks TextLib.FindIntersectingLink
            pixelPreferedSize = textLib.MeasureText(nativeSettings, IntPtr.Zero);
        }

        public (NativeTextInfo, bool) UpdateNative(bool generateNativeSettings = true)
        {
            if (generateNativeSettings && !ConvertUssToNativeTextGenerationSettings())
                return (default, false);

            if (m_TextElement.enableRichText && !String.IsNullOrEmpty(nativeSettings.text))
            {
                CreateTextGenerationSettingsArray(ref nativeSettings, Links, atgHyperlinkColor);
            }
            else
                nativeSettings.textSpans = null;

            if ((nativeSettings.hasLink) && textGenerationInfo == IntPtr.Zero)
            {
                textGenerationInfo = TextGenerationInfo.Create();
                m_ATGTextEventHandler ??= new ATGTextEventHandler(m_TextElement);
            }
            var textInfo = textLib.GenerateText(nativeSettings, textGenerationInfo);
            return (textInfo, true);
        }

        public void ProcessMeshInfos(NativeTextInfo textInfo)
        {
            textLib.ProcessMeshInfos(textInfo, nativeSettings);
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

            int id = TextLib.FindIntersectingLink(point, textGenerationInfo);

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

        internal bool ConvertUssToNativeTextGenerationSettings()
        {
            var fa = TextUtilities.GetFontAsset(m_TextElement);
            if (fa.atlasPopulationMode == AtlasPopulationMode.Static)
            {
                Debug.LogError($"Advanced text system cannot render using static font asset {fa.faceInfo.familyName}");
                return false;
            }

            var scale = GetPixelsPerPoint();

            var style = m_TextElement.computedStyle;
            nativeSettings.textSettings = TextUtilities.GetTextSettingsFrom(m_TextElement).nativeTextSettings;
            var renderedText = m_TextElement.isElided && !TextLibraryCanElide() ?
                new RenderedText(m_TextElement.elidedText) : m_TextElement.renderedText;
            nativeSettings.text = renderedText.CreateString();

            nativeSettings.fontSize = (int)(style.fontSize.value * 64.0f *scale);
            nativeSettings.bestFit = style.unityTextAutoSize.mode == TextAutoSizeMode.BestFit;
            nativeSettings.maxFontSize = (int)(style.unityTextAutoSize.maxSize.value * 64.0f * scale);
            nativeSettings.minFontSize = (int)(style.unityTextAutoSize.minSize.value * 64.0f * scale);

            nativeSettings.wordWrap = style.whiteSpace.toTextCore(m_TextElement.isInputField);
            nativeSettings.overflow = style.textOverflow.toTextCore(style.overflow);
            nativeSettings.horizontalAlignment = TextGeneratorUtilities.GetHorizontalAlignment(style.unityTextAlign);
            nativeSettings.verticalAlignment = TextGeneratorUtilities.GetVerticalAlignment(style.unityTextAlign);
            nativeSettings.characterSpacing = (int)(style.letterSpacing.value * 64.0f);
            nativeSettings.wordSpacing = (int)(style.wordSpacing.value * 64.0f);
            nativeSettings.paragraphSpacing = (int)(style.unityParagraphSpacing.value * 64.0f);

            nativeSettings.color = style.color;
            nativeSettings.fontAsset = fa.nativeFontAsset;
            nativeSettings.languageDirection = m_TextElement.localLanguageDirection.toTextCore();
            nativeSettings.vertexPadding = (int)(GetVertexPadding(fa) * 64.0f);

            //Bold is not part of the font style in css and in text native, but it is in textCore/Uitk
            var sourcefontStyle = TextGeneratorUtilities.LegacyStyleToNewStyle(style.unityFontStyleAndWeight);
            nativeSettings.fontStyle = sourcefontStyle & ~FontStyles.Bold;
            //Backward compatibility with text core
            nativeSettings.fontWeight = (sourcefontStyle & FontStyles.Bold) == FontStyles.Bold ? TextFontWeight.Bold : TextFontWeight.Regular;

            // The screenRect in TextCore is not properly implemented with regards to the offset part, so zero it out for now and we will add it ourselves later
            var size = m_TextElement.contentRect.size;

            // If the size is the last rounded size, we use the cached size before the rounding that was calculated
            if (Mathf.Abs(size.x - ATGRoundedSizes.x) < 0.01f && Mathf.Abs(size.y - ATGRoundedSizes.y) < 0.01f)
            {
                size = ATGMeasuredSizes;
            }
            else
            {
                //the size has change, we need to save that information
                ATGRoundedSizes = size;
                ATGMeasuredSizes = size;
            }

            nativeSettings.screenWidth = (int)(size.x * 64.0f * scale);
            nativeSettings.screenHeight = (int)(size.y * 64.0f * scale);

            return true;
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
