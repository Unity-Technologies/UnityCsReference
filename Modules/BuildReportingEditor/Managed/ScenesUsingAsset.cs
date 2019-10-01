// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Reporting
{
    [NativeType(Header = "Modules/BuildReportingEditor/Public/ScenesUsingAssets.h")]
    public struct ScenesUsingAsset
    {
        public string assetPath { get; }
        public string[] scenePaths { get; }
    }
}
