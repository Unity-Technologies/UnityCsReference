// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Unity.Burst;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static partial class UnsafeUtility
    {
        private static bool IsBlittableValueType(Type t) { return t.IsValueType && IsBlittable(t); }

        private static string GetReasonForTypeNonBlittableImpl(Type t, string name)
        {
            if (!t.IsValueType)
                return String.Format("{0} is not blittable because it is not of value type ({1})\n", name, t);
            if (t.IsPrimitive)
                return String.Format("{0} is not blittable ({1})\n", name, t);

            string ret = "";
            foreach (FieldInfo f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!IsBlittableValueType(f.FieldType))
                    ret += GetReasonForTypeNonBlittableImpl(f.FieldType, String.Format("{0}.{1}", name, f.Name));
            }
            return ret;
        }

        // while it would make sense to have functions like ThrowIfArgumentIsNonBlittable
        // currently we insist on including part of call stack into exception message in these cases
        // e.g. "T used in NativeArray<T> must be blittable"
        // due to that we will need to pass message string to this function
        //   but most of the time we will be creating it using string.Format and it will happen on every check
        //   instead of "only if we fail check for is-blittable"
        // thats why we provide the means to implement this pattern on your code (but not function itself)

        internal static bool IsArrayBlittable(Array arr)
        {
            return IsBlittableValueType(arr.GetType().GetElementType());
        }

        internal static bool IsGenericListBlittable<T>() where T : struct
        {
            return IsBlittable<T>();
        }

        internal static string GetReasonForArrayNonBlittable(Array arr)
        {
            Type t = arr.GetType().GetElementType();
            return GetReasonForTypeNonBlittableImpl(t, t.Name);
        }

        internal static string GetReasonForGenericListNonBlittable<T>() where T : struct
        {
            Type t = typeof(T);
            return GetReasonForTypeNonBlittableImpl(t, t.Name);
        }

        internal static string GetReasonForTypeNonBlittable(Type t)
        {
            return GetReasonForTypeNonBlittableImpl(t, t.Name);
        }

        internal static string GetReasonForValueTypeNonBlittable<T>() where T : struct
        {
            Type t = typeof(T);
            return GetReasonForTypeNonBlittableImpl(t, t.Name);
        }

        // Since burst would fail to compile managed types anyway, we can default to unmanaged and
        // conditionally mark as managed in managed code paths
        // These flags must be kept in sync with kScriptingTypeXXX flags in Scripting.cpp
        const int kIsManaged = 0x01;
        const int kIsNativeContainer = 0x02;

        // Supports burst compatible invocation
        internal struct TypeFlagsCache<T>
        {
            internal static readonly int flags;

            static TypeFlagsCache()
            {
                Init(ref flags);
            }

            [BurstDiscard]
            static void Init(ref int flags)
            {
                flags = GetScriptingTypeFlags(typeof(T));
            }
        }

        public static bool IsUnmanaged<T>()
        {
            return (TypeFlagsCache<T>.flags & kIsManaged) == 0;
        }

        public static bool IsNativeContainerType<T>()
        {
            return (TypeFlagsCache<T>.flags & kIsNativeContainer) != 0;
        }

        // Keeping this to support Collections pre-2.0 and user defined containers. To support nested native containers,
        // code will need switched to
        // - checking IsUnmanaged (if constrained to struct instead of unmanaged)
        // - checking IsNativeContainer
        // - properly set up safety handles
        public static bool IsValidNativeContainerElementType<T>()
        {
            return TypeFlagsCache<T>.flags == 0;  // not managed, not a container
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AlignOfHelper<T> where T : struct
        {
            public byte dummy;
            public T data;
        }

        // minimum alignment of a struct
        public static int AlignOf<T>() where T : struct
        {
            return SizeOf<AlignOfHelper<T>>() - SizeOf<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe Span<byte> GetByteSpanFromArray(System.Array array, int elementSize)
        {
            if (array == null || array.Length == 0)
                return new Span<byte>();

            System.Diagnostics.Debug.Assert(UnsafeUtility.SizeOf(array.GetType().GetElementType()) == elementSize);

            var bArray = UnsafeUtility.As<System.Array, byte[]>(ref array);
            return new Span<byte>(UnsafeUtility.AddressOf(ref bArray[0]), array.Length * elementSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Span<byte> GetByteSpanFromList<T>(List<T> list) where T : struct
        {
            return MemoryMarshal.AsBytes(NoAllocHelpers.ExtractArrayFromList(list).AsSpan());
        }
    }
}
