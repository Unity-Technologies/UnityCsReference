// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.TextCore.Text
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/TextCoreTextEngine/Native/TextLib.h")]
    [VisibleToOtherModules("UnityEngine.UIElementsModule","Unity.UIElements.PlayModeTests")]
    internal class TextLib
    {
        private readonly IntPtr m_Ptr;

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal TextLib()
        {
            m_Ptr = GetInstance();
        }

        private static extern IntPtr GetInstance();

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal NativeTextInfo GenerateText(NativeTextGenerationSettings settings, IntPtr textGenerationInfo)
        {
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

                    textElementInfo.bottomLeft.uv0 = bottomLeftUV;
                    textElementInfo.topLeft.uv0 = topLeftUV;
                    textElementInfo.topRight.uv0 = topRightUV;
                    textElementInfo.bottomRight.uv0 = bottomRightUV;
                }
            }
            return textInfo;
        }

        [NativeMethod(Name = "TextLib::GenerateTextMesh")]
        private extern NativeTextInfo GenerateTextInternal(NativeTextGenerationSettings settings, IntPtr textGenerationInfo);

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        [NativeMethod(Name = "TextLib::MeasureText")]
        internal extern Vector2 MeasureText(NativeTextGenerationSettings settings, IntPtr textGenerationInfo);

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(TextLib textLib) => textLib.m_Ptr;
        }

        static byte[] s_ICUData = null;
        internal static Func<UnityEngine.TextAsset> GetICUAssetEditorDelegate;
        private static UnityEngine.TextAsset GetICUAsset()
        {
            if (GetICUAssetEditorDelegate != null)
            {
                //Editor will load the ICU library before the scene, so we need to check in the asset database as the asset may not be loaded yet.
                var asset = GetICUAssetEditorDelegate();
                if (asset != null)
                    return asset;
            }
            else
            {
                Debug.LogError("GetICUAssetEditorDelegate is null");
            }
            // Dont know about the panelSettings class existence here so we must filter by name
            foreach( var t in Resources.FindObjectsOfTypeAll<UnityEngine.TextAsset>())
            {
                if (t.name == "icudt73l")
                    return t;
            }

            return null;
        }

        [RequiredByNativeCode]
        internal static int LoadAndCountICUdata()
        {
            s_ICUData = GetICUAsset()?.bytes;
            return s_ICUData?.Length ?? 0;
        }

        [VisibleToOtherModules("Unity.UIElements.PlayModeTests")]
        [RequiredByNativeCode]
        internal static bool GetICUdata(Span<byte> data,  int maxSize)
        {
            if (s_ICUData == null)
                LoadAndCountICUdata();

            Debug.Assert(s_ICUData != null, "s_ICUData not initialized");
            Debug.Assert(maxSize == s_ICUData.Length);

            s_ICUData.CopyTo(data);
            s_ICUData = null;
            return true;
        }
    }

    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal static class TextGenerationInfo
    {
        internal static extern IntPtr Create();

        [FreeFunction("TextGenerationInfo::Destroy")]
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern void Destroy(IntPtr ptr);
    }
}
