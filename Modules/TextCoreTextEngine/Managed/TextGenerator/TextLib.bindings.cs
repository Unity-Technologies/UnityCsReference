// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.TextCore.Text
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/TextCoreTextEngine/Native/TextLib.h")]
    [VisibleToOtherModules("UnityEngine.UIElementsModule", "Unity.UIElements.PlayModeTests", "UnityEngine.IMGUIModule")]
    internal class TextLib
    {
        public const int k_unconstrainedScreenSize = -1;
        private readonly IntPtr m_Ptr;

        public TextLib(byte[] icuData)
        {
            m_Ptr = GetInstance(icuData);
        }

        private static extern IntPtr GetInstance(byte[] icuData);

        public NativeTextInfo GenerateText(NativeTextGenerationSettings settings, IntPtr textGenerationInfo, ref bool wasCached)
        {
            Debug.Assert((settings.fontStyle & FontStyles.Bold) == 0);// Bold need to be set by the fontWeight only.
            var textInfo = GenerateTextInternal(settings, textGenerationInfo, ref wasCached);

            return textInfo;
        }

        public bool HasMissingGlyphs(NativeTextInfo textInfo, ref Dictionary<EntityId, HashSet<uint>> missingGlyphsPerFontAsset)
        {
            Span<ATGMeshInfo> meshInfosSpan = textInfo.meshInfos;
            bool hasMissingGlyphs = false;
            foreach (ref var meshInfo in meshInfosSpan)
            {
                var textAsset = Object.FindObjectFromInstanceIDThreadSafe(meshInfo.textAssetId) as TextAsset;
                HashSet<uint> missingUnicodes = null;

                if (textAsset is SpriteAsset || textAsset == null)
                {
                    // Sprite is never populated during text generation.
                    // If it's missing, we won't add it to the atlas anyway.
                    continue;
                }

                Span<NativeTextElementInfo> textElementInfosSpan = meshInfo.textElementInfos;

                for (int i = 0; i < textElementInfosSpan.Length; i++)
                {
                    ref var textElementInfo = ref textElementInfosSpan[i];
                    var glyphID = textElementInfo.glyphID;
                    Glyph glyph = null;

                    glyph = ((FontAsset)textAsset).GetGlyphInCache((uint)glyphID);
                    if (glyph == null)
                    {
                        hasMissingGlyphs = true;
                        if (missingUnicodes == null)
                        {
                            // The HashSet for this FontAsset might already be in our Dictionary. If not, create it.
                            if (!missingGlyphsPerFontAsset.TryGetValue(meshInfo.textAssetId, out missingUnicodes))
                            {
                                missingUnicodes = new HashSet<uint>();
                                missingGlyphsPerFontAsset.Add(meshInfo.textAssetId, missingUnicodes);
                            }
                        }
                        missingUnicodes.Add((uint)glyphID);
                    }
                }
            }
            return hasMissingGlyphs;
        }

        // The rasterization of glyph was extracted to its own method to ensure it is called on the main thread.
        public void ProcessMeshInfos(NativeTextInfo textInfo, NativeTextGenerationSettings settings, ref List<List<List<int>>> textElementIndicesByMesh, bool uvsAreGenerated)
        {
            Span<ATGMeshInfo> meshInfosSpan = textInfo.meshInfos;

            int meshInfoIndex = 0;

            foreach (ref var meshInfo in meshInfosSpan)
            {
                var textAsset = Object.FindObjectFromInstanceIDThreadSafe(meshInfo.textAssetId) as TextAsset;
                if (textAsset == null)
                    continue;
                float inverseAtlasWidth = 0f;
                float inverseAtlasHeight = 0f;
                bool isSprite = false;

                // Note that textElementIndicesByMesh is not empty since we reuse it to avoid allocations.
                // This is why we sometimes need to grow it, or refer to an existing index.
                List<List<int>> atlasList;
                int requiredAtlasCount = 1;
                if (meshInfoIndex < textElementIndicesByMesh.Count)
                {
                    atlasList = textElementIndicesByMesh[meshInfoIndex];
                }
                else
                {
                    atlasList = new List<List<int>>();
                    textElementIndicesByMesh.Add(atlasList);
                }

                if (textAsset is FontAsset fontAsset)
                {
                    isSprite = false;
                    requiredAtlasCount = fontAsset.atlasTextures.Length;

                    inverseAtlasWidth = 1.0f / fontAsset.atlasWidth;
                    inverseAtlasHeight = 1.0f / fontAsset.atlasHeight;
                }
                else if (textAsset is SpriteAsset spriteAsset)
                {
                    isSprite = true;
                    requiredAtlasCount = 1;

                    inverseAtlasWidth = 1.0f / spriteAsset.width;
                    inverseAtlasHeight = 1.0f / spriteAsset.height;
                }

                while (atlasList.Count < requiredAtlasCount)
                {
                    atlasList.Add(new List<int>());
                }

                var padding = settings.vertexPadding / 64.0f;

                Span<NativeTextElementInfo> textElementInfosSpan = meshInfo.textElementInfos;

                for (int i = 0; i < textElementInfosSpan.Length; i++)
                {
                    ref var textElementInfo = ref textElementInfosSpan[i];
                    var glyphID = textElementInfo.glyphID;
                    Glyph glyph = null;
                    int spriteIndex = 0;

                    if (isSprite)
                    {
                        spriteIndex = glyphID - '\uE000';
                        padding = 0;
                        glyph = ((SpriteAsset)textAsset).spriteCharacterTable[spriteIndex].glyph;
                        if (spriteIndex == -1)
                            continue;
                    }
                    else
                    {
                        glyph = ((FontAsset)textAsset).GetGlyphInCache((uint)glyphID);
                        if (glyph == null)
                            continue;
                    }

                    var glyphRect = glyph.glyphRect;

                    textElementIndicesByMesh[meshInfoIndex][glyph.atlasIndex].Add(i);

                    if (uvsAreGenerated)
                        continue;

                    // This is false for special characters like Underline
                    bool allUVsAreZeroOrOne =
                        (textElementInfo.bottomLeft.uv0.x == 0f || textElementInfo.bottomLeft.uv0.x == 1f) &&
                        (textElementInfo.bottomLeft.uv0.y == 0f || textElementInfo.bottomLeft.uv0.y == 1f) &&
                        (textElementInfo.topLeft.uv0.x == 0f || textElementInfo.topLeft.uv0.x == 1f) &&
                        (textElementInfo.topLeft.uv0.y == 0f || textElementInfo.topLeft.uv0.y == 1f) &&
                        (textElementInfo.topRight.uv0.x == 0f || textElementInfo.topRight.uv0.x == 1f) &&
                        (textElementInfo.topRight.uv0.y == 0f || textElementInfo.topRight.uv0.y == 1f) &&
                        (textElementInfo.bottomRight.uv0.x == 0f || textElementInfo.bottomRight.uv0.x == 1f) &&
                        (textElementInfo.bottomRight.uv0.y == 0f || textElementInfo.bottomRight.uv0.y == 1f);

                    if (allUVsAreZeroOrOne)
                    {
                        float xMin = (glyphRect.x - padding) * inverseAtlasWidth;
                        float yMin = (glyphRect.y - padding) * inverseAtlasHeight;
                        float xMax = (glyphRect.x + glyphRect.width + padding) * inverseAtlasWidth;
                        float yMax = (glyphRect.y + glyphRect.height + padding) * inverseAtlasHeight;

                        textElementInfo.bottomLeft.uv0 = new Vector2(xMin, yMin);
                        textElementInfo.topLeft.uv0 = new Vector2(xMin, yMax);
                        textElementInfo.topRight.uv0 = new Vector2(xMax, yMax);
                        textElementInfo.bottomRight.uv0 = new Vector2(xMax, yMin);
                    }
                    else
                    {
                        Vector2 bottomLeftUV = new Vector2((glyphRect.x - padding) * inverseAtlasWidth,
                                                           (glyphRect.y - padding) * inverseAtlasHeight);
                        Vector2 topLeftUV = new Vector2(bottomLeftUV.x,
                                                        (glyphRect.y + glyphRect.height + padding) * inverseAtlasHeight);
                        Vector2 topRightUV = new Vector2((glyphRect.x + glyphRect.width + padding) * inverseAtlasWidth,
                                                         topLeftUV.y);

                        textElementInfo.bottomLeft.uv0 = topRightUV * textElementInfo.bottomLeft.uv0 + bottomLeftUV * (Vector2.one - textElementInfo.bottomLeft.uv0);
                        textElementInfo.topLeft.uv0 = topRightUV * textElementInfo.topLeft.uv0 + bottomLeftUV * (Vector2.one - textElementInfo.topLeft.uv0);
                        textElementInfo.topRight.uv0 = topRightUV * textElementInfo.topRight.uv0 + bottomLeftUV * (Vector2.one - textElementInfo.topRight.uv0);
                        textElementInfo.bottomRight.uv0 = topRightUV * textElementInfo.bottomRight.uv0 + bottomLeftUV * (Vector2.one - textElementInfo.bottomRight.uv0);
                    }

                }

                meshInfoIndex++;
            }
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern void ShapeText(NativeTextGenerationSettings settings, IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextLib::GenerateTextMesh", IsThreadSafe = true)]
        private extern NativeTextInfo GenerateTextInternal(NativeTextGenerationSettings settings, IntPtr textGenerationInfo, ref bool uvsAreGenerated);

        [NativeMethod(Name = "TextLib::MeasureText")]
        public extern Vector2 MeasureText(NativeTextGenerationSettings settings, IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextLib::FindIntersectingLink")]
        static public extern int FindIntersectingLink(Vector2 point, IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextLib::GetCharacterCount")]
        static public extern int GetCharacterCount(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextLib::GetNumCharactersThatFitWithinWidth")]
        static public extern int GetNumCharactersThatFitWithinWidth(IntPtr textGenerationInfo, int width);

        [NativeMethod(Name = "TextLib::GetHyperlinkRects")]
        static public extern Rect[] GetHyperlinkRects(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextLib::IsMainDirectionRTL")]
        static public extern bool IsMainDirectionRTL(IntPtr textGenerationInfo);

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(TextLib textLib) => textLib.m_Ptr;
        }

        public static Func<UnityEngine.TextAsset> GetICUAssetEditorDelegate;
    }

    [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
    internal static class TextGenerationInfo
    {
        public static int CurrentGenerationIteration { get; private set; }
        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr Create(bool isPermanent);

        [NativeMethod(IsThreadSafe = true)]
        public static extern void Destroy(IntPtr ptr);

        public static void OnRepaintEnd()
        {
            CurrentGenerationIteration++;
            DestroyAllTempAllocations();
        }

        private static extern void DestroyAllTempAllocations();

        [NativeMethod(IsThreadSafe = true)]
        public static extern NativeGlyphRenderInfo GetGlyphRenderInfo(IntPtr ptr, int glyphIndex);

        [NativeMethod(IsThreadSafe = true)]
        public static extern int GetGlyphCount(IntPtr ptr);

        [NativeMethod(IsThreadSafe = true)]
        public static extern int GetLineCount(IntPtr ptr);

        [NativeMethod(IsThreadSafe = true)]
        public static extern int GetLogicalIndexFromGlyphIndex(IntPtr ptr, int glyphIndex);

        // Used for testing purposes
        public static extern NativeTextInfo GetTextInfo(IntPtr ptr);

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        public static extern float GetLineHeight(IntPtr ptr, int lineNumber);

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        public static extern float GetLineAscender(IntPtr ptr, int lineNumber);

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        public static extern float GetLineBaselineY(IntPtr ptr, int lineNumber);
    }
}
