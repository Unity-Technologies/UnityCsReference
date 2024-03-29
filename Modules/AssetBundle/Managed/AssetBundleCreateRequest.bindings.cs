// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using UnityEngineInternal;

namespace UnityEngine
{
    // Asynchronous create request for an [[AssetBundle]].
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleLoadFromAsyncOperation.h")]
    public class AssetBundleCreateRequest : AsyncOperation
    {
        public extern UnityEngine.AssetBundle assetBundle
        {
            [NativeMethod("GetAssetBundleBlocking")]
            get;
        }

        [NativeMethod("SetEnableCompatibilityChecks")]
        private extern void SetEnableCompatibilityChecks(bool set);
        internal void DisableCompatibilityChecks()
        {
            SetEnableCompatibilityChecks(false);
        }

        public AssetBundleCreateRequest() { }

        private AssetBundleCreateRequest(IntPtr ptr) : base(ptr)
        { }

        new internal static class BindingsMarshaller
        {
            public static AssetBundleCreateRequest ConvertToManaged(IntPtr ptr) => new AssetBundleCreateRequest(ptr);
            public static IntPtr ConvertToNative(AssetBundleCreateRequest assetBundleCreateRequest) => assetBundleCreateRequest.m_Ptr;
        }
    }
}
