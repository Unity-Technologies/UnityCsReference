// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Runtime/Mono/MonoAttributeHelpers.h")]
    static partial class EditorAssemblies
    {
        const BindingFlags k_DefaultMethodBindingFlags =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static;

        internal static MethodInfo[] GetAllMethodsWithAttribute<T>(BindingFlags bindingFlags = k_DefaultMethodBindingFlags)
            where T : Attribute
        {
            return GetAllMethodsWithAttribute(typeof(T), bindingFlags);
        }

        [FreeFunction]
        extern static MethodInfo[] GetAllMethodsWithAttribute(Type attrType, BindingFlags staticness);

        internal static Type[] GetAllTypesWithAttribute<T>() where T : Attribute
        {
            return GetAllTypesWithAttribute(typeof(T));
        }

        [FreeFunction]
        extern static Type[] GetAllTypesWithAttribute(Type attrType);

        internal static Type[] GetAllTypesWithInterface<T>() where T : class
        {
            return GetAllTypesWithInterface(typeof(T));
        }

        internal static Type[] GetAllTypesWithInterface(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException(string.Format("Specified type {0} is not an interface.", interfaceType), "interfaceType");
            return Internal_GetAllTypesWithInterface(interfaceType);
        }

        [FreeFunction(Name = "GetAllTypesWithInterface")]
        extern static Type[] Internal_GetAllTypesWithInterface(Type interfaceType);
    }
}
