// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace Unity.Scripting.Reflection
{
    [NativeHeader("ScriptingReflection/AttributeHandle.bindings.h")]
    [StaticAccessor("ScriptingReflectionBindings::AttributeHandle", StaticAccessorType.DoubleColon)]
    internal partial struct AttributeHandle
    {
        // Not for a Type argument, which needs GetAttributeArgClass; calling the wrong one aborts
        [NativeMethod(IsThreadSafe = true)]
        internal static extern IntPtr GetAttributeArgValue(IntPtr attribute, int index);

        [NativeMethod(IsThreadSafe = true)]
        static extern IntPtr GetAttributeArgClass(IntPtr attribute, int index);

        [NativeMethod(IsThreadSafe = true)]
        static extern IntPtr GetAttributeClass(IntPtr attribute);
    }
}
