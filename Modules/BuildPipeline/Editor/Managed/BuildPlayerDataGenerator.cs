// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditor.VisualStudioIntegration;

namespace UnityEditor.Build.Player
{
    internal class BuildPlayerDataGenerator
    {
        public const string OutputPath = "Library/BuildPlayerData";

        public string EditorOutputPath
        {
            get;
        }

        public string PlayerOutputPath
        {
            get;
        }

        public BuildPlayerDataGenerator(string outputPath = OutputPath)
        {
            EditorOutputPath = Path.Combine(outputPath, "Editor");
            PlayerOutputPath = Path.Combine(outputPath, "Player");
        }

        public static List<string> GetStaticSearchPaths(BuildTarget buildTarget)
        {
            var unityAssembliesInternal =
                EditorCompilationInterface.Instance.PrecompiledAssemblyProvider.GetUnityAssemblies(true, buildTarget);
            var namedBuildTarget = NamedBuildTarget.FromActiveSettings(buildTarget);
            var systemReferenceDirectories =
                MonoLibraryHelpers.GetSystemReferenceDirectories(
                    PlayerSettings.GetApiCompatibilityLevel(namedBuildTarget));

            var searchPaths = unityAssembliesInternal.Select(x => Path.GetDirectoryName(x.Path))
                .Distinct().ToList();
            searchPaths.AddRange(systemReferenceDirectories);
            return searchPaths;
        }

        public string[] GetTypeDbFilePaths(bool isEditor)
        {
            var specificFolderPath = isEditor ? EditorOutputPath : PlayerOutputPath;
            return GetTypeDbFilePathsFrom(specificFolderPath);
        }

        public static string[] GetTypeDbFilePathsFrom(string path)
        {
            // Can happen when building an empty project with no serializable types
            if (!Directory.Exists(path))
                return Array.Empty<string>();
            return Directory.GetFiles(path, "TypeDb-*", SearchOption.AllDirectories);
        }
    }
}
