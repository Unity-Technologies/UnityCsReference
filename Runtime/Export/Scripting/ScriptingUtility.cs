// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting;

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine
{
    internal class ScriptingUtility
    {
        struct TestClass
        {
            public int value;
        }

        // This method is the first thing called after loading assemblies.
        // If the scripting runtime somehow breaks because of "broken"
        // assemblies, then the call to this method will fail.
        // It's purpose is to communicate that there is an issue with
        // calling C# code in general and nothing that is specific
        // to any specific C# code/system.
        [RequiredByNativeCode]
        static bool IsManagedCodeWorking()
        {
            var testClass = new TestClass { value = 42 };
            return testClass.value == 42;
        }

        class ObjectWrapper
        {
            public byte Data;
        }

        [RequiredByNativeCode]
        static unsafe void GetValueAtOffsetObjectInstanceID(IntPtr handle, int offset, out EntityId instanceID)
        {
            int kObjectHeader = 2 * IntPtr.Size;
            var gchandle = GCHandle.FromIntPtr(handle);
            var o = gchandle.Target;

            var value = UnsafeUtility.As<byte, UnityEngine.Object>(ref UnsafeUtility.Add(ref UnsafeUtility.As<object, ObjectWrapper>(ref o).Data, offset - kObjectHeader));

            if (object.ReferenceEquals(value, null))
            {
                instanceID = EntityId.None;
                return;
            }

            instanceID = value.GetEntityId();
        }
    }
}
