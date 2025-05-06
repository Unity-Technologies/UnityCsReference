// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.TextCore.Text
{
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/TextCoreTextEngine/Native/ATGMeshInfo.h")]
    internal struct ATGMeshInfo
    {
        public NativeTextElementInfo[] textElementInfos;
        public int fontAssetId;
        public int textElementCount;

        [Ignore] // This field must be populated on the managed side
        public FontAsset fontAsset;

        [Ignore]
        public List<List<int>> textElementInfoIndicesByAtlas;

        [Ignore]
        public bool hasMultipleColors;
    }
}
