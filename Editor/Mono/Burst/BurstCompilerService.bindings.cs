// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.Bindings;
using System.Reflection;

namespace Unity.Burst.LowLevel
{
    [NativeHeader("Runtime/Burst/Burst.h")]
    [NativeHeader("Runtime/Burst/BurstDelegateCache.h")]
    [StaticAccessor("BurstCompilerService::Get()", StaticAccessorType.Arrow)]
    internal static partial class BurstCompilerService
    {
        [NativeMethod("Initialize")]
        static extern string InitializeInternal(string path, ExtractCompilerFlags extractCompilerFlags);

        public static extern string GetDisassembly(MethodInfo m, string compilerOptions);

        [FreeFunction]
        public static extern int CompileAsyncDelegateMethod(object delegateMethod, string compilerOptions);
        [FreeFunction]
        public unsafe static extern void* GetAsyncCompiledAsyncDelegateMethod(int userID);

        public static extern bool IsInitialized { get; }
    }
}
