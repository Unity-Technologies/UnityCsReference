// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Reflection;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Scripting.LifecycleManagement;

namespace Unity.Burst.LowLevel
{
    [NativeHeader("Modules/Burst/Include/Burst/Burst.h")]
    [NativeHeader("Modules/Burst/Include/Burst/BurstDelegateCache.h")]
    [StaticAccessor("BurstCompilerService::Get()", StaticAccessorType.Arrow)]
    [VisibleToOtherModules]
    internal static partial class BurstCompilerService
    {
        [NativeMethod("ReloadAssemblySearchPathsForBurst", ThrowsException = true)]
        public static extern void ReloadAssemblySearchPathsForBurstInternal();

        [ClearCacheBetweenCodeLoads]
        static void ClearCacheBetweenCodeLoads() => ReloadAssemblySearchPathsForBurstInternal();

        [NativeMethod("Initialize")]
        static extern string InitializeInternal(string path, ExtractCompilerFlags extractCompilerFlags);

        [NativeMethod(IsThreadSafe = true)]
        public static extern string GetDisassembly(MethodInfo m, string compilerOptions);

        [FreeFunction(IsThreadSafe = true)]
        public static extern int CompileAsyncDelegateMethod(object delegateMethod, string compilerOptions);

        [FreeFunction(IsThreadSafe = true)]
        public static extern unsafe void* GetAsyncCompiledAsyncDelegateMethod(int userID);

        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe void* GetOrCreateSharedMemory(long keyLow, long keyHigh, uint size_of, uint alignment);

        [NativeMethod(IsThreadSafe = true)]
        public static extern string GetMethodSignature(MethodInfo method);

        public static extern bool IsInitialized { get; }

        public static extern bool DequeuePendingBurstLoad();
        public static extern bool WasScriptDebugInfoEnabledAtDomainReload { get; }

        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetCurrentExecutionMode(uint environment);

        [NativeMethod(IsThreadSafe = true)]
        public static extern uint GetCurrentExecutionMode();

        [FreeFunction("DefaultBurstLogCallback", isThreadSafe: true)]
        public static extern unsafe void Log(void* userData, BurstLogType logType, byte* message, byte* filename, int lineNumber);

        [FreeFunction("DefaultBurstRuntimeLogCallback", isThreadSafe: true)]
        public static extern unsafe void RuntimeLog(void* userData, BurstLogType logType, byte* message, byte* filename, int lineNumber);

        public static extern bool LoadBurstLibrary(string fullPathToLibBurstGenerated);

       [RequiredByNativeCode]
        private static MethodInfo GetMethodInfoForDelegate(System.Delegate targetMethod)
        {
            return targetMethod.Method;
        }

        [RequiredByNativeCode]
        static void InvokeReset([UnityMarshalAs(NativeType.GCHandle)] object instance, MethodInfo methodHandlePtr)
        {
            methodHandlePtr.Invoke(instance, null);
        }

        [RequiredByNativeCode]
        static void InvokeCompileInternal(
            [UnityMarshalAs(NativeType.GCHandle)] object instance,
            MethodInfo methodHandlePtr,
            string fullMethodName,
            string assemblyPaths,
            IntPtr userdata,
            int dumpFlags,
            IntPtr compilerCallbackPointer,
            IntPtr logCallBack,
            string compilerFlags)
        {
            
            methodHandlePtr.Invoke(
                instance,
                new object[]
                {
                    fullMethodName,
                    assemblyPaths,
                    userdata,
                    dumpFlags,
                    compilerCallbackPointer,
                    logCallBack,
                    compilerFlags
                });
        }

        [RequiredByNativeCode]
        static void InvokeSetNativeGetExternalFunctionPointer(
            [UnityMarshalAs(NativeType.GCHandle)] object instance,
            MethodInfo methodHandlePtr,
            IntPtr externalFunctionCallback)
        {
            methodHandlePtr.Invoke(
                instance,
                new object[]
                {
                    externalFunctionCallback
                });
        }

        [RequiredByNativeCode]
        static string InvokeExtractCompilerFlags(
            Delegate extractCompilerFlagsDelegate,
            IntPtr jobTypeHandle,
            ref bool didGetFlags)
        {
            ExtractCompilerFlags typedDelegate = (ExtractCompilerFlags)extractCompilerFlagsDelegate;

            var jobType = SystemReflectionMarshalling.UnmarshalSystemType(jobTypeHandle);

            didGetFlags = typedDelegate.Invoke(jobType, out var compilerFlags);
            return compilerFlags;
        }
    }
}
