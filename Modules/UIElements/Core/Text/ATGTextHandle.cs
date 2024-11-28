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
        ATGTextEventHandler m_ATGTextEventHandler;
        // int LinkID: The identifier for the link.
        // TagType: Specifies the type of tag (either Hyperlink or Link).
        // string Attribute: For Hyperlink, this is the 'href' attribute; for Link, it's the associated attribute.
        List<(int, TagType, string)> m_Links;//Not clearing links would result in a leak of strings and enum, but no class, so no consideration for clearing the list at the moment
        private List<(int, TagType, string)> Links => m_Links ??= new();

        public void ComputeNativeTextSize(in RenderedText textToMeasure, float width, float height)
        {
            if (!ConvertUssToNativeTextGenerationSettings())
                return;
            nativeSettings.text = textToMeasure.CreateString();
            nativeSettings.screenWidth = float.IsNaN(width) ? Int32.MaxValue : (int)(width * 64.0f);
            nativeSettings.screenHeight = float.IsNaN(height) ? Int32.MaxValue : (int)(height * 64.0f);

            if (m_TextElement.enableRichText && !String.IsNullOrEmpty(nativeSettings.text))
            {
                Color hyperlinkColor =  (m_TextElement.panel as Panel)?.HyperlinkColor ?? Color.blue;
                RichTextTagParser.CreateTextGenerationSettingsArray(ref nativeSettings, Links, hyperlinkColor);
            }
            else
                nativeSettings.textSpans = null;

            // Passing a zero pointer instead of the cached textGenerationInfo because it is possible that calling the measure will not
            // change the size, there will therefore be no full layout of the glyph and the textGenerationInfo will not have the final
            // glyph position populated and it breaks TextLib.FindIntersectingLink
            preferredSize = TextLib.MeasureText(nativeSettings, IntPtr.Zero);
        }

        public NativeTextInfo UpdateNative(ref bool success)
        {
            if (!ConvertUssToNativeTextGenerationSettings())
            {
                success = false;
                return default;
            }

            success = true;

            if (m_TextElement.enableRichText && !String.IsNullOrEmpty(nativeSettings.text))
            {
                Color hyperlinkColor = (m_TextElement.panel as Panel)?.HyperlinkColor ?? Color.blue;
                RichTextTagParser.CreateTextGenerationSettingsArray(ref nativeSettings, Links, hyperlinkColor);
            }
            else
                nativeSettings.textSpans = null;

            if ((nativeSettings.hasLink) && textGenerationInfo == IntPtr.Zero)
            {
                textGenerationInfo = TextGenerationInfo.Create();
                m_ATGTextEventHandler ??= new ATGTextEventHandler(m_TextElement);
            }
            var textInfo = TextLib.GenerateText(nativeSettings, textGenerationInfo);
            UpdateATGTextEventHandler(nativeSettings);

            return textInfo;
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

        private void UpdateATGTextEventHandler(NativeTextGenerationSettings setting)
        {
            if (m_ATGTextEventHandler == null)
                return;

            var (hasLink, hasHyperlink) = hasLinkAndHyperlink();
            if (hasLink)
            {
                m_ATGTextEventHandler.RegisterLinkTagCallbacks();
            }
            else
            {
                m_ATGTextEventHandler.UnRegisterLinkTagCallbacks();
            }

            if (hasHyperlink)
            {
                m_ATGTextEventHandler.RegisterHyperlinkCallbacks();
            }
            else
            {
                m_ATGTextEventHandler.UnRegisterHyperlinkCallbacks();
            }
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
            nativeSettings.fontSize = (int)(style.fontSize.value > 0
                ? style.fontSize.value * 64.0f
                : fa.faceInfo.pointSize * 64.0f);
            nativeSettings.wordWrap = style.whiteSpace.toTextCore();
            nativeSettings.overflow = style.textOverflow.toTextCore(style.overflow);
            nativeSettings.horizontalAlignment = TextGeneratorUtilities.GetHorizontalAlignment(style.unityTextAlign);
            nativeSettings.verticalAlignment = TextGeneratorUtilities.GetVerticalAlignment(style.unityTextAlign);

            nativeSettings.color = style.color;
            nativeSettings.fontAsset = fa.nativeFontAsset;
            nativeSettings.languageDirection = m_TextElement.localLanguageDirection.toTextCore();
            nativeSettings.vertexPadding = (int)(GetVertexPadding(fa) * 64.0f);

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

            if (textSettings != null && textSettings.fallbackOSFontAssets != null)
            {
                foreach (var fallback in textSettings.fallbackOSFontAssets)
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

            nativeSettings.screenWidth = (int)(size.x * 64.0f);
            nativeSettings.screenHeight = (int)(size.y * 64.0f);

            return true;
        }


        private TextAsset GetICUAsset()
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

        internal protected TextLib TextLib
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (s_TextLib == null)
                {
                    s_TextLib = new TextLib(GetICUAsset().bytes);
                }
                return s_TextLib;
            }
        }


    }
}
