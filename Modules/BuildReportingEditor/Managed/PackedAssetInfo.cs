// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Reporting
{
    [NativeType(Header = "Modules/BuildReportingEditor/Public/PackedAssets.h")]
    public struct PackedAssetInfo
    {
        [NativeName("fileID")]
        public long id { get; }
        public Type type { get; }
        public ulong packedSize { get; }
        public ulong offset { get; }
        public GUID sourceAssetGUID { get; }
        [NativeName("buildTimeAssetPath")]
        public string sourceAssetPath { get; }
    }
}
