// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Compilation;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal class UXMLFactoryPreserver
    {
        static HashSet<string> s_PrecompiledUnityAssemblies;
        static readonly HashSet<string> s_UsedTypesInsideAsset = new HashSet<string>();
        static readonly List<Type> s_FactoryTypesUsedInAsset = new List<Type>();

        // Called from native code when preparing assets for a build
        [RequiredByNativeCode]
        public static List<Type> ExtractTypesFromVisualTreeAsset(VisualTreeAsset asset)
        {
            if (s_PrecompiledUnityAssemblies == null)
            {
                // Ignore all precompiled Unity assemblies, but include those from users
                CompilationPipeline.PrecompiledAssemblySources sources = CompilationPipeline.PrecompiledAssemblySources.All;
                sources &= ~CompilationPipeline.PrecompiledAssemblySources.UserAssembly;
                s_PrecompiledUnityAssemblies = new HashSet<string>(CompilationPipeline.GetPrecompiledAssemblyPaths(sources));
            }

            s_FactoryTypesUsedInAsset.Clear();
            s_UsedTypesInsideAsset.Clear();

            asset.ExtractUsedUxmlQualifiedNames(s_UsedTypesInsideAsset);

            #pragma warning disable CS0618 // Type or member is obsolete
            foreach (var qualifiedName in s_UsedTypesInsideAsset)
            {
                if (VisualElementFactoryRegistry.TryGetValue(qualifiedName, out List<IUxmlFactory> factoryList))
                {
                    foreach (var factory in factoryList)
                    {
                        var type = factory.GetType();

                        if (s_PrecompiledUnityAssemblies.Contains(type.Assembly.Location))
                            continue;

                        s_FactoryTypesUsedInAsset.Add(type);
                    }
                }
            }
            #pragma warning restore CS0618 // Type or member is obsolete

            return s_FactoryTypesUsedInAsset;
        }
    }
}
