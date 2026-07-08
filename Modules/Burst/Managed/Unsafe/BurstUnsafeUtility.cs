// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using Unity.Burst;

namespace Unity.Burst.LowLevel.Unsafe
{
    [NativeHeader("Modules/Scripting/Include/Scripting/ScriptingUtility.h")]
    [VisibleToOtherModules]
    internal static class BurstUnsafeUtility
    {
        public unsafe static void MemCpy (void* destination, void* source, long size)
        {
            byte* dst = (byte*)destination;
            for (long i = 0; i < size; i++)
            {
                dst[i] = ((byte*)source)[i];
            }
        }

        [FreeFunction("Scripting::IsUnmanaged", IsThreadSafe = true)]
        internal static extern bool IsUnmanagedInternal(Type type);


        [BurstDiscard]
        private static void IsUnmanagedCheck(Type type, ref bool isUnmanaged)
        {
            isUnmanaged = IsUnmanagedInternal(type);
        }

        public static bool IsUnmanaged<T>()
        {
            bool isUnmanaged = true;
            IsUnmanagedCheck(typeof(T), ref isUnmanaged);
            return isUnmanaged;
        }
    }
}
