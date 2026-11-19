// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityEditor.ShaderVariantAnalyser
{
    [StructLayout(LayoutKind.Sequential)]
    struct AssetId
    {
        public GUID guid;
        public long localId;
    }
}
