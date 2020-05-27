// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using System.Reflection;
using UnityEngine;

namespace Unity.Burst.LowLevel
{
    [NativeHeader("Runtime/Burst/Burst.h")]
    [NativeHeader("Runtime/Burst/BurstDelegateCache.h")]
    [StaticAccessor("BurstCompilerService::Get()", StaticAccessorType.Arrow)]
    internal static partial class BurstCompilerService
    {
        [NativeMethod("Initialize")]
        static extern string InitializeInternal(string path, ExtractCompilerFlags extractCompilerFlags);

        [ThreadSafe]
        public static extern string GetDisassembly(MethodInfo m, string compilerOptions);

        [FreeFunction]
        public static extern int CompileAsyncDelegateMethod(object delegateMethod, string compilerOptions);

        [FreeFunction]
        public static extern unsafe void* GetAsyncCompiledAsyncDelegateMethod(int userID);

        [ThreadSafe]
        public static extern unsafe void* GetOrCreateSharedMemory(ref Hash128 key, uint size_of, uint alignment);

        [ThreadSafe]
        public static extern string GetMethodSignature(MethodInfo method);

        public static extern bool IsInitialized { get; }

        [ThreadSafe]
        public static extern void SetCurrentExecutionMode(uint environment);

        [ThreadSafe]
        public static extern uint GetCurrentExecutionMode();

        [FreeFunction("DefaultBurstLogCallback", isThreadSafe: true)]
        public static extern unsafe void Log(void* userData, BurstLogType logType, byte* message, byte* filename, int lineNumber);
    }
}
