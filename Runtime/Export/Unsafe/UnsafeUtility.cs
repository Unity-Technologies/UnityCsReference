// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Collections.Generic;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static partial class UnsafeUtility
    {
        // just to hide .net API differences
        private static bool IsValueType(Type t)
        {
            return t.IsValueType;
        }

        private static bool IsPrimitive(Type t)
        {
            return t.IsPrimitive;
        }

        private static bool IsBlittableValueType(Type t) { return IsValueType(t) && IsBlittable(t); }

        private static string GetReasonForTypeNonBlittableImpl(Type t, string name)
        {
            if (!IsValueType(t))
                return String.Format("{0} is not blittable because it is not of value type ({1})\n", name, t);
            if (IsPrimitive(t))
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
    }
}
