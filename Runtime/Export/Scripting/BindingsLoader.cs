// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using UnityEngine.Scripting;

namespace UnityEngine.Bindings
{
    internal static class BindingsLoader
    {
        public static IntPtr GetBindingsFunctionsPointer(ulong hash1, ulong hash2)
        {
            var ptr = IntPtr.Zero;

            // Non-Burst code path
            GetBindingsFunctionsForModule(hash1, hash2, out ptr);
            if (ptr != IntPtr.Zero)
                return ptr;
                
            // Burst code path
            return BurstGetBindingsFunctionsForModuleWrapper(hash1, hash2);
        }

        // Burst support
        [BurstAuthorizedExternalMethod] // allow icall within static constructor
        [BindingsGeneratorIgnore]       // don't generate a wrapper for this, Burst registers handler for this method
        [MethodImpl(MethodImplOptions.InternalCall)]
        static extern IntPtr BurstGetBindingsFunctionsForModule(ulong hash1, ulong hash2);

        [MethodImpl(MethodImplOptions.NoInlining)] // avoid inlining so non-Burst JIT will never try to resolve the icall
        static IntPtr BurstGetBindingsFunctionsForModuleWrapper(ulong hash1, ulong hash2)
        {
            return BurstGetBindingsFunctionsForModule(hash1, hash2);
        }

        // Non-Burst support
        private static IntPtr _getBindingsFunctionsForModule;
        
        [RequiredByNativeCode]
        static void RegisterCallbackToNative(IntPtr getBindingsFunctionsForModule)
        {
            _getBindingsFunctionsForModule = getBindingsFunctionsForModule;
        }

        [BurstDiscard] // don't call in Burst code path
        static unsafe void GetBindingsFunctionsForModule(ulong hash1, ulong hash2, out IntPtr ptr)
        {
            ptr = ((delegate* unmanaged[Cdecl] <ulong, ulong, IntPtr>)_getBindingsFunctionsForModule)(hash1, hash2);
        }
    }
}
