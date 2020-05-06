// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor.Compilation;
using UnityEditor.PackageManager;

namespace UnityEditor.VisualStudioIntegration
{
    interface IAssemblyNameProvider
    {
        string[] ProjectSupportedExtensions { get; }
        string ProjectGenerationRootNamespace { get; }
        ProjectGenerationFlag ProjectGenerationFlag { get; }

        string GetAssemblyNameFromScriptPath(string path);
        string GetCompileOutputPath(string assemblyName);
        bool IsInternalizedPackagePath(string path);
        IEnumerable<Assembly> GetAssemblies(Func<string, bool> shouldFileBePartOfSolution);
        IEnumerable<string> GetAllAssetPaths();
        UnityEditor.PackageManager.PackageInfo FindForAssetPath(string assetPath);
        ResponseFileData ParseResponseFile(string responseFilePath, string projectDirectory, string[] systemReferenceDirectories);
        IEnumerable<string> GetRoslynAnalyzerPaths();
        void ToggleProjectGeneration(ProjectGenerationFlag preference);
    }

    [Flags]
    enum ProjectGenerationFlag
    {
        None = 0,
        Embedded = 1,
        Local = 2,
        Registry = 4,
        Git = 8,
        BuiltIn = 16,
        Unknown = 32,
        PlayerAssemblies = 64,
        LocalTarBall = 128,
    }

    class AssemblyNameProvider : IAssemblyNameProvider
    {
        ProjectGenerationFlag m_ProjectGenerationFlag = (ProjectGenerationFlag)EditorPrefs.GetInt("unity_project_generation_flag", 3);

        public string[] ProjectSupportedExtensions => EditorSettings.projectGenerationUserExtensions;

        public string ProjectGenerationRootNamespace => EditorSettings.projectGenerationRootNamespace;

        public ProjectGenerationFlag ProjectGenerationFlag
        {
            get { return m_ProjectGenerationFlag; }
            private set
            {
                EditorPrefs.SetInt("unity_project_generation_flag", (int)value);
                m_ProjectGenerationFlag = value;
            }
        }

        public string GetAssemblyNameFromScriptPath(string path)
        {
            return CompilationPipeline.GetAssemblyNameFromScriptPath(path);
        }

        public IEnumerable<Assembly> GetAssemblies(Func<string, bool> shouldFileBePartOfSolution)
        {
            foreach (var assembly in CompilationPipeline.GetAssemblies())
            {
                if (assembly.sourceFiles.Any(shouldFileBePartOfSolution))
                {
                    yield return new Assembly(assembly.name, assembly.outputPath, assembly.sourceFiles, new[] { "DEBUG", "TRACE" }.Concat(assembly.defines).Concat(EditorUserBuildSettings.activeScriptCompilationDefines).ToArray(), assembly.assemblyReferences, assembly.compiledAssemblyReferences, assembly.flags)
                    {
                        compilerOptions =
                        {
                            ResponseFiles = assembly.compilerOptions.ResponseFiles,
                            AllowUnsafeCode = assembly.compilerOptions.AllowUnsafeCode,
                            ApiCompatibilityLevel = assembly.compilerOptions.ApiCompatibilityLevel
                        }
                    };
                }
            }

            if (HasFlag(ProjectGenerationFlag.PlayerAssemblies))
            {
                foreach (var assembly in CompilationPipeline.GetAssemblies(AssembliesType.Player).Where(assembly => assembly.sourceFiles.Any(shouldFileBePartOfSolution)))
                {
                    yield return new Assembly(assembly.name + ".Player", assembly.outputPath, assembly.sourceFiles, new[] { "DEBUG", "TRACE" }.Concat(assembly.defines).ToArray(), assembly.assemblyReferences, assembly.compiledAssemblyReferences, assembly.flags)
                    {
                        compilerOptions =
                        {
                            ResponseFiles = assembly.compilerOptions.ResponseFiles,
                            AllowUnsafeCode = assembly.compilerOptions.AllowUnsafeCode,
                            ApiCompatibilityLevel = assembly.compilerOptions.ApiCompatibilityLevel
                        }
                    };
                }
            }
        }

        public string GetCompileOutputPath(string assemblyName)
        {
            return assemblyName.EndsWith(".Player", StringComparison.Ordinal) ? @"Temp\Bin\Debug\Player\" : @"Temp\Bin\Debug\";
        }

        public IEnumerable<string> GetAllAssetPaths()
        {
            return AssetDatabase.GetAllAssetPaths();
        }

        public UnityEditor.PackageManager.PackageInfo FindForAssetPath(string assetPath)
        {
            return UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath);
        }

        public bool IsInternalizedPackagePath(string path)
        {
            if (string.IsNullOrEmpty(path.Trim()))
            {
                return false;
            }

            var packageInfo = FindForAssetPath(path);
            if (packageInfo == null)
            {
                return false;
            }

            var packageSource = packageInfo.source;
            switch (packageSource)
            {
                case PackageSource.Embedded:
                    return !HasFlag(ProjectGenerationFlag.Embedded);
                case PackageSource.Registry:
                    return !HasFlag(ProjectGenerationFlag.Registry);
                case PackageSource.BuiltIn:
                    return !HasFlag(ProjectGenerationFlag.BuiltIn);
                case PackageSource.Unknown:
                    return !HasFlag(ProjectGenerationFlag.Unknown);
                case PackageSource.Local:
                    return !HasFlag(ProjectGenerationFlag.Local);
                case PackageSource.Git:
                    return !HasFlag(ProjectGenerationFlag.Git);
                case PackageSource.LocalTarball:
                    return !HasFlag(ProjectGenerationFlag.LocalTarBall);
            }

            return false;
        }

        public ResponseFileData ParseResponseFile(string responseFilePath, string projectDirectory, string[] systemReferenceDirectories)
        {
            return CompilationPipeline.ParseResponseFile(
                responseFilePath,
                projectDirectory,
                systemReferenceDirectories
            );
        }

        public IEnumerable<string> GetRoslynAnalyzerPaths()
        {
            return PluginImporter.GetAllImporters()
                .Where(i => !i.isNativePlugin && AssetDatabase.GetLabels(i).SingleOrDefault(l => l == "RoslynAnalyzer") != null)
                .Select(i => i.assetPath);
        }

        public void ToggleProjectGeneration(ProjectGenerationFlag preference)
        {
            if (HasFlag(preference))
            {
                ProjectGenerationFlag ^= preference;
            }
            else
            {
                ProjectGenerationFlag |= preference;
            }
        }

        bool HasFlag(ProjectGenerationFlag flag)
        {
            return (this.ProjectGenerationFlag & flag) == flag;
        }

        public void ResetProjectGenerationFlag()
        {
            ProjectGenerationFlag = ProjectGenerationFlag.None;
        }
    }
}
