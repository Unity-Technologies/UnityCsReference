// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;
using Unity.Scripting.LifecycleManagement;
using Assert = UnityEngine.Assertions.AssertCore;
using Debug = Unity.Scripting.LowLevel.Debug;

namespace Unity.Entities
{
    enum UnmanagedSystemFunctionType
    {
        OnCreate,
        OnUpdate,
        OnDestroy,
        OnStartRunning,
        OnStopRunning,
        OnCreateForCompiler,

        Count
    }

    unsafe struct UnmanagedComponentSystemDelegates
    {
        // The function to call from a burst context to create/update/destroy.
        internal fixed ulong BurstFunctions[(int)UnmanagedSystemFunctionType.Count];

        // The function to call from a managed context to create/update/destroy.
        internal fixed ulong ManagedFunctions[(int)UnmanagedSystemFunctionType.Count];

        // Maintain a reference to any burst->managed delegate wrapper so they are not collected
        internal fixed ulong GCDefeat1[(int)UnmanagedSystemFunctionType.Count];

        internal ushort PresentFunctionBits;
        internal ushort BurstFunctionBits;

        internal void Dispose()
        {
            for (int i = 5; i >= 0; --i)
            {
                if (ManagedFunctions[i] != 0)
                {
                    GCHandle.FromIntPtr((IntPtr)ManagedFunctions[i]).Free();
                }

                if (GCDefeat1[i] != 0)
                {
                    GCHandle.FromIntPtr((IntPtr)GCDefeat1[i]).Free();
                }
            }
        }
    }

    unsafe struct UnmanagedSystemTypeRegistryData
    {
        UnsafeParallelHashMap<long, int> m_TypeHashToIndex;
        UnsafeList<UnmanagedComponentSystemDelegates> m_Delegates;

        public bool Constructed => m_Delegates.Ptr != null;

        internal void Construct()
        {
            m_TypeHashToIndex = new UnsafeParallelHashMap<long, int>(64, Allocator.Persistent);
            m_Delegates = new UnsafeList<UnmanagedComponentSystemDelegates>(64, Allocator.Persistent);
        }

        internal void Dispose()
        {
            if (Constructed)
            {
                for (int i = 0; i < m_Delegates.Length; ++i)
                {
                    m_Delegates[i].Dispose();
                }

                m_Delegates.Dispose();
                m_TypeHashToIndex.Dispose();
            }

            this = default;
        }

        internal int AddSystemType(long typeHash, UnmanagedComponentSystemDelegates delegates)
        {
            if (m_TypeHashToIndex.TryGetValue(typeHash, out int index))
            {
                m_Delegates[index] = delegates;
                return index;
            }
            else
            {
                int newIndex = m_Delegates.Length;
                m_TypeHashToIndex.Add(typeHash, newIndex);
                m_Delegates.Add(delegates);
                return newIndex;
            }
        }

        internal bool FindSystemMetaIndex(long typeHash, out int index)
        {
            return m_TypeHashToIndex.TryGetValue(typeHash, out index);
        }

        internal ref readonly UnmanagedComponentSystemDelegates GetSystemDelegates(int index)
        {
            return ref m_Delegates.Ptr[index];
        }
    }

    /// <summary>
    /// Internal class used by codegen (as such it is necessary to be public). For registering unmanaged systems
    /// prefer <seealso cref="World.AddSystem"/>
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public static partial class SystemBaseRegistry
    {
        class Managed
        {
            [NoAutoStaticsCleanup] // OnCodeUnloading resets it.
            public static List<Type> s_StructTypes = null;
            [NoAutoStaticsCleanup]
            public static List<RegistrationEntry> s_PendingRegistrations;
        }

        struct Dummy
        {
        }

        internal readonly static SharedStatic<UnmanagedSystemTypeRegistryData> s_Data = SharedStatic<UnmanagedSystemTypeRegistryData>.GetOrCreate<Dummy>();

        [OnCodeUnloading]
        static void OnCodeUnloading()
        {
            s_Data.Data.Dispose();
            Managed.s_PendingRegistrations = null;
            Managed.s_StructTypes = null;
        }

        /// <summary>
        /// Calls one of an unmanaged system's lifecycle methods. Generated code registers these for each system type.
        /// </summary>
        /// <param name="systemPtr">A pointer to the system struct.</param>
        /// <param name="state">A pointer to the system's <see cref="SystemState"/>.</param>
        public delegate void ForwardingFunc(IntPtr systemPtr, IntPtr state);

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private static void AssertTypeRegistryInitialized(in UnmanagedSystemTypeRegistryData data)
        {
            if (!data.Constructed)
                throw new InvalidOperationException("type registry is not initialized");
        }

        [GenerateTestsForBurstCompatibility]
        internal static int GetSystemTypeMetaIndex(long typeHash)
        {
            ref var data = ref s_Data.Data;
            AssertTypeRegistryInitialized(in data);

            if (!data.FindSystemMetaIndex(typeHash, out int index))
                index = -1;
            return index;
        }

        private class RegistrationEntry
        {
            public Type m_Type;
            public long m_TypeHash;
            public ForwardingFunc[] m_Functions;
            public string m_DebugName;
            public int m_BurstCompileBits;
        }

        [UnityEngine.Internal.ExcludeFromDocs]
        [ExcludeFromBurstCompatTesting("Takes managed Type")]
        public static unsafe void AddUnmanagedSystemType(Type type, long typeHash, ForwardingFunc onCreate, ForwardingFunc onUpdate,
            ForwardingFunc onDestroy, ForwardingFunc onStartRunning, ForwardingFunc onStopRunning, ForwardingFunc onCreateForCompiler,
            string debugName, int burstCompileBits)
        {
            //Debug.Log($"Adding unmanaged system type {debugName}, bcb={burstCompileBits}");

            // Lazily create list to hold pending work items as needed. This will be called from early initialization code and we need to defer burst compilation.
            if (Managed.s_PendingRegistrations == null)
            {
                Managed.s_PendingRegistrations = new List<RegistrationEntry>();
                Managed.s_StructTypes = new List<Type>();

                ref var data = ref s_Data.Data;
                if (!data.Constructed)
                    data.Construct();
            }

            // The order/number here must match UnmanagedSystemFunctionType
            var functions = new[] { onCreate, onUpdate, onDestroy, onStartRunning, onStopRunning, onCreateForCompiler };
            Assert.AreEqual(functions.Length, (int)UnmanagedSystemFunctionType.Count);

            // Buffer the data
            Managed.s_PendingRegistrations.Add(new RegistrationEntry
            {
                m_Type = type,
                m_TypeHash = typeHash,
                m_Functions = functions,
                m_DebugName = debugName,
                m_BurstCompileBits = burstCompileBits
            });
        }

        [ExcludeFromBurstCompatTesting("Uses managed delegate")]
        static void SelectManagedFn(out ulong result, ref ulong burstFn, ForwardingFunc managedFn)
        {
            if (burstFn != 0)
            {
                var fp = new FunctionPointer<ForwardingFunc>((IntPtr)burstFn);
                try
                {
                    result = (ulong)GCHandle.ToIntPtr(GCHandle.Alloc(fp.Invoke));
                    return;
                }
                catch(InvalidCastException)
                {
                    // Asset ImportWorkers are occasionally throwing InvalidCastExceptions. Provide some debug information when this happens
                    // then fall through to use the managed delegate directly rather than the delegate returned from Burst. DOTS-8094
                    Debug.LogWarning($"SelectManagedFn failed to cast {fp.GetType()}. BurstFn: {burstFn}.Forwarding Func: {managedFn.GetType()}. Falling back to managed delegate. Please report this as a bug using Help > Report a Bug...");
                    burstFn = 0;
                }
            }

            result = (ulong)GCHandle.ToIntPtr(GCHandle.Alloc(managedFn));
        }

        [ExcludeFromBurstCompatTesting("Uses managed delegate")]
        static void SelectBurstFn(out ulong result, out ulong defeatGc, ulong burstFn, ForwardingFunc managedFn)
        {
            if (burstFn != default)
            {
                result = burstFn;
                defeatGc = default;
            }
            else
            {
                ForwardingFunc wrapper = (IntPtr system, IntPtr state) =>
                {
                    try
                    {
                        managedFn(system, state);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                };

                defeatGc = (ulong)GCHandle.ToIntPtr(GCHandle.Alloc(wrapper));
                result = (ulong)Marshal.GetFunctionPointerForDelegate(wrapper);
            }
        }

        [UnityEngine.Internal.ExcludeFromDocs]
        [ExcludeFromBurstCompatTesting("Uses managed delegates")]
        public unsafe static void InitializePendingTypes()
        {
            if (Managed.s_PendingRegistrations == null)
                return;

            foreach (var r in Managed.s_PendingRegistrations)
            {
                var burstCompileBits = r.m_BurstCompileBits;
                var delegates = default(UnmanagedComponentSystemDelegates);

                ushort functionBit = 1;
                delegates.BurstFunctionBits = 0;
                for (int i = 0; i < r.m_Functions.Length; ++i, functionBit <<= 1)
                {
                    var dlg = r.m_Functions[i];

                    if (dlg != null)
                    {
                        var useBurstFunction = (burstCompileBits & functionBit) != 0;
                        ulong burstFunc = useBurstFunction ? (ulong)BurstCompiler.CompileFunctionPointer(dlg).Value : 0;

                        // Select what to call when calling into a system from managed code.
                        SelectManagedFn(out delegates.ManagedFunctions[i], ref burstFunc, dlg);

                        // Select what to call when calling into a system from Burst code.
                        SelectBurstFn(out delegates.BurstFunctions[i], out delegates.GCDefeat1[i], burstFunc, dlg);

                        delegates.PresentFunctionBits |= functionBit;
                        if (useBurstFunction)
                            delegates.BurstFunctionBits |= functionBit;
                    }
                }

                Managed.s_StructTypes.Add(r.m_Type);
                s_Data.Data.AddSystemType(r.m_TypeHash, delegates);
            }

            Managed.s_PendingRegistrations = null;
        }

        [BurstDiscard]
        internal static void CheckBurst(ref bool status)
        {
            status = false;
        }

        [Burst.CompilerServices.IgnoreWarning(1371)]
        static unsafe void CallForwardingFunction(SystemState* systemState, UnmanagedSystemFunctionType functionType)
        {
            var metaIndex = systemState->UnmanagedMetaIndex;
            var systemPointer = systemState->m_SystemPtr;
            var delegates = s_Data.Data.GetSystemDelegates(metaIndex);
            bool isBurst = true;
            CheckBurst(ref isBurst);

            var functionIndex = (int)functionType;
            if (0 != (delegates.PresentFunctionBits & (1 << functionIndex)))
            {
                if (isBurst)
                {
                    // Burst: we're calling either directly into Burst code, or we are calling into a managed wrapper.
                    // In any case, creating the function pointer from the IntPtr is free.
                    new FunctionPointer<ForwardingFunc>((IntPtr)delegates.BurstFunctions[functionIndex]).Invoke((IntPtr)systemPointer, (IntPtr)systemState);
                }
                else
                {
                    // We're in managed land. We may be calling into either a managed routine, or into Burst code.
                    // We have a managed delegate GCHandle ready to go.
                    var delegatePtr = (IntPtr)delegates.ManagedFunctions[functionIndex];
                    ForwardToManaged(delegatePtr, systemState, systemPointer);
                }
            }
        }

        internal static unsafe ref readonly UnmanagedComponentSystemDelegates GetDelegates(SystemState* systemState)
        {
            var metaIndex = systemState->UnmanagedMetaIndex;
            return ref s_Data.Data.GetSystemDelegates(metaIndex);
        }

        // Returns if OnUpdate is going to use burst.
        // This method is unfortunately only an approximation.
        // It doesn't work if:
        // * Burst failed to compile
        // * Burst has completed compilation of the method yet
        // Unfortunately there is no way to get this data from burst yet.
        // BUR-1651
        internal unsafe static bool IsOnUpdateUsingBurst(in SystemState* state)
        {
            bool isBurst = true;
            CheckBurst(ref isBurst);

            ref readonly var delegates = ref GetDelegates(state);
            return isBurst & (delegates.BurstFunctionBits & 2) != 0;
        }

        [BurstDiscard]
        private static unsafe void ForwardToManaged(IntPtr delegateIntPtr, SystemState* systemState, void* systemPointer)
        {
            GCHandle h = GCHandle.FromIntPtr(delegateIntPtr);
            ((ForwardingFunc)h.Target)((IntPtr)systemPointer, (IntPtr)systemState);
        }

        [GenerateTestsForBurstCompatibility]
        internal static unsafe void CallOnCreate(SystemState* systemState)
        {
            CallForwardingFunction(systemState, UnmanagedSystemFunctionType.OnCreate);
        }

        [GenerateTestsForBurstCompatibility]
        internal static unsafe void CallOnUpdate(SystemState* systemState)
        {
            CallForwardingFunction(systemState, UnmanagedSystemFunctionType.OnUpdate);
        }

        [GenerateTestsForBurstCompatibility]
        internal static unsafe void CallOnDestroy(SystemState* systemState)
        {
            CallForwardingFunction(systemState, UnmanagedSystemFunctionType.OnDestroy);
        }

        [GenerateTestsForBurstCompatibility]
        internal static unsafe void CallOnStartRunning(SystemState* systemState)
        {
            CallForwardingFunction(systemState, UnmanagedSystemFunctionType.OnStartRunning);
        }

        [GenerateTestsForBurstCompatibility]
        internal static unsafe void CallOnStopRunning(SystemState* systemState)
        {
            CallForwardingFunction(systemState, UnmanagedSystemFunctionType.OnStopRunning);
        }

        [GenerateTestsForBurstCompatibility]
        internal static unsafe void CallOnCreateForCompiler(SystemState* systemState)
        {
            CallForwardingFunction(systemState, UnmanagedSystemFunctionType.OnCreateForCompiler);
        }

        [ExcludeFromBurstCompatTesting("returns managed Type")]
        internal static Type GetStructType(int metaIndex)
        {
            return Managed.s_StructTypes[metaIndex];
        }
    }
}
