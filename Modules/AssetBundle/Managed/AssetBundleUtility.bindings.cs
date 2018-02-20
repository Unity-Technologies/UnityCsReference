// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEngine.Experimental.AssetBundlePatching
{
    [NativeHeader("Modules/AssetBundle/Public/AssetBundlePatching.h")]
    public static class AssetBundleUtility
    {
        [FreeFunction]
        public static extern void PatchAssetBundles(AssetBundle[] bundles, string[] filenames);
    }
}
