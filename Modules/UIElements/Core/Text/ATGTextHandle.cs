// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements
{
    internal partial class UITKTextHandle
    {
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

        public void ComputeNativeTextSize(in RenderedText textToMeasure, float width, float height)
        {
            ConvertUssToNativeTextGenerationSettings();
            nativeSettings.text = textToMeasure.CreateString();
            nativeSettings.screenWidth = float.IsNaN(width) ? Int32.MaxValue : (int)(width * 64.0f);
            nativeSettings.screenHeight = float.IsNaN(height) ? Int32.MaxValue : (int)(height * 64.0f);

            preferredSize = TextLib.MeasureText(nativeSettings);
        }

        public NativeTextInfo UpdateNative(ref bool success)
        {
            if (!ConvertUssToNativeTextGenerationSettings())
            {
                success = false;
                return default;
            }

            if (m_PreviousNativeGenerationSettingsHash != nativeSettings.GetHashCode())
                nativeTextInfo = TextLib.GenerateText(nativeSettings);

            success = true;
            return nativeTextInfo;
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
            nativeSettings.fontSize = style.fontSize.value > 0
                ? style.fontSize.value
                : fa.faceInfo.pointSize;
            nativeSettings.wordWrap = m_TextElement.computedStyle.whiteSpace.toTextCore();
            nativeSettings.horizontalAlignment = TextGeneratorUtilities.GetHorizontalAlignment(style.unityTextAlign);
            nativeSettings.verticalAlignment = TextGeneratorUtilities.GetVerticalAlignment(style.unityTextAlign);

            nativeSettings.color = m_TextElement.computedStyle.color;
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
    }
}
