// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Bindings
{
    [VisibleToOtherModules]
    internal static class SystemReflectionMarshalling
    {
        public static Type[] UnmarshalSystemTypes(ReadOnlySpan<IntPtr> handlePtrs)
        {
            var types = new Type[handlePtrs.Length];
            for (int i = 0; i < handlePtrs.Length; ++i)
                types[i] = UnmarshalSystemType(handlePtrs[i]);
            return types;
        }

        public static Type UnmarshalSystemType(IntPtr handlePtr)
        {
            if (handlePtr == IntPtr.Zero)
                return null;
            var handle = UnsafeUtility.As<IntPtr, RuntimeTypeHandle>(ref handlePtr);
            return Type.GetTypeFromHandle(handle);
        }

        public static FieldInfo UnmarshalFieldInfo(IntPtr handlePtr)
        {
            if (handlePtr == IntPtr.Zero)
                return null;
            var handle = UnsafeUtility.As<IntPtr, RuntimeFieldHandle>(ref handlePtr);
            return FieldInfo.GetFieldFromHandle(handle);
        }

        public static MethodBase UnmarshalMethodBase(IntPtr handlePtr)
        {
            if (handlePtr == IntPtr.Zero)
                return null;

            var handle = UnsafeUtility.As<IntPtr, RuntimeMethodHandle>(ref handlePtr);
            return MethodBase.GetMethodFromHandle(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr MarshalSystemType(Type type)
        {
            return type != null ? type.TypeHandle.Value : IntPtr.Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr MarshalMethodBase(MethodBase methodBase)
        {
            return methodBase != null ? methodBase.MethodHandle.Value : IntPtr.Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr MarshalFieldInfo(FieldInfo fieldInfo)
        {
            return fieldInfo != null ? fieldInfo.FieldHandle.Value : IntPtr.Zero;
        }
    }
}


