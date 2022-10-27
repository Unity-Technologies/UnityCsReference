// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace Unity.GraphToolsFoundation.Editor
{
    static class AssemblyCache_Internal
    {
        static readonly string[] k_BlackListedAssemblies =
        {
            "boo.lang",
            "castle.core",
            "excss.unity",
            "jetbrains",
            "lucene",
            "microsoft",
            "mono",
            "moq",
            "nunit",
            "system.web",
            "unityscript",
            "visualscriptingassembly-csharp"
        };

        static IEnumerable<Assembly> s_Assemblies;

        internal static IEnumerable<Assembly> CachedAssemblies_Internal
        {
            get
            {
                return s_Assemblies ??= AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic
                        && !k_BlackListedAssemblies.Any(b => a.GetName().Name.ToLower().Contains(b)))
                    .ToList();
            }
        }

        // Create a dictionary of every method in a class tagged with TAttribute, grouped by the type of their first parameter type
        // [MyAttr] class Foo { public void A(float); public void B(int); public void C(float); public void D() }
        // GetExtensionMethods<MyAttrAttribute>() =>  { float => {A, C}, int => B }
        internal static Dictionary<Type, List<MethodInfo>> GetExtensionMethods_Internal<TAttribute>(IEnumerable<Assembly> assemblies) where TAttribute : Attribute
        {
            static Type GetMethodFirstParameterType(MethodInfo m) => m.GetParameters()[0].ParameterType.IsArray ? m.GetParameters()[0].ParameterType.GetElementType() : m.GetParameters()[0].ParameterType;

            return TypeCache.GetTypesWithAttribute<TAttribute>()
                .Where(t => assemblies.Contains(t.Assembly) && t.IsClass)
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .Where(m => m.GetParameters().Length > 0)
                .GroupBy(GetMethodFirstParameterType)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
    }
}
