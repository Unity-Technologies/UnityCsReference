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
        [ThreadSafe]
        static extern Type[] Internal_GetTypesWithAttribute(Type attrType);

        [ThreadSafe]
        static extern MethodInfo[] Internal_GetMethodsWithAttribute(Type attrType);

        [ThreadSafe]
        static extern FieldInfo[] Internal_GetFieldsWithAttribute(Type attrType);

        [ThreadSafe]
        static extern Type[] Internal_GetTypesDerivedFromInterface(Type interfaceType);

        [ThreadSafe]
        static extern Type[] Internal_GetTypesDerivedFromType(Type parentType);

        [ThreadSafe]
        static extern Type[] Internal_GetTypesWithAttributeFromAssembly(Type attrType, string assemblyName);

        [ThreadSafe]
        static extern MethodInfo[] Internal_GetMethodsWithAttributeFromAssembly(Type attrType, string assemblyName);

        [ThreadSafe]
        static extern FieldInfo[] Internal_GetFieldsWithAttributeFromAssembly(Type attrType, string assemblyName);

        [ThreadSafe]
        static extern Type[] Internal_GetTypesDerivedFromInterfaceFromAssembly(Type interfaceType, string assemblyName);

        [ThreadSafe]
        static extern Type[] Internal_GetTypesDerivedFromTypeFromAssembly(Type parentType, string assemblyName);

        internal static extern ulong GetCurrentAge();
    }
}
