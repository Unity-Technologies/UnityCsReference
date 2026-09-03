// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: ScriptingRuntime not yet converted
#nullable enable
using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/BaseClasses/TypeManager.h")]
    [NoAutoStaticsCleanup]
    public partial class TypeManagerV2
    {
        static readonly Dictionary<RuntimeTypeHandle, int> s_TypeHandleToRuntimeIndex = new Dictionary<RuntimeTypeHandle, int>();

        public static void RegisterFactory(RuntimeTypeHandle typeHandle, string typeName, IntPtr factoryPtr,
                                           int hardcodedPersistentId = 0)
        {
            if (factoryPtr == IntPtr.Zero)
                Debug.LogError($"TypeManagerV2.RegisterFactory: factoryPtr is null for type '{typeName}'");

            // Native owns persistent-id assignment. For hybrid types the caller passes the
            // hardcoded IMPLEMENT_REGISTER_CLASS id; for pure-managed types unique hashes are generated
            var runtimeTypeIndex = RegisterInstantiationFunctionManaged(typeName, factoryPtr, hardcodedPersistentId);
            s_TypeHandleToRuntimeIndex[typeHandle] = runtimeTypeIndex;
        }

        // Metadata only, a value-type factory would box. hardcodedPersistentId binds a hybrid to native's RTTI.
        public static void RegisterType(RuntimeTypeHandle typeHandle, string typeName,
                                        int hardcodedPersistentId = 0)
        {
            var runtimeTypeIndex = RegisterInstantiationFunctionManaged(typeName, IntPtr.Zero, hardcodedPersistentId);
            s_TypeHandleToRuntimeIndex[typeHandle] = runtimeTypeIndex;
        }

        // Used in testing only
        internal static unsafe object? Produce(RuntimeTypeHandle typeHandle)
        {
            if (!s_TypeHandleToRuntimeIndex.TryGetValue(typeHandle, out int runtimeTypeIndex))
            {
                Debug.LogError($"TypeManagerV2.Produce: type '{Type.GetTypeFromHandle(typeHandle)}' is not registered");
                return null;
            }

            IntPtr factoryPtr = GetManagedFactoryPtr(runtimeTypeIndex);
            if (factoryPtr == IntPtr.Zero)
            {
                Debug.LogError($"TypeManagerV2.Produce: factory function is null for type '{Type.GetTypeFromHandle(typeHandle)}'");
                return null;
            }

            return ((delegate*<object>)factoryPtr)();
        }

        // Used in testing only
        internal static int GetRuntimeTypeId(RuntimeTypeHandle typeHandle)
        {
            return s_TypeHandleToRuntimeIndex[typeHandle];
        }

        // The native side caches a factory IntPtr per registered type. Clearing the map on
        // unload prevents Produce from reaching a stale function pointer and lets the next
        // assembly load re-establish the lookup cleanly.
        [OnAssemblyUnloading]
        internal static void OnAssemblyUnloading()
        {
            ClearManagedFactoriesForUnload();
            s_TypeHandleToRuntimeIndex.Clear();
        }

        [NativeMethod(Name = "TypeManager::RegisterInstantiationFunctionManaged", IsFreeFunction = true)]
        extern static int RegisterInstantiationFunctionManaged(string name, IntPtr initFunc, int hardcodedPersistentId);

        [NativeMethod(Name = "TypeManager::GetManagedFactoryPtr", IsFreeFunction = true)]
        extern static IntPtr GetManagedFactoryPtr(int runtimeTypeIndex);

        [NativeMethod(Name = "TypeManager::ClearManagedFactoriesForUnload", IsFreeFunction = true)]
        extern static void ClearManagedFactoriesForUnload();
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
