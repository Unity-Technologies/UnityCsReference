// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Reporting
{
    ///<summary>Contains information about which scenes in a build have references to an Asset in the build.</summary>
    [NativeType(Header = "Modules/BuildReportingEditor/Public/ScenesUsingAssets.h")]
    public struct ScenesUsingAsset
    {
        ///<summary>The asset path.</summary>
        public string assetPath { get; }
        ///<summary>The list of scenes in the build referring to the asset, identified by a string containing the scene index in the <see cref="BuildPlayerOptions.scenes" /> list, as well as the scene path.</summary>
        public string[] scenePaths { get; }
    }
}
