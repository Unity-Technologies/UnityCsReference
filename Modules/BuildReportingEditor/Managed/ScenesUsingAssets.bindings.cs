// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor.Build.Reporting
{
    ///<summary>An extension to the <see cref="BuildReport" /> class that tracks which scenes in the build have references to a specific asset in the build.</summary>
    ///<remarks>The build process generates this information when <see cref="BuildOptions.DetailedBuildReport" /> is used during a build.</remarks>
    [NativeType(Header = "Modules/BuildReportingEditor/Public/ScenesUsingAssets.h")]
    [NativeClass("BuildReporting::ScenesUsingAssets")]
    public sealed class ScenesUsingAssets : Object
    {
        ///<summary>An array of <see cref="ScenesUsingAsset" /> that holds information about the Assets that are included in the build.</summary>
        public ScenesUsingAsset[] list
        {
            get { return GetList(); }
        }
        internal extern ScenesUsingAsset[] GetList();
    }
}
