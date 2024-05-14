// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEngine.Analytics
{
    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    class ShaderRuntimeInfoAnalytic : AnalyticsEventBase
    {
        ShaderRuntimeInfoAnalytic() : base("shaderRuntimeInfo", 1) { }

        [RequiredByNativeCode]
        public static ShaderRuntimeInfoAnalytic CreateShaderRuntimeInfoAnalytic() { return new ShaderRuntimeInfoAnalytic(); }

        public long VariantsRequested = 0;              // The total amount of shader variants requested during the application's lifetime
        public long VariantsRequestedMissing = 0;       // The total amount of shader variants requested but missing from the build, during the application's lifetime
        public long VariantsRequestedUnsupported = 0;   // The total amount of shader variants requested but not supported by the hardware, during the application's lifetime
        public long VariantsRequestedCompiled = 0;      // The total amount of shader variants requiested and compiled, during the application's lifetime
        public long VariantsRequestedViaWarmup = 0;     // The total amount of shader variants requested via shader warmup, during the application's lifetime
        public long VariantsUnused = 0;                 // The total amount of shader variants included in the build but never requested during the application's lifetime

        public int VariantsCompilationTimeTotal = 0;    // The total amount of time (in ms) spent compiling shaders during the application's lifetime
        public int VariantsCompilationTimeMax = 0;      // The maximum amount of time (in ms) spent compiling shaders during the application's lifetime (per frame)
        public int VariantsCompilationTimeMedian = 0;   // The median amount of time (in ms) spent compiling shaders during the application's lifetime (per frame)
        public int VariantsWarmupTimeTotal = 0;         // The total amount of time (in ms) spent prewarming shaders during the application's lifetime
        public int VariantsWarmupTimeMax = 0;           // The maximum amount of time (in ms) spent prewarming shaders during the application's lifetime (per warm-up)
        public int VariantsWarmupTimeMedian = 0;        // The median amount of time (in ms) spent prewarming shaders during the application's lifetime (per warm-up)

        public bool UseProgressiveWarmup = false;       // Progressive Warmup was used

        public int ShaderChunkSizeMin = 0;              // The minimum shader chunk size during the application's lifetime
        public int ShaderChunkSizeMax = 0;              // The maximum shader chunk size during the application's lifetime
        public int ShaderChunkSizeAvg = 0;              // The average shader chunk size during the application's lifetime
        public int ShaderChunkCountMin = 0;             // The minimum shader chunk count during the application's lifetime
        public int ShaderChunkCountMax = 0;             // The maximum shader chunk count during the application's lifetime
        public int ShaderChunkCountAvg = 0;             // The average shader chunk count during the application's lifetime
    }
}
