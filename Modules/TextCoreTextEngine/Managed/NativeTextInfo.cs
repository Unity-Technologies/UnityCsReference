// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.TextCore;

namespace UnityEngine.TextCore.Text
{
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/TextCoreTextEngine/Native/TextInfo.h")]
    internal struct NativeTextInfo
    {
        public NativeTextElementInfo[] textElementInfos;
        public int[] fontAssetIds;
        public int[] fontAssetLastGlyphIndex;

        [Ignore] // This field must be populated on the managed side
        public FontAsset[] fontAssets;
    }
}
