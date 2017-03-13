// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
using UnityEditor.Modules;
using UnityEngine;
using UnityEditor.Utils;

namespace UnityEditor.Scripting.Serialization
{
    internal static class Weaver
    {
        public static bool ShouldWeave(string name)
        {
            if (name.Contains("Boo."))
                return false;
            if (name.Contains("Mono."))
                return false;
            if (name.Contains("System"))
                return false;
            if (!name.EndsWith(".dll"))
                return false;

            return true;
        }

        private static ManagedProgram SerializationWeaverProgramWith(string arguments, string playerPackage)
        {
            return ManagedProgramFor(playerPackage + "/SerializationWeaver/SerializationWeaver.exe", arguments);
        }

        private static ManagedProgram ManagedProgramFor(string exe, string arguments)
        {
            return new ManagedProgram(MonoInstallationFinder.GetMonoInstallation("MonoBleedingEdge"), null, exe, arguments, false, null);
        }

        private static ICompilationExtension GetCompilationExtension()
        {
            var target = ModuleManager.GetTargetStringFromBuildTarget(EditorUserBuildSettings.activeBuildTarget);
            return ModuleManager.GetCompilationExtension(target);
        }

        private static void QueryAssemblyPathsAndResolver(ICompilationExtension compilationExtension, string file, bool editor, out string[] assemblyPaths, out IAssemblyResolver assemblyResolver)
        {
            assemblyResolver = compilationExtension.GetAssemblyResolver(editor, file, null);
            assemblyPaths = compilationExtension.GetCompilerExtraAssemblyPaths(editor, file).ToArray();
        }

        public static void WeaveAssembliesInFolder(string folder, string playerPackage)
        {
            ICompilationExtension compilationExtension = GetCompilationExtension();

            var unityEngine = Path.Combine(folder, "UnityEngine.dll");
            foreach (var file in Directory.GetFiles(folder).Where(f => ShouldWeave(Path.GetFileName(f))))
            {
                IAssemblyResolver assemblyResolver;
                string[] assemblyPaths;
                QueryAssemblyPathsAndResolver(compilationExtension, file, false, out assemblyPaths, out assemblyResolver);
                WeaveInto(file, file, unityEngine, playerPackage, assemblyPaths, assemblyResolver);
            }
        }

        // this is called when building in the editor
        public static bool WeaveUnetFromEditor(string assemblyPath, string destPath, string unityEngine, string unityUNet, bool buildingForEditor)
        {
            IEnumerable<MonoIsland> islands = UnityEditorInternal.InternalEditorUtility.GetMonoIslands().Where(i => 0 < i._files.Length);
            return WeaveUnetFromEditor(islands, assemblyPath, destPath, unityEngine, unityUNet, buildingForEditor);
        }

        public static bool WeaveUnetFromEditor(IEnumerable<MonoIsland> islands, string assemblyPath, string destPath, string unityEngine, string unityUNet, bool buildingForEditor)
        {
            Console.WriteLine("WeaveUnetFromEditor " + assemblyPath);

            ICompilationExtension compilationExtension = GetCompilationExtension();
            IAssemblyResolver assemblyResolver;
            string[] assemblyPaths;
            QueryAssemblyPathsAndResolver(compilationExtension, assemblyPath, buildingForEditor, out assemblyPaths, out assemblyResolver);
            return WeaveInto(islands, unityUNet, destPath, unityEngine, assemblyPath, assemblyPaths, assemblyResolver);
        }

        public static bool WeaveInto(string unityUNet, string destPath, string unityEngine, string assemblyPath, string[] extraAssemblyPaths, IAssemblyResolver assemblyResolver)
        {
            // Get full list of references (based on SolutionSynchronizer.ProjectText())
            IEnumerable<MonoIsland> islands = UnityEditorInternal.InternalEditorUtility.GetMonoIslands().Where(i => 0 < i._files.Length);
            return WeaveInto(islands, unityUNet, destPath, unityEngine, assemblyPath, extraAssemblyPaths, assemblyResolver);
        }

        public static bool WeaveInto(IEnumerable<MonoIsland> islands, string unityUNet, string destPath, string unityEngine, string assemblyPath, string[] extraAssemblyPaths, IAssemblyResolver assemblyResolver)
        {
            string projectDirectory = Directory.GetParent(Application.dataPath).FullName;

            string[] dependencies = null;
            foreach (MonoIsland island in islands)
            {
                if (destPath.Equals(island._output))  // See if this project matches the one we've been requested to build
                {
                    dependencies = GetReferences(island, projectDirectory);  // If so, retrieve the list of dependencies
                    break;
                }
            }
            if (dependencies == null) // No matching project found
            {
                UnityEngine.Debug.LogError("Weaver failure: unable to locate assemblies (no matching project) for: " + destPath);
                return false;
            }

            var dependencyPaths = new List<string>();
            foreach (var dependency in dependencies)
                dependencyPaths.Add(Path.GetDirectoryName(dependency));
            if (extraAssemblyPaths != null)
                dependencyPaths.AddRange(extraAssemblyPaths);
            try
            {
                if (!Unity.UNetWeaver.Program.Process(unityEngine, unityUNet, Path.GetDirectoryName(destPath), new[] { assemblyPath }, dependencyPaths.ToArray(), assemblyResolver, UnityEngine.Debug.LogWarning, UnityEngine.Debug.LogError))
                {
                    UnityEngine.Debug.LogError("Failure generating network code.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("Exception generating network code: " + ex.ToString() + " " + ex.StackTrace);
            }
            return true;
        }

        // Get the list of references that belong to the specified Mono island
        public static string[] GetReferences(MonoIsland island, string projectDirectory)
        {
            var returnReferences = new List<string>();

            // Prune list of references to only relevant ones
            var references = new List<string>();
            foreach (string reference in references.Union(island._references))
            {
                string fileName = Path.GetFileName(reference);

                if (!string.IsNullOrEmpty(fileName) && (fileName.Contains("UnityEditor.dll") || fileName.Contains("UnityEngine.dll")))
                {
                    continue;
                }

                string fullReference = Path.IsPathRooted(reference) ? reference : Path.Combine(projectDirectory, reference);
                if (!AssemblyHelper.IsManagedAssembly(fullReference))
                    continue;
                if (AssemblyHelper.IsInternalAssembly(fullReference))
                    continue;
                returnReferences.Add(fullReference);
            }

            return returnReferences.ToArray();
        }

    }
}
