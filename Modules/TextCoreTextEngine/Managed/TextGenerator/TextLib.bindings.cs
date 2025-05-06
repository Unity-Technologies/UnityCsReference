// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.TextCore.Text
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/TextCoreTextEngine/Native/TextLib.h")]
    [VisibleToOtherModules("UnityEngine.UIElementsModule", "Unity.UIElements.PlayModeTests")]
    internal class TextLib
    {
        public const int k_unconstrainedScreenSize = -1;
        private readonly IntPtr m_Ptr;

        public TextLib(byte[] icuData)
        {
            m_Ptr = GetInstance(icuData);
        }

        private static extern IntPtr GetInstance(byte[] icuData);

        public NativeTextInfo GenerateText(NativeTextGenerationSettings settings, IntPtr textGenerationInfo)
        {
            Debug.Assert((settings.fontStyle & FontStyles.Bold) == 0);// Bold need to be set by the fontWeight only.
            var textInfo = GenerateTextInternal(settings, textGenerationInfo);

            return textInfo;
        }

        // The rasterization of glyph was extracted to its own method to ensure it is called on the main thread.
        public void ProcessMeshInfos(NativeTextInfo textInfo, NativeTextGenerationSettings settings)
        {
            foreach (ref var meshInfo in textInfo.meshInfos.AsSpan())
            {
                var fa = FontAsset.GetFontAssetByID(meshInfo.fontAssetId);
                meshInfo.fontAsset = fa;
                meshInfo.textElementInfoIndicesByAtlas = new List<List<int>>(fa.atlasTextures.Length);
                for (int i = 0; i < fa.atlasTextures.Length; i++)
                    meshInfo.textElementInfoIndicesByAtlas.Add(new List<int>());

                var padding = settings.vertexPadding / 64.0f;
                var inverseAtlasWidth = 1.0f / fa.atlasWidth;
                var inverseAtlasHeight = 1.0f / fa.atlasHeight;

                bool hasMultipleColors = false;
                Color? previousColor = null;

                // TODO we should add glyphs in batch instead
                for (int i = 0; i < meshInfo.textElementInfos.Length; i++)
                {
                    ref var textElementInfo = ref meshInfo.textElementInfos[i];
                    var glyphID = textElementInfo.glyphID;

                    bool success = fa.TryAddGlyphInternal((uint)glyphID, out var glyph);
                    if (!success)
                        continue;

                    var currentColor = textElementInfo.topLeft.color;
                    if (previousColor.HasValue && previousColor.Value != currentColor)
                    {
                        hasMultipleColors = true;
                    }
                    previousColor = currentColor;

                    var glyphRect = glyph.glyphRect;

                    while (meshInfo.textElementInfoIndicesByAtlas.Count < fa.atlasTextures.Length)
                        meshInfo.textElementInfoIndicesByAtlas.Add(new List<int>());

                    meshInfo.textElementInfoIndicesByAtlas[glyph.atlasIndex].Add(i);

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
                        textElementInfo.topLeft.uv0    = new Vector2(xMin, yMax);
                        textElementInfo.topRight.uv0   = new Vector2(xMax, yMax);
                        textElementInfo.bottomRight.uv0= new Vector2(xMax, yMin);
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
                        textElementInfo.topLeft.uv0    = topRightUV * textElementInfo.topLeft.uv0    + bottomLeftUV * (Vector2.one - textElementInfo.topLeft.uv0);
                        textElementInfo.topRight.uv0   = topRightUV * textElementInfo.topRight.uv0   + bottomLeftUV * (Vector2.one - textElementInfo.topRight.uv0);
                        textElementInfo.bottomRight.uv0= topRightUV * textElementInfo.bottomRight.uv0+ bottomLeftUV * (Vector2.one - textElementInfo.bottomRight.uv0);
                    }
                }
                meshInfo.hasMultipleColors = hasMultipleColors;
            }
        }

        [NativeMethod(Name = "TextLib::GenerateTextMesh", IsThreadSafe = true)]
        private extern NativeTextInfo GenerateTextInternal(NativeTextGenerationSettings settings, IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextLib::MeasureText")]
        public extern Vector2 MeasureText(NativeTextGenerationSettings settings, IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextLib::FindIntersectingLink")]
        static public extern int FindIntersectingLink(Vector2 point, IntPtr textGenerationInfo);

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(TextLib textLib) => textLib.m_Ptr;
        }

        public static Func<UnityEngine.TextAsset> GetICUAssetEditorDelegate;
    }

    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal static class TextGenerationInfo
    {
        [ThreadSafe]
        public static extern IntPtr Create();

        public static extern void Destroy(IntPtr ptr);
    }
}
