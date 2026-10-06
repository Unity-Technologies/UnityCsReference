// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using Unity.Collections;
using UnityEngine.Bindings;

namespace Unity.Scripting.Reflection
{
    // Bulk queries fill the buffer and return the full count; a capacity of 0 asks for the count alone
    [NativeHeader("ScriptingReflection/AssemblyHandle.bindings.h")]
    [StaticAccessor("ScriptingReflectionBindings::AssemblyHandle", StaticAccessorType.DoubleColon)]
    internal partial struct AssemblyHandle
    {
        [NativeMethod(IsThreadSafe = true)]
        static extern bool ReferencesAssembly(IntPtr source, IntPtr reference);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern int GetAllAssemblies(IntPtr outAssemblies, int capacity);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool AssemblyHasAttribute(IntPtr assembly, IntPtr attributeKlass);

        [NativeMethod(IsThreadSafe = true)]
        static extern int GetAssemblyCustomAttributes(IntPtr assembly, IntPtr attributeKlass, Allocator allocator, IntPtr outAttributes, int capacity);

        [NativeMethod(IsThreadSafe = true)]
        static extern IntPtr AssemblyHandleFromAssembly(Assembly assembly);

        // Unresolvable types, and skipTypeAndNested with its nested types, are written as zero to keep row order
        [NativeMethod(IsThreadSafe = true)]
        static extern int GetAssemblyTypes(IntPtr assembly, IntPtr skipTypeAndNested, IntPtr outClasses, int capacity);
    }
}
