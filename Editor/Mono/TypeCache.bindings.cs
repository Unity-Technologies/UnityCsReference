// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine.Bindings;

namespace UnityEditor
{
    // The bindings generator derives the native names referenced by the generated
    // [PreventExecutionInState] checks from this enum's name (g_TypeCachePreventExecutionBitField,
    // TypeCachePreventExecutionChecks, TypeCachePreventExecution::ReportExecutionPrevention).
    // Do not rename it independently of Runtime/Scripting/TypeCache.h, and keep the flag
    // values in sync with TypeCachePreventExecutionChecks there.
    internal enum TypeCachePreventExecution
    {
        kNoTypeCacheRestriction = 0,
        kTypeCacheNotYetRefreshed = 1 << 0,
    }

    [NativeHeader("Runtime/Scripting/TypeCache.h")]
    public static partial class TypeCache
    {
        const string k_TypeCacheNotYetRefreshedHowToFix = "Defer this query to a lifecycle callback such as [OnCodeInitializing] or [OnCodeLoaded].";

        [NativeMethod(IsThreadSafe = true)]
        [PreventExecutionInState(TypeCachePreventExecution.kTypeCacheNotYetRefreshed, PreventExecutionSeverity.PreventExecution_ManagedException, k_TypeCacheNotYetRefreshedHowToFix)]
        static extern Type[] Internal_GetTypesWithAttribute(Type attrType);

        [NativeMethod(IsThreadSafe = true)]
        [PreventExecutionInState(TypeCachePreventExecution.kTypeCacheNotYetRefreshed, PreventExecutionSeverity.PreventExecution_ManagedException, k_TypeCacheNotYetRefreshedHowToFix)]
        static extern MethodInfo[] Internal_GetMethodsWithAttribute(Type attrType);

        [NativeMethod(IsThreadSafe = true)]
        [PreventExecutionInState(TypeCachePreventExecution.kTypeCacheNotYetRefreshed, PreventExecutionSeverity.PreventExecution_ManagedException, k_TypeCacheNotYetRefreshedHowToFix)]
        static extern FieldInfo[] Internal_GetFieldsWithAttribute(Type attrType);

        [NativeMethod(IsThreadSafe = true)]
        [PreventExecutionInState(TypeCachePreventExecution.kTypeCacheNotYetRefreshed, PreventExecutionSeverity.PreventExecution_ManagedException, k_TypeCacheNotYetRefreshedHowToFix)]
        static extern Type[] Internal_GetTypesDerivedFromInterface(Type interfaceType);

        [NativeMethod(IsThreadSafe = true)]
        [PreventExecutionInState(TypeCachePreventExecution.kTypeCacheNotYetRefreshed, PreventExecutionSeverity.PreventExecution_ManagedException, k_TypeCacheNotYetRefreshedHowToFix)]
        static extern Type[] Internal_GetTypesDerivedFromType(Type parentType);

        [NativeMethod(IsThreadSafe = true)]
        [PreventExecutionInState(TypeCachePreventExecution.kTypeCacheNotYetRefreshed, PreventExecutionSeverity.PreventExecution_ManagedException, k_TypeCacheNotYetRefreshedHowToFix)]
        static extern Type[] Internal_GetTypesWithAttributeFromAssembly(Type attrType, string assemblyName);

        [NativeMethod(IsThreadSafe = true)]
        [PreventExecutionInState(TypeCachePreventExecution.kTypeCacheNotYetRefreshed, PreventExecutionSeverity.PreventExecution_ManagedException, k_TypeCacheNotYetRefreshedHowToFix)]
        static extern MethodInfo[] Internal_GetMethodsWithAttributeFromAssembly(Type attrType, string assemblyName);

        [NativeMethod(IsThreadSafe = true)]
        [PreventExecutionInState(TypeCachePreventExecution.kTypeCacheNotYetRefreshed, PreventExecutionSeverity.PreventExecution_ManagedException, k_TypeCacheNotYetRefreshedHowToFix)]
        static extern FieldInfo[] Internal_GetFieldsWithAttributeFromAssembly(Type attrType, string assemblyName);

        [NativeMethod(IsThreadSafe = true)]
        [PreventExecutionInState(TypeCachePreventExecution.kTypeCacheNotYetRefreshed, PreventExecutionSeverity.PreventExecution_ManagedException, k_TypeCacheNotYetRefreshedHowToFix)]
        static extern Type[] Internal_GetTypesDerivedFromInterfaceFromAssembly(Type interfaceType, string assemblyName);

        [NativeMethod(IsThreadSafe = true)]
        [PreventExecutionInState(TypeCachePreventExecution.kTypeCacheNotYetRefreshed, PreventExecutionSeverity.PreventExecution_ManagedException, k_TypeCacheNotYetRefreshedHowToFix)]
        static extern Type[] Internal_GetTypesDerivedFromTypeFromAssembly(Type parentType, string assemblyName);

        internal static extern ulong GetCurrentAge();

        // Raises/lowers the kTypeCacheNotYetRefreshed restriction so tests can verify the
        // generated [PreventExecutionInState] checks without having to run code inside the
        // reload window, which is not reachable from user code.
        internal static extern void Internal_SetPreventExecutionStateForTesting(bool restricted);
    }
}
