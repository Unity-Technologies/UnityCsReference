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
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.Scripting.Serialization
{
    internal static class Weaver
    {
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

        public static bool WeaveUnetFromEditor(ScriptAssembly assembly, string assemblyDirectory, string outputDirectory, string unityEngine, string unityUNet, bool buildingForEditor)
        {
            if ((assembly.Flags & AssemblyFlags.EditorOnly) == AssemblyFlags.EditorOnly)
                return true;

            var assemblyPath = Path.Combine(assemblyDirectory, assembly.Filename);

            ICompilationExtension compilationExtension = GetCompilationExtension();
            IAssemblyResolver assemblyResolver;
            string[] assemblyPaths;
            QueryAssemblyPathsAndResolver(compilationExtension, assemblyPath, buildingForEditor, out assemblyPaths, out assemblyResolver);
            return WeaveInto(assembly, assemblyPath, outputDirectory, unityEngine, unityUNet, assemblyPaths, assemblyResolver);
        }

        private static bool WeaveInto(ScriptAssembly assembly, string assemblyPath, string outputDirectory, string unityEngine, string unityUNet, string[] extraAssemblyPaths, IAssemblyResolver assemblyResolver)
        {
            var dependencies = assembly.GetAllReferences();
            var dependencyPaths = new string[dependencies.Count() + (extraAssemblyPaths != null ? extraAssemblyPaths.Length : 0)];

            int i = 0;

            foreach (var dependency in dependencies)
                dependencyPaths[i++] = Path.GetDirectoryName(dependency);

            if (extraAssemblyPaths != null)
                extraAssemblyPaths.CopyTo(dependencyPaths, i);

            try
            {
                if (!Unity.UNetWeaver.Program.Process(unityEngine, unityUNet, outputDirectory, new[] { assemblyPath }, dependencyPaths, assemblyResolver, UnityEngine.Debug.LogWarning, UnityEngine.Debug.LogError))
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

    }
}
