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
        private IntPtr m_Ptr;

        ~TextLib()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal TextLib()
        {
            m_Ptr = Create();
        }

        private static extern IntPtr Create();
        private static extern void Destroy(IntPtr ptr);

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal NativeTextInfo GenerateText(NativeTextGenerationSettings settings)
        {
            var textInfo = GenerateTextInternal(settings);
            var textElementInfos = textInfo.textElementInfos;
            textInfo.fontAssets = new FontAsset[textInfo.fontAssetIds.Length];

            int previousLastGlyphIndex = 0;
            for (int i = 0; i < textInfo.fontAssetIds.Length; i++)
            {
                var fa = FontAsset.GetFontAssetByID(textInfo.fontAssetIds[i]);
                textInfo.fontAssets[i] = fa;

                for (int j = previousLastGlyphIndex; j < textInfo.fontAssetLastGlyphIndex[i]; j++)
                {
                    var glyphID = textElementInfos[j].glyphID;

                    bool success = fa.TryAddGlyphInternal((uint)glyphID, out var glyph);
                    if (!success)
                        continue;

                    var glyphRect = glyph.glyphRect;

                    Vector2 uv0;
                    uv0.x = (float)glyphRect.x / fa.atlasWidth;
                    uv0.y = (float)glyphRect.y / fa.atlasHeight;
                    Vector2 uv1;
                    uv1.x = uv0.x;
                    uv1.y = (float)(glyphRect.y + glyphRect.height) / fa.atlasHeight;
                    Vector2 uv2;
                    uv2.x = (float)(glyphRect.x + glyphRect.width) / fa.atlasWidth;
                    uv2.y = uv1.y;
                    Vector2 uv3;
                    uv3.x = uv2.x;
                    uv3.y = uv0.y;

                    textElementInfos[j].bottomLeft.uv0 = uv0;
                    textElementInfos[j].topLeft.uv0 = uv1;
                    textElementInfos[j].topRight.uv0 = uv2;
                    textElementInfos[j].bottomRight.uv0 = uv3;
                }
                previousLastGlyphIndex = textInfo.fontAssetLastGlyphIndex[i];
            }

            return textInfo;
        }

        [NativeMethod(Name = "TextLib::GenerateText")]
        private extern NativeTextInfo GenerateTextInternal(NativeTextGenerationSettings settings);

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
}
