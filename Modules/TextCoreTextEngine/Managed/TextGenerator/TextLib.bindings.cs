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

                var padding = settings.vertexPadding / 64.0f;
                var inverseAtlasWidth = 1.0f / fa.atlasWidth;
                var inverseAtlasHeight = 1.0f / fa.atlasHeight;


                // TODO we should add glyphs in batch instead
                foreach (ref var textElementInfo in meshInfo.textElementInfos.AsSpan())
                {
                    var glyphID = textElementInfo.glyphID;

                    bool success = fa.TryAddGlyphInternal((uint)glyphID, out var glyph);
                    if (!success)
                        continue;

                    var glyphRect = glyph.glyphRect;

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

                        textElementInfo.bottomLeft.uv0.x = xMin;
                        textElementInfo.bottomLeft.uv0.y = yMin;

                        textElementInfo.topLeft.uv0.x = xMin;
                        textElementInfo.topLeft.uv0.y = yMax;

                        textElementInfo.topRight.uv0.x = xMax;
                        textElementInfo.topRight.uv0.y = yMax;

                        textElementInfo.bottomRight.uv0.x = xMax;
                        textElementInfo.bottomRight.uv0.y = yMin;
                    }
                    else
                    {
                        Vector2 bottomLeftUV; // Bottom Left
                        bottomLeftUV.x = (float)(glyphRect.x - padding) * inverseAtlasWidth;
                        bottomLeftUV.y = (float)(glyphRect.y - padding) * inverseAtlasHeight;

                        Vector2 topLeftUV; // Top Left
                        topLeftUV.x = bottomLeftUV.x;
                        topLeftUV.y = (float)(glyphRect.y + glyphRect.height + padding) * inverseAtlasHeight;

                        Vector2 topRightUV; // Top Right
                        topRightUV.x = (float)(glyphRect.x + glyphRect.width + padding) * inverseAtlasWidth;
                        topRightUV.y = topLeftUV.y;

                        Vector2 bottomRightUV; // Bottom Right
                        bottomRightUV.x = topRightUV.x;
                        bottomRightUV.y = bottomLeftUV.y;

                        textElementInfo.bottomLeft.uv0 = topRightUV * textElementInfo.bottomLeft.uv0 + bottomLeftUV * (new Vector2(1, 1) - textElementInfo.bottomLeft.uv0);
                        textElementInfo.topLeft.uv0 = topRightUV * textElementInfo.topLeft.uv0 + bottomLeftUV * (new Vector2(1, 1) - textElementInfo.topLeft.uv0); ;
                        textElementInfo.topRight.uv0 = topRightUV * textElementInfo.topRight.uv0 + bottomLeftUV * (new Vector2(1, 1) - textElementInfo.topRight.uv0); ;
                        textElementInfo.bottomRight.uv0 = topRightUV * textElementInfo.bottomRight.uv0 + bottomLeftUV * (new Vector2(1, 1) - textElementInfo.bottomRight.uv0); ;
                    }
                }
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
