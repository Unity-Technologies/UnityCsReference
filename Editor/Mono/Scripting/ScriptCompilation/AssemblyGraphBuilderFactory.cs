// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal static class AssemblyGraphBuilderFactory
    {
        class AssemblyGraphBuilderKey
        {
            public bool Equals(AssemblyGraphBuilderKey other)
            {
                return string.Equals(projectPath, other.projectPath, StringComparison.Ordinal)
                    && assemblies.SequenceEqual(other.assemblies)
                    && customScriptAssemblyReferences.SequenceEqual(other.customScriptAssemblyReferences);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((AssemblyGraphBuilderKey) obj);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(projectPath, assemblies.Count, customScriptAssemblyReferences.Count);
            }

            public string projectPath;
            public IReadOnlyCollection<CustomScriptAssembly> assemblies;
            public IReadOnlyCollection<CustomScriptAssemblyReference> customScriptAssemblyReferences;
        }

        private static Dictionary<AssemblyGraphBuilderKey, AssemblyGraphBuilder> m_AlreadyInitializedAssemblyGraphBuilder =
            new Dictionary<AssemblyGraphBuilderKey, AssemblyGraphBuilder>();

        public static IAssemblyGraphBuilder GetOrCreate(string projectPath,
            IReadOnlyCollection<CustomScriptAssembly> assemblies,
            IReadOnlyCollection<CustomScriptAssemblyReference> customScriptAssemblyReferences)
        {
            var assemblyGraphBuilderKey = new AssemblyGraphBuilderKey
            {
                projectPath = projectPath,
                assemblies = assemblies,
                customScriptAssemblyReferences = customScriptAssemblyReferences,
            };

            if (!m_AlreadyInitializedAssemblyGraphBuilder.TryGetValue(assemblyGraphBuilderKey,
                    out var assemblyGraphBuilder))
            {
                assemblyGraphBuilder = new AssemblyGraphBuilder(projectPath);
                assemblyGraphBuilder.Initialize(assemblies, customScriptAssemblyReferences);
                m_AlreadyInitializedAssemblyGraphBuilder[assemblyGraphBuilderKey] = assemblyGraphBuilder;
            }

            return assemblyGraphBuilder;
        }
    }
}
