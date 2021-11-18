// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using static UnityEditor.Scripting.ScriptCompilation.EditorBuildRules;

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class AutoReferencedPackageAssemblies
    {
        static HashSet<string> runtimeAssemblyNames = new HashSet<string>(new[]
        {
            "UnityEngine.UI.dll",
        },
            StringComparer.Ordinal);

        static HashSet<string> editorAssemblyNames = new HashSet<string>(new[]
        {
            "UnityEditor.UI.dll",
        },
            StringComparer.Ordinal);

        // Do not add automatic package references to these assemblies,
        // as they also add themselves to all .asmdefs
        static HashSet<string> ignoreAssemblies = new HashSet<string>(new[]
        {
            "UnityEngine.TestRunner.dll",
            "UnityEditor.TestRunner.dll",
        },
            StringComparer.Ordinal);

        static AutoReferencedPackageAssemblies()
        {
            editorAssemblyNames.UnionWith(runtimeAssemblyNames);
            ignoreAssemblies.UnionWith(editorAssemblyNames);
        }

        public static void AddReferences(Dictionary<string, TargetAssembly> customTargetAssemblies, EditorScriptCompilationOptions options, Func<TargetAssembly, bool> shouldAdd)
        {
            if (customTargetAssemblies == null || customTargetAssemblies.Count == 0)
                return;

            bool buildingForEditor = (options & EditorScriptCompilationOptions.BuildingForEditor) == EditorScriptCompilationOptions.BuildingForEditor;

            // Add runtime assembly references for the players and
            // runtime and editor assembly references for the editor.
            var autoReferencedAssemblies = buildingForEditor ? editorAssemblyNames : runtimeAssemblyNames;

            var additionalReferences = new HashSet<TargetAssembly>();

            foreach (var assemblyName in autoReferencedAssemblies)
            {
                TargetAssembly targetAssembly;

                // If the automatic referenced package assemblies are in
                // the project, then they should be add to all .asmdefs.
                if (customTargetAssemblies.TryGetValue(assemblyName, out targetAssembly))
                {
                    additionalReferences.Add(targetAssembly);
                }
            }

            // If none of the automatic references package assemblies are in
            // the project, do not add anything.
            if (!additionalReferences.Any())
            {
                return;
            }

            foreach (var entry in customTargetAssemblies)
            {
                var assembly = entry.Value;

                if (!shouldAdd?.Invoke(assembly) ?? false)
                {
                    continue;
                }

                // Do not add additional references to any of the
                // automatically referenced or ignored assemblies
                if (ignoreAssemblies.Contains(assembly.Filename))
                    continue;

                // Add the automatic references.
                var newReferences = assembly.References.Concat(additionalReferences).Distinct().ToList();
                assembly.References = newReferences;
            }
        }
    }
}
