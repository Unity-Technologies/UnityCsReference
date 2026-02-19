// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Runtime/Scripting/TypeCache.h")]
    public static partial class TypeCache
    {
        [NativeMethod(IsThreadSafe = true)]
        static extern Type[] Internal_GetTypesWithAttribute(Type attrType);

        [NativeMethod(IsThreadSafe = true)]
        static extern MethodInfo[] Internal_GetMethodsWithAttribute(Type attrType);

        [NativeMethod(IsThreadSafe = true)]
        static extern FieldInfo[] Internal_GetFieldsWithAttribute(Type attrType);

        [NativeMethod(IsThreadSafe = true)]
        static extern Type[] Internal_GetTypesDerivedFromInterface(Type interfaceType);

        [NativeMethod(IsThreadSafe = true)]
        static extern Type[] Internal_GetTypesDerivedFromType(Type parentType);

        [NativeMethod(IsThreadSafe = true)]
        static extern Type[] Internal_GetTypesWithAttributeFromAssembly(Type attrType, string assemblyName);

        [NativeMethod(IsThreadSafe = true)]
        static extern MethodInfo[] Internal_GetMethodsWithAttributeFromAssembly(Type attrType, string assemblyName);

        [NativeMethod(IsThreadSafe = true)]
        static extern FieldInfo[] Internal_GetFieldsWithAttributeFromAssembly(Type attrType, string assemblyName);

        [NativeMethod(IsThreadSafe = true)]
        static extern Type[] Internal_GetTypesDerivedFromInterfaceFromAssembly(Type interfaceType, string assemblyName);

        [NativeMethod(IsThreadSafe = true)]
        static extern Type[] Internal_GetTypesDerivedFromTypeFromAssembly(Type parentType, string assemblyName);

        internal static extern ulong GetCurrentAge();
    }
}
