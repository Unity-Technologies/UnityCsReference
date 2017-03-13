// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Reflection;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [RequiredByNativeCode]
    internal class SetupCoroutine
    {
        [RequiredByNativeCode]
        [System.Security.SecuritySafeCritical]
        unsafe static public void InvokeMoveNext(IEnumerator enumerator, IntPtr returnValueAddress)
        {
            if (returnValueAddress == IntPtr.Zero)
                throw new ArgumentException("Return value address cannot be 0.", "returnValueAddress");
            (*(bool*)returnValueAddress) = enumerator.MoveNext();
        }

        [RequiredByNativeCode]
        static public object InvokeMember(object behaviour, string name, object variable)
        {
            // We need these stubs because methods marked with [RequiredByNativeCode] must match between scripting backends
            object[] args = null;
            if (variable != null)
            {
                args = new System.Object[1];
                args[0] = variable;
            }
            return behaviour.GetType().InvokeMember(name, BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, behaviour, args, null, null, null);
        }

        static public object InvokeStatic(Type klass, string name, object variable)
        {
            object[] args = null;
            if (variable != null)
            {
                args = new System.Object[1];
                args[0] = variable;
            }
            return klass.InvokeMember(name, BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null, null, args, null, null, null);
        }
    }
}
