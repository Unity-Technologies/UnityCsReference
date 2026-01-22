// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace UnityEngine.Bindings
{
    [VisibleToOtherModules]
    internal static class NativeInvokeLookupHelpers
    {
        public static IntPtr GetMethodFunctionPointerSafe(Type declaringType, string methodName, BindingFlags bindingFlags)
        {
            return declaringType.GetMethod(methodName, bindingFlags)?.MethodHandle.GetFunctionPointer() ?? IntPtr.Zero;
        }

        public static Delegate GetMethodDelegateSafe(Type declaringType, string methodName, Type delegateType, BindingFlags bindingFlags)
        {
            return declaringType.GetMethod(methodName, bindingFlags)?.CreateDelegate(delegateType);
        }

        public static IntPtr GetFunctionPointerFromDelegateSafe(Delegate adelegate)
        {
            return adelegate != null ? Marshal.GetFunctionPointerForDelegate(adelegate) : IntPtr.Zero;
        }
    }
}
