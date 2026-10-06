// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using UnityEngine.Bindings;

namespace Unity.Scripting.Reflection
{
    [NativeHeader("ScriptingReflection/FieldHandle.bindings.h")]
    [StaticAccessor("ScriptingReflectionBindings::FieldHandle", StaticAccessorType.DoubleColon)]
    internal partial struct FieldHandle
    {
        [NativeMethod(IsThreadSafe = true)]
        static extern bool IsFieldStatic(IntPtr field, IntPtr parent);

        [NativeMethod(IsThreadSafe = true)]
        static extern int GetFieldOffset(IntPtr field, IntPtr parent);

        [NativeMethod(IsThreadSafe = true)]
        static extern string GetFieldName(IntPtr field, IntPtr parent);

        [NativeMethod(IsThreadSafe = true)]
        static extern int GetFieldCustomAttributes(IntPtr field, IntPtr fieldParent, IntPtr attributeKlass, Allocator allocator, IntPtr outAttributes, int capacity);

        [NativeMethod(IsThreadSafe = true)]
        static extern IntPtr GetFieldType(IntPtr field, IntPtr parent);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool IsFieldTypePointer(IntPtr field, IntPtr parent);
    }
}
