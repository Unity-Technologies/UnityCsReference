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

        // The two proxy generators build the table field list independently, so only a runtime compare catches a mismatch
        public static void ValidateFcnPtrStructSize(int nativeSize, int managedSize, string assemblyName)
        {
            if (nativeSize != managedSize)
            {
                throw new InvalidOperationException(
                    "Proxy function pointer table size mismatch for " + assemblyName + ": native struct is " +
                    nativeSize + " bytes, managed struct is " + managedSize +
                    " bytes. The native and managed proxy generators disagree on which methods get a table entry; " +
                    "populating the table would write outside the native allocation.");
            }
        }
    }
}
