// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.TextCore;
using UnityEngine.TextCore.Text;

namespace UnityEngine
{
    internal partial class IMGUITextHandle : TextHandle
    {
        internal static Func<Color> GetHyperlinkColor;
        internal int hashCode;

        internal override UnityEngine.TextAsset GetICUAsset()
        {
            // TODO: This is Editor only.
            return GetICUAssetStaticFalback();
        }

        internal static NativeTextGenerationSettings nativeSettingsIMGUI = new NativeTextGenerationSettings();
        static string s_IMGUICurrentText;
        NativeTextBuffer m_IMGUITextBuffer = NativeTextBuffer.CreateDomainScoped();

        internal NativeTextInfo nativeTextInfo;

        private static void GetMeshInfoNative(GUIStyle style, Color color, string content, Rect rect, ref MeshInfoBindings[] meshInfos, ref Vector2 dimensions, ref int generationId)
        {
            bool isCached = false;
            var textHandle = IMGUITextHandle.GetATGTextHandle(style, rect, content, color, ref isCached);
            var textInfo = textHandle.nativeTextInfo;
            generationId = textHandle.hashCode;
            // If not already cached on the native side, we must send the meshInfo
            if (!isCached)
            {
                textHandle.ComputeMeshInfos(ref meshInfos);
            }
            dimensions = textHandle.preferredSize;
        }

        internal static void ConvertGUIStyleToNativeTextGenerationSettings(ref NativeTextGenerationSettings nativeSettings, GUIStyle style, Color textColor, Rect rect, IntPtr textBufferPtr, int textBufferLength)
        {
            nativeSettings.textBufferPtr = textBufferPtr;
            nativeSettings.textBufferLength = textBufferLength;

            var items = GetTextSettingsFontAssetAndFontSize(style);
            var textSettings = items.Item1;
            var fontAsset = items.Item2;
            var fontSize = items.Item3;

            if (textSettings == null || fontAsset == null)
                return;

            nativeSettings.textSettings = textSettings.nativeTextSettings;
            nativeSettings.fontSize = fontSize * 64;
            nativeSettings.fontAsset = fontAsset.nativeFontAsset;

            var scale = GUIUtility.pixelsPerPoint;

            nativeSettings.preProcessFlags = PreProcessFlags.None;

            nativeSettings.fontStyle = TextGeneratorUtilities.LegacyStyleToNewStyle(style.fontStyle);
            nativeSettings.fontStyle = nativeSettings.fontStyle & ~FontStyles.Bold;
            nativeSettings.fontWeight = style.fontStyle == FontStyle.Bold ? TextFontWeight.Bold : TextFontWeight.Regular;

            Vector2 size = Vector2.zero;
            // If the raster mode is bitmap, we need to have a clean rect for the alignment to work properly.
            if (fontAsset.IsBitmap())
            {
                size = new Vector2(Mathf.Max(0, Mathf.Round(rect.width)), Mathf.Max(0, Mathf.Round(rect.height)));
            }
            else
            {
                size = new Vector2(Mathf.Max(0, rect.width), Mathf.Max(0, rect.height));

                if (fontAsset.IsEditorFont)
                {
                    fontAsset.material.SetFloat("_Sharpness", textSettings.GetEditorTextSharpness());
                }
                else
                {
                    fontAsset.material.SetFloat("_Sharpness", 0.5f);
                }
            }
            nativeSettings.screenWidth = Mathf.RoundToInt(size.x * 64.0f * scale);
            nativeSettings.screenHeight = Mathf.RoundToInt(size.y * 64.0f * scale);

            var tempAlignment = style.alignment;
            if (style.imagePosition == ImagePosition.ImageAbove)
            {
                switch (style.alignment)
                {
                    case TextAnchor.MiddleRight:
                    case TextAnchor.LowerRight:
                        tempAlignment = TextAnchor.UpperRight;
                        break;
                    case TextAnchor.MiddleCenter:
                    case TextAnchor.LowerCenter:
                        tempAlignment = TextAnchor.UpperCenter;
                        break;
                    case TextAnchor.MiddleLeft:
                    case TextAnchor.LowerLeft:
                        tempAlignment = TextAnchor.UpperLeft;
                        break;
                }
            }

            nativeSettings.horizontalAlignment = TextGeneratorUtilities.GetHorizontalAlignment(tempAlignment);
            nativeSettings.verticalAlignment = TextGeneratorUtilities.GetVerticalAlignment(tempAlignment);

            nativeSettings.overflow = LegacyClippingToNativeOverflow(style.clipping);

            if (rect.width > 0 && style.wordWrap)
            {
                nativeSettings.wordWrapEnabled = true;
            }
            else
            {
                nativeSettings.wordWrapEnabled = false;
            }

            // PreProcess flags are not enabled for IMGUI.
            nativeSettings.preProcessFlags = PreProcessFlags.None;
            nativeSettings.bestFit = false;
            nativeSettings.maxFontSize = 0;
            nativeSettings.minFontSize = 0;

            nativeSettings.languageDirection = LanguageDirection.LTR;
            nativeSettings.characterSpacing = 0;
            nativeSettings.wordSpacing = 0;
            nativeSettings.paragraphSpacing = 0;
            nativeSettings.color = textColor;

            nativeSettings.vertexPadding = (int)(k_MinPadding * 64.0f);
            nativeSettings.disableAdvancedFontFeatures = true;

            nativeSettings.richTextEnabled = style.richText;
            nativeSettings.hoveredTag = HoveredTag.UnderlineAllLinks;
            nativeSettings.pixelsPerPointFixed64 = (int)Math.Round(GUIUtility.pixelsPerPoint * 64.0f);
        }

        private void SyncLinksFromNative()
        {
            if (textGenerationInfo == IntPtr.Zero)
            {
                m_Links = null;
                return;
            }

            m_Links = NativeRichTextParser.GetAllLinks(textGenerationInfo);
        }

        const int k_MinPadding = 6;

        internal static (TextSettings, FontAsset, int) GetTextSettingsFontAssetAndFontSize(GUIStyle style)
        {
            if (s_EditorTextSettings == null)
            {
                s_EditorTextSettings = (TextSettings)GetEditorTextSettings?.Invoke();
            }

            TextSettings textSettings = s_EditorTextSettings;
            if (textSettings == null)
                return (null, null, 0);

            Font font = style.font;

            if (!font)
            {
                font = GUIStyle.GetDefaultFont();
            }

            var scale = GUIUtility.pixelsPerPoint;
            float fontSize = 0;
            int roundedFontSize = 0;

            if (style.fontSize > 0)
                fontSize = style.fontSize * scale;
            else if (font)
                fontSize = font.fontSize * scale;
            else
                fontSize = sFallbackFontSize * scale;

            FontAsset fontAsset = textSettings.GetCachedFontAsset(font);
            if (fontAsset == null)
                return (textSettings, null, 0);

            roundedFontSize = Mathf.RoundToInt(fontSize);
            var shouldRenderBitmap = !style.isSDF && fontAsset.IsEditorFont && TextCore.Text.TextGenerationSettings.IsEditorTextRenderingModeBitmap();
            if (shouldRenderBitmap)
            {
                roundedFontSize = (int)Math.Round(fontSize, MidpointRounding.AwayFromZero);
                fontAsset = GetBlurryFontAssetMapping(roundedFontSize, fontAsset, TextCore.Text.TextGenerationSettings.IsEditorTextRenderingModeRaster());
            }
           return (textSettings, fontAsset, roundedFontSize);
        }

        internal static unsafe IMGUITextHandle GetATGTextHandle(GUIStyle style, Rect position, string content, Color32 textColor, bool update)
        {
            bool isCached = false;
            s_IMGUICurrentText = content;
            fixed (char* textPtr = content)
            {
                ConvertGUIStyleToNativeTextGenerationSettings(ref nativeSettingsIMGUI, style, textColor, position, (IntPtr)textPtr, content?.Length ?? 0);
                return GetATGTextHandle(nativeSettingsIMGUI, false, ref isCached, update);
            }
        }

        internal static unsafe IMGUITextHandle GetATGTextHandle(GUIStyle style, Rect position, string content, Color32 textColor, ref bool isCached)
        {
            s_IMGUICurrentText = content;
            fixed (char* textPtr = content)
            {
                ConvertGUIStyleToNativeTextGenerationSettings(ref nativeSettingsIMGUI, style, textColor, position, (IntPtr)textPtr, content?.Length ?? 0);
                return GetATGTextHandle(nativeSettingsIMGUI, true, ref isCached, true);
            }
        }

        private static IMGUITextHandle GetATGTextHandle(NativeTextGenerationSettings nativeSettings, bool isCalledFromNative, ref bool isCached, bool update)
        {
            isCached = false;
            var currentTime = Time.realtimeSinceStartup;
            if (ShouldCleanup(currentTime, lastCleanupTime, sTimeBetweenCleanupRuns) ||
                newHandlesSinceCleanup > sNewHandlesBetweenCleanupRuns)
            {
                ClearUnusedTextHandles();
                lastCleanupTime = currentTime;
                newHandlesSinceCleanup = 0;
            }

            int hash = nativeSettings.GetHashCode();
         
            if (textHandles.TryGetValue(hash, out IMGUITextHandle textHandleCached))
            {
                textHandlesTuple.Remove(textHandleCached.tuple);
                textHandlesTuple.AddLast(textHandleCached.tuple);

                isCached = isCalledFromNative ? textHandleCached.isCachedOnNative : true;
                if (!textHandleCached.isCachedOnNative && isCalledFromNative)
                {
                    bool c = false;
                    textHandleCached.hashCode = hash;
                    textHandleCached.UpdateNative(ref c);
                    textHandleCached.isCachedOnNative = true;
                }
                return textHandleCached;
            }

            var handle = new IMGUITextHandle();
            var tuple = new TextHandleTuple(currentTime, hash);
            var listNode = new LinkedListNode<TextHandleTuple>(tuple);
            handle.tuple = listNode;
            textHandles[hash] = handle;
            handle.hashCode = hash;
            // we might not want to update if we know we will be adding it to the permanent cache right after.
            if (update)
                handle.UpdateNative(ref isCached);
            isCached = false;
            textHandlesTuple.AddLast(listNode);
            handle.isCachedOnNative = isCalledFromNative;
            ++newHandlesSinceCleanup;
            return handle;
        }

        static List<List<List<int>>> s_TextElementIndicesByMesh = new();
        static Dictionary<EntityId, HashSet<uint>> s_MissingGlyphsPerFontAsset = new();
        internal void ComputeMeshInfos(ref MeshInfoBindings[] meshInfos)
        {
            foreach (var listOfAtlases in s_TextElementIndicesByMesh)
            {
                foreach (var listOfIndices in listOfAtlases)
                {
                    listOfIndices.Clear();
                }
            }
            s_MissingGlyphsPerFontAsset.Clear();

            var invScale = 1 / GUIUtility.pixelsPerPoint;
            if (textLib.HasMissingGlyphs(nativeTextInfo, ref s_MissingGlyphsPerFontAsset))
            {
                PopulateGlyphs(s_MissingGlyphsPerFontAsset);
            }
            textLib.ProcessMeshInfos(nativeTextInfo, nativeSettingsIMGUI, ref s_TextElementIndicesByMesh, false);

            // Count total number of atlases across all mesh infos
            int totalAtlasCount = 0;
            for (int i = 0; i < nativeTextInfo.meshInfoCount; i++)
            {
                ATGMeshInfo meshInfo = nativeTextInfo.meshInfos[i];
                var textAsset = Object.FindObjectFromInstanceIDThreadSafe(meshInfo.textAssetId) as TextCore.Text.TextAsset;
                if (textAsset == null || textAsset is SpriteAsset)
                    continue;
                FontAsset fa = textAsset as FontAsset;
                totalAtlasCount += fa.atlasTextures.Length;
            }

            meshInfos = new MeshInfoBindings[totalAtlasCount];
            int meshInfoIndex = 0;

            for (int i = 0; i < nativeTextInfo.meshInfoCount; i++)
            {
                ATGMeshInfo meshInfo = nativeTextInfo.meshInfos[i];
                FontAsset fa = null;
                var textAsset = Object.FindObjectFromInstanceIDThreadSafe(meshInfo.textAssetId) as TextCore.Text.TextAsset;
                if (textAsset == null || textAsset is SpriteAsset)
                    continue;
                fa = textAsset as FontAsset;
                int atlasCount = fa.atlasTextures.Length;

                var sdfScale = 0;
                if (!fa.IsBitmap())
                    sdfScale = fa.atlasPadding + 1;

                var textElementInfos = meshInfo.textElementInfos;

                // Create a separate MeshInfoBindings for each atlas
                for (int j = 0; j < atlasCount; ++j)
                {
                    var textElementInfoInAtlas = s_TextElementIndicesByMesh[i][j];
                    int vertexCount = textElementInfoInAtlas.Count * 4;

                    if (vertexCount == 0)
                        continue;

                    meshInfos[meshInfoIndex].vertexData = new TextCoreVertex[vertexCount];
                    meshInfos[meshInfoIndex].vertexCount = vertexCount;
                    // Get the correct material for this specific atlas texture
                    meshInfos[meshInfoIndex].material = j == 0 ? fa.material : MaterialManager.GetFallbackMaterial(fa, fa.material, j);

                    int vertexOffset = 0;
                    // Iterate through the indices for this specific atlas
                    for (int vSrc = 0; vSrc < textElementInfoInAtlas.Count; vSrc++)
                    {
                        int textElementIndex = textElementInfoInAtlas[vSrc];
                        var te = textElementInfos[textElementIndex];

                        meshInfos[meshInfoIndex].vertexData[vertexOffset + 0].position = te.bottomLeft.position * invScale;
                        meshInfos[meshInfoIndex].vertexData[vertexOffset + 1].position = te.topLeft.position * invScale;
                        meshInfos[meshInfoIndex].vertexData[vertexOffset + 2].position = te.topRight.position * invScale;
                        meshInfos[meshInfoIndex].vertexData[vertexOffset + 3].position = te.bottomRight.position * invScale;

                        meshInfos[meshInfoIndex].vertexData[vertexOffset + 0].uv0 = te.bottomLeft.uv0;
                        meshInfos[meshInfoIndex].vertexData[vertexOffset + 1].uv0 = te.topLeft.uv0;
                        meshInfos[meshInfoIndex].vertexData[vertexOffset + 2].uv0 = te.topRight.uv0;
                        meshInfos[meshInfoIndex].vertexData[vertexOffset + 3].uv0 = te.bottomRight.uv0;

                        float nativeBold = te.bottomLeft.uv2.y;
                        float signedSdfScale = nativeBold != 0 ? -sdfScale : sdfScale;
                        var uv2 = new Vector2(1, signedSdfScale);
                        meshInfos[meshInfoIndex].vertexData[vertexOffset + 0].uv2 = uv2;
                        meshInfos[meshInfoIndex].vertexData[vertexOffset + 1].uv2 = uv2;
                        meshInfos[meshInfoIndex].vertexData[vertexOffset + 2].uv2 = uv2;
                        meshInfos[meshInfoIndex].vertexData[vertexOffset + 3].uv2 = uv2;

                        meshInfos[meshInfoIndex].vertexData[vertexOffset + 0].color = te.bottomLeft.color;
                        meshInfos[meshInfoIndex].vertexData[vertexOffset + 1].color = te.topLeft.color;
                        meshInfos[meshInfoIndex].vertexData[vertexOffset + 2].color = te.topRight.color;
                        meshInfos[meshInfoIndex].vertexData[vertexOffset + 3].color = te.bottomRight.color;

                        vertexOffset += 4;
                    }

                    meshInfoIndex++;
                }
            }
        }

        static void PopulateGlyphs(Dictionary<EntityId, HashSet<uint>> missingGlyphsPerFontAsset)
        {
            if (missingGlyphsPerFontAsset.Count == 0)
                return;

            List<uint> missingGlyphs = new();
            foreach (var entry in missingGlyphsPerFontAsset)
            {
                var textAsset = Object.FindObjectFromInstanceIDThreadSafe(entry.Key) as TextCore.Text.TextAsset;
                if (textAsset == null || textAsset is not FontAsset fa || entry.Value.Count == 0)
                    continue;

                missingGlyphs.Clear();
                missingGlyphs.AddRange(entry.Value);
                fa.TryAddGlyphs(missingGlyphs, populateFontFeatures: false);
            }

            FontAsset.UpdateFontAssetsInUpdateQueue();
        }

        public override void AddToPermanentCacheAndGenerateMesh()
        {
            if (useAdvancedText)
            {
                CacheTextGenerationInfo();
                bool isCached = false;
                UpdateNative(ref isCached);
            }
            else
            {
                base.AddToPermanentCacheAndGenerateMesh();
            }
        }

        public override void RemoveFromPermanentCacheATG()
        {
            m_IMGUITextBuffer.Dispose();
            base.RemoveFromPermanentCacheATG();
        }

        public void UpdateNative(ref bool isCached)
        {
            m_IMGUITextBuffer.CopyFrom(s_IMGUICurrentText);
            nativeSettingsIMGUI.SetTextBuffer(m_IMGUITextBuffer.buffer, m_IMGUITextBuffer.length);
            FontAsset.CreateHbFaceIfNeeded();
            nativeTextInfo = textLib.GenerateText(nativeSettingsIMGUI, textGenerationInfo, ref isCached);
            SyncLinksFromNative();
            pixelPreferedSize = new Vector2(nativeTextInfo.totalWidth / 64.0f, nativeTextInfo.totalHeight / 64.0f);
            m_IsElided = nativeTextInfo.isElided;
        }

        internal static float GetNativeLineHeightDefault(GUIStyle style)
        {
            var items = GetTextSettingsFontAssetAndFontSize(style);
            var textSettings = items.Item1;
            var fontAsset = items.Item2;
            var fontSize = items.Item3 * 64;

            if (textSettings == null || fontAsset == null)
                return 0;

            return GetLineHeightDefault(fontAsset, fontSize / 64);
        }

        private bool HasClickedOnLinkATG(Vector2 mousePosition, out string linkData)
        {
            linkData = "";
            var info = ATGFindIntersectingLink(mousePosition);
            if (info.id == -1)
                return false;

            if (info.value != null && info.value.Length > 0)
            {
                linkData = new string(info.value);
                return true;
            }
            return false;
        }

        private bool HasClickedOnHREFATG(Vector2 mousePosition, out string href)
        {
            href = "";
            var info = ATGFindIntersectingLink(mousePosition);
            if (info.id == -1)
                return false;

            if (info.isHyperlink)
            {
                if (info.value != null && info.value.Length > 0)
                {
                    href = info.value;
                    if (Uri.IsWellFormedUriString(href, UriKind.Absolute))
                    {
                        return true;
                    }
                }
            }
            return false;
        } 
    }
}
