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

            foreach (ref var meshInfo in textInfo.meshInfos.AsSpan())
            {
                var fa = FontAsset.GetFontAssetByID(meshInfo.fontAssetId);
                meshInfo.fontAsset = fa;

                // TODO we should add glyphs in batch instead
                foreach (ref var textElementInfo in meshInfo.textElementInfos.AsSpan())
                {
                    var glyphID = textElementInfo.glyphID;

                    bool success = fa.TryAddGlyphInternal((uint)glyphID, out var glyph);
                    if (!success)
                        continue;

                    var glyphRect = glyph.glyphRect;
                    var padding = settings.vertexPadding / 64.0f;

                    Vector2 bottomLeftUV; // Bottom Left
                    bottomLeftUV.x = (float)(glyphRect.x - padding) / fa.atlasWidth;
                    bottomLeftUV.y = (float)(glyphRect.y - padding) / fa.atlasHeight;

                    Vector2 topLeftUV; // Top Left
                    topLeftUV.x = bottomLeftUV.x;
                    topLeftUV.y = (float)(glyphRect.y + glyphRect.height + padding) / fa.atlasHeight;

                    Vector2 topRightUV; // Top Right
                    topRightUV.x = (float)(glyphRect.x + glyphRect.width + padding) / fa.atlasWidth;
                    topRightUV.y = topLeftUV.y;

                    Vector2 bottomRightUV; // Bottom Right
                    bottomRightUV.x = topRightUV.x;
                    bottomRightUV.y = bottomLeftUV.y;

                    // The native code is not yet aware of the atlas, and glyphs for the underline+strikethrough have their UV manually eddited to
                    // be stretched without side effect. We need to combine the position in the atlas with the expected position in the source glyph.
                    textElementInfo.bottomLeft.uv0 = topRightUV * textElementInfo.bottomLeft.uv0 + bottomLeftUV * (new Vector2(1, 1) - textElementInfo.bottomLeft.uv0);
                    textElementInfo.topLeft.uv0 = topRightUV * textElementInfo.topLeft.uv0 + bottomLeftUV * (new Vector2(1, 1) - textElementInfo.topLeft.uv0); ;
                    textElementInfo.topRight.uv0 = topRightUV * textElementInfo.topRight.uv0 + bottomLeftUV * (new Vector2(1, 1) - textElementInfo.topRight.uv0); ;
                    textElementInfo.bottomRight.uv0 = topRightUV * textElementInfo.bottomRight.uv0 + bottomLeftUV * (new Vector2(1, 1) - textElementInfo.bottomRight.uv0); ;
                }
            }
            return textInfo;
        }

        [NativeMethod(Name = "TextLib::GenerateTextMesh")]
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
        public static extern IntPtr Create();

        [FreeFunction("TextGenerationInfo::Destroy",IsThreadSafe = true)]
        public static extern void Destroy(IntPtr ptr);
    }
}
