// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
        public static extern unsafe void* GetAsyncCompiledAsyncDelegateMethod(int userID);

        public static extern string GetMethodSignature(MethodInfo method);

        public static extern bool IsInitialized { get; }
    }
}
