// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;

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

        internal struct IsUnmanagedCache<T>
        {
            internal static int value; // 0 == unknown, -1 false, 1 true
        }

        public static bool IsUnmanaged<T>()
        {
            int value = IsUnmanagedCache<T>.value;
            if (value == 1)  // most common case
                return true;
            if (value == 0)
                IsUnmanagedCache<T>.value = value = IsUnmanaged(typeof(T)) ? 1 : -1;
            return value == 1;
        }

        internal struct IsValidNativeContainerElementTypeCache<T>
        {
            internal static int value; // 0 == unknown, -1 false, 1 true
        }

        public static bool IsValidNativeContainerElementType<T>()
        {
            int value = IsValidNativeContainerElementTypeCache<T>.value;
            if (value == -1)  // most common case
                return false;
            if (value == 0)
                IsValidNativeContainerElementTypeCache<T>.value = value = IsValidNativeContainerElementType(typeof(T)) ? 1 : -1;
            return value == 1;
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
    }
}
