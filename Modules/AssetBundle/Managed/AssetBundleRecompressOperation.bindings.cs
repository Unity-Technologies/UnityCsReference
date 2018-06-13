// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // Asynchronous recompression operation for a downloaded [[AssetBundle]] from its original compression method to one of the runtime supported methods.
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleRecompressOperation.h")]
    public class AssetBundleRecompressOperation : AsyncOperation
    {
        // String describing the result of recompressing the [[AssetBundle]]
        public extern string humanReadableResult
        {
            [NativeMethod("GetResultStr")]
            get;
        }

        public extern string inputPath
        {
            [NativeMethod("GetInputPath")]
            get;
        }

        public extern string outputPath
        {
            [NativeMethod("GetOutputPath")]
            get;
        }

        public extern AssetBundleLoadResult result
        {
            [NativeMethod("GetResult")]
            get;
        }

        public extern bool success
        {
            [NativeMethod("GetSuccess")]
            get;
        }
    }
}
