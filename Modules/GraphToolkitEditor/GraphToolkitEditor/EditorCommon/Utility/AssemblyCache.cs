// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using Unity.Collections;

namespace Unity.GraphToolkit.Editor
{
    static class AssemblyCache
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

        static List<Assembly> s_Assemblies;

        public static IReadOnlyList<Assembly> CachedAssemblies
        {
            get
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                return s_Assemblies ??= AppDomain.CurrentDomain.GetAssemblies()
#pragma warning restore UA2001
                    .Where(a => !a.IsDynamic
                        && !k_BlackListedAssemblies.HasAny(b => a.GetName().Name.ToLower().Contains(b)))
                    .ToList();
            }
        }

        // Create a dictionary of every method in a class tagged with TAttribute, grouped by the type of their first parameter type
        // [MyAttr] class Foo { public void A(float); public void B(int); public void C(float); public void D() }
        // GetExtensionMethods<MyAttrAttribute>() =>  { float => {A, C}, int => B }
        public static Dictionary<Type, List<MethodInfo>> GetExtensionMethods<TAttribute>(IReadOnlyList<Assembly> assemblies) where TAttribute : Attribute
        {
            static Type GetMethodFirstParameterType(MethodInfo m) => m.GetParameters()[0].ParameterType.IsArray ? m.GetParameters()[0].ParameterType.GetElementType() : m.GetParameters()[0].ParameterType;

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return TypeCache.GetTypesWithAttribute<TAttribute>()
#pragma warning restore UA2001
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                .Where(t => assemblies.Contains(t.Assembly) && t.IsClass)
#pragma warning restore UA2001
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .Where(m => m.GetParameters().Length > 0)
                .GroupBy(GetMethodFirstParameterType)
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                .ToDictionary(g => g.Key, g => g.ToList());
#pragma warning restore UA2001
        }
    }
}
