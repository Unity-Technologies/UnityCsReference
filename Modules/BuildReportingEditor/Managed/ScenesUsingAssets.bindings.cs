// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor.Build.Reporting
{
    [NativeType(Header = "Modules/BuildReportingEditor/Public/ScenesUsingAssets.h")]
    [NativeClass("BuildReporting::ScenesUsingAssets")]
    public sealed class ScenesUsingAssets : Object
    {
        public ScenesUsingAsset[] list
        {
            get { return GetList(); }
        }
        internal extern ScenesUsingAsset[] GetList();
    }
}
