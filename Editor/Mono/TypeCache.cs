// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace UnityEditor
{
    public static partial class TypeCache
    {
        public static TypeCollection GetTypesWithAttribute<T>()
            where T : Attribute
        {
            return GetTypesWithAttribute(typeof(T));
        }

        public static MethodCollection GetMethodsWithAttribute<T>()
            where T : Attribute
        {
            return GetMethodsWithAttribute(typeof(T));
        }

        public static FieldInfoCollection GetFieldsWithAttribute<T>()
            where T : Attribute
        {
            return GetFieldsWithAttribute(typeof(T));
        }

        public static TypeCollection GetTypesDerivedFrom<T>()
        {
            var parentType = typeof(T);
            return GetTypesDerivedFrom(parentType);
        }

        public static TypeCollection GetTypesDerivedFrom(Type parentType)
        {
            return parentType.IsInterface ? GetTypesDerivedFromInterface(parentType) : GetTypesDerivedFromType(parentType);
        }
    }
}
