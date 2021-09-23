// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngineInternal;

namespace UnityEngine
{
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleUnloadOperation.h")]
    public class AssetBundleUnloadOperation : AsyncOperation
    {
        [NativeMethod("WaitForCompletion")]
        public extern void WaitForCompletion();
    }
}
