// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Runtime/Scripting/TypeCache.h")]
    static partial class EditorAssemblies
    {
        const BindingFlags k_DefaultMethodBindingFlags =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static;

        internal static IEnumerable<MethodInfo> GetAllMethodsWithAttribute<T>(BindingFlags bindingFlags = k_DefaultMethodBindingFlags)
            where T : Attribute
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return Internal_GetAllMethodsWithAttribute(typeof(T), bindingFlags).Cast<MethodInfo>();
#pragma warning restore UA2001
        }

        [FreeFunction(Name = "GetAllMethodsWithAttribute")]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        extern static object[] Internal_GetAllMethodsWithAttribute(Type attrType, BindingFlags staticness);

        [FreeFunction("GetUnchangedAssemblyNames")]
        internal static extern string[] GetUnchangedAssemblyNames();

        internal static extern bool AllAssembliesAreUnchanged
        {
            [FreeFunction("AllAssembliesAreUnchanged")]
            get;
        }
    }
}
