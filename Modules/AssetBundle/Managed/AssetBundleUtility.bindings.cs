// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("Modules/AssetBundle/Public/AssetBundlePatching.h")]
    internal static class AssetBundleUtility
    {
        [FreeFunction]
        internal static extern void PatchAssetBundles(AssetBundle[] bundles, string[] filenames);
    }
}
