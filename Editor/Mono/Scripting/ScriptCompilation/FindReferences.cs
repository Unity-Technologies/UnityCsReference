// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace UnityEditor.Scripting.ScriptCompilation
{
    [Flags]
    internal enum FindReferencesQueryOptions
    {
        Direct = 1,
        Indirect = 2,
        Transitive = Indirect | Direct,
    }

    internal class FindReferences
    {
        readonly Dictionary<string, EditorBuildRules.TargetAssembly> m_AllTargetAssemblies;
        readonly Dictionary<string, bool> m_CompatibleTargetAssemblies = new Dictionary<string, bool>();
        readonly ScriptAssemblySettings m_AssemblySettings;

        readonly Dictionary<string, HashSet<string>> m_AssemblyNameReferences =
            new Dictionary<string, HashSet<string>>();

        public FindReferences(Dictionary<string, EditorBuildRules.TargetAssembly> targetAssemblies,
                              ScriptAssemblySettings assemblySettings)
        {
            m_AllTargetAssemblies = targetAssemblies;
            m_AssemblySettings = assemblySettings;
        }

        public HashSet<string> Execute(string assembly, string[] searchReferences,
            FindReferencesQueryOptions referencesOptions)
        {
            if (searchReferences.Length <= 0)
            {
                return new HashSet<string>();
            }

            var searchQuery = new HashSet<string>(searchReferences);
            HashSet<string> result = new HashSet<string>();
            if ((referencesOptions & FindReferencesQueryOptions.Direct) == FindReferencesQueryOptions.Direct)
            {
                HashSet<string> directResult = AllDirectReferences(m_AllTargetAssemblies[assembly]);
                result.UnionWith(directResult);
                result.IntersectWith(searchReferences);
            }

            if ((referencesOptions & FindReferencesQueryOptions.Indirect) == FindReferencesQueryOptions.Indirect)
            {
                foreach (var reference in m_AllTargetAssemblies[assembly].References)
                {
                    if (searchQuery.Count <= 0)
                    {
                        break;
                    }
                    result.UnionWith(FindReferencesRecursive(reference, searchQuery));
                }
            }

            return result;
        }

        private bool IsCompatibleCached(EditorBuildRules.TargetAssembly targetAssembly)
        {
            bool isCompatible;
            if (m_CompatibleTargetAssemblies.TryGetValue(targetAssembly.Filename, out isCompatible))
            {
                return isCompatible;
            }

            isCompatible = targetAssembly.IsCompatibleFunc(m_AssemblySettings, targetAssembly.Defines ?? new string[0]);
            m_CompatibleTargetAssemblies.Add(targetAssembly.Filename, isCompatible);
            return isCompatible;
        }

        private HashSet<string> AllDirectReferences(EditorBuildRules.TargetAssembly targetAssembly)
        {
            HashSet<string> references;
            if (m_AssemblyNameReferences.TryGetValue(targetAssembly.Filename, out references))
            {
                return references;
            }

            references = new HashSet<string>();
            foreach (var targetAssemblyReference in targetAssembly.References)
            {
                if (IsCompatibleCached(targetAssemblyReference))
                {
                    references.Add(targetAssemblyReference.Filename);
                }
            }

            foreach (var assemblyPrecompiledReference in targetAssembly.PrecompiledReferences)
            {
                var fileName = Path.GetFileName(assemblyPrecompiledReference.Path);
                references.Add(fileName);
            }

            m_AssemblyNameReferences.Add(targetAssembly.Filename, references);
            return references;
        }

        private List<string> FindReferencesRecursive(EditorBuildRules.TargetAssembly targetAssembly,
            HashSet<string> searchFor)
        {
            var result = new List<string>(searchFor.Count);
            var allDirectReferences = AllDirectReferences(targetAssembly);
            allDirectReferences.IntersectWith(searchFor);
            result.AddRange(allDirectReferences);

            searchFor.ExceptWith(result);
            if (!searchFor.Any())
            {
                return result;
            }

            foreach (var assemblyReference in targetAssembly.References)
            {
                if (!searchFor.Any())
                {
                    continue;
                }

                var referenceResult = FindReferencesRecursive(assemblyReference, searchFor);
                result.AddRange(referenceResult);
            }

            return result;
        }
    }
}
