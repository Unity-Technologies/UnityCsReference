// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditor.VisualStudioIntegration;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor.Build.Player
{
    internal class BuildPlayerDataGeneratorNativeInterface
    {
        private static BuildPlayerDataGenerator _buildPlayerDataGenerator = new BuildPlayerDataGenerator(new BuildPlayerDataGeneratorProcess(), new DirectoryIOProvider());

        [RequiredByNativeCode]
        public static bool GenerateForAssemblies(string[] assemblies, string[] searchPaths, BuildTarget buildTarget, bool isEditor)
        {
            return _buildPlayerDataGenerator.GenerateForAssemblies(assemblies, searchPaths, buildTarget, isEditor);
        }
    }

    internal class BuildPlayerDataGenerator
    {
        public const string OutputPath = "Library/BuildPlayerData";

        public string EditorOutputPath
        {
            get; private set;
        }

        public string PlayerOutputPath
        {
            get; private set;
        }

        private readonly IBuildPlayerDataGeneratorProcess m_BuildPlayerDataGeneratorProcess;
        private readonly IDirectoryIO m_DirectoryIo;

        public BuildPlayerDataGenerator(IBuildPlayerDataGeneratorProcess buildPlayerDataGeneratorProcess,
                                        IDirectoryIO directoryIO, string outputPath = OutputPath)
        {
            EditorOutputPath = Path.Combine(outputPath, "Editor");
            PlayerOutputPath = Path.Combine(outputPath, "Player");
            m_BuildPlayerDataGeneratorProcess = buildPlayerDataGeneratorProcess;
            m_DirectoryIo = directoryIO;
        }

        public bool GenerateForAssemblies(string[] assemblies, string[] searchPaths, BuildTarget buildTarget, bool isEditor)
        {
            CreateCleanFolder(isEditor);
            var staticSearchPaths = GetStaticSearchPaths(buildTarget);

            string runtimeInitOnLoadFileName = null;
            if (!isEditor)
            {
                runtimeInitOnLoadFileName = "RuntimeInitializeOnLoads.json";
            }

            string fullOutputPath = Path.GetFullPath(isEditor ? EditorOutputPath : PlayerOutputPath);
            var assembliesWithFullPath = assemblies.Select(x => Path.GetFullPath(x));

            var buildPlayerDataGeneratorOptions = new BuildPlayerDataGeneratorOptions
            {
                Assemblies = assembliesWithFullPath.ToArray(),
                OutputPath = fullOutputPath,
                SearchPaths = searchPaths.Concat(staticSearchPaths).ToArray(),
                GeneratedTypeDbName = "TypeDb-All.json",
                GeneratedRuntimeInitializeOnLoadName = runtimeInitOnLoadFileName,
            };
            return m_BuildPlayerDataGeneratorProcess.Execute(buildPlayerDataGeneratorOptions);
        }

        private void CreateCleanFolder(bool isEditor)
        {
            string path = isEditor ? EditorOutputPath : PlayerOutputPath;

            if (m_DirectoryIo.Exists(path))
            {
                m_DirectoryIo.Delete(path, true);
            }

            m_DirectoryIo.CreateDirectory(path);
        }

        private static List<string> GetStaticSearchPaths(BuildTarget buildTarget)
        {
            var unityAssembliesInternal =
                EditorCompilationInterface.Instance.PrecompiledAssemblyProvider.GetUnityAssemblies(true, buildTarget);
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var systemReferenceDirectories =
                MonoLibraryHelpers.GetSystemReferenceDirectories(
                    PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup));

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
            return Directory.GetFiles(path, "TypeDb-*", SearchOption.AllDirectories);
        }
    }
}
