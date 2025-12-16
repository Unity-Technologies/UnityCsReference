// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.AssemblyUtils
{
    struct CompilerMessage
    {
        /// <summary>
        ///   <para>Message code.</para>
        /// </summary>
        public string Code;
        /// <summary>
        ///   <para>Message type.</para>
        /// </summary>
        public CompilerMessageType Type;
        /// <summary>
        ///   <para>Message body.</para>
        /// </summary>
        public string Message;
        /// <summary>
        ///   <para>File for the message.</para>
        /// </summary>
        public string File;
        /// <summary>
        ///   <para>File line for the message.</para>
        /// </summary>
        public int Line;
    }

    enum CompilationStatus
    {
        NotStarted,
        IsCompiling,
        Compiled,
        MissingDependency
    }

    class AssemblyCompilation : IDisposable
    {
        Dictionary<string, AssemblyCompilationTask> m_AssemblyCompilationTasks;
        string m_OutputFolder = string.Empty;

        public string[] AssemblyNames;
        public CodeOptimization CodeOptimization = CodeOptimization.Release;
        [Obsolete("Please use CodeAnalysisFlags instead", true)]
        public CompilationMode CompilationMode = CompilationMode.Player;
        public CodeAnalysisFlags CodeAnalysisFlags = CodeAnalysisFlagsExtensions.Default;
        internal CodeOwnerFlags CodeOwnerFlags = CodeOwnerFlags.User;
        public BuildTarget Platform = EditorUserBuildSettings.activeBuildTarget;
        public string[] RoslynAnalyzers;

        public Action<AssemblyCompilationResult> OnAssemblyCompilationFinished;

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(m_OutputFolder) && Directory.Exists(m_OutputFolder))
            {
                foreach (var task in m_AssemblyCompilationTasks.Select(pair => pair.Value).Where(u => u.IsCompletedSuccessfully))
                {
                    File.Delete(task.AssemblyPath);
                    File.Delete(Path.ChangeExtension(task.AssemblyPath, ".pdb"));
                }

                m_AssemblyCompilationTasks.Clear();

                // We can't delete the folder because of the CompilationLog.txt created by the AssemblyBuilder compilationTask
                //Directory.Delete(m_OutputFolder, true);
            }
            m_OutputFolder = string.Empty;
        }

        public void Compile(out List<AssemblyInfo> compiledEditorAssemblyPaths, out List<AssemblyInfo> compiledPlayerAssemblyPaths, IProgress progress = null)
        {
            GetAssemblies(out var editorAssemblies, out var playerAssemblies);

            if (AssemblyNames != null)
            {
                editorAssemblies = CollectAssemblyDependencies(editorAssemblies);
                playerAssemblies = CollectAssemblyDependencies(playerAssemblies);
            }

            IEnumerable<string> compiledPlayerPaths = CompilePlayerAssemblies(playerAssemblies.ToArray(), progress);

            var editorPaths = editorAssemblies.Select(a => AssemblyInfoProvider.GetAssemblyInfoFromAssemblyPath(a.outputPath, true)).Distinct().ToList();
            var playerPaths = compiledPlayerPaths.Select(p => AssemblyInfoProvider.GetAssemblyInfoFromAssemblyPath(p, false)).Distinct().ToList();

            // If only auditing Unity code, remove all User assemblies (can't do this the other way around because User code depends on Unity code)
            if ((CodeOwnerFlags & CodeOwnerFlags.All) == CodeOwnerFlags.Unity)
            {
                editorPaths = editorPaths.Where(p => p.IsUnityOwned).ToList();
                playerPaths = playerPaths.Where(p => p.IsUnityOwned).ToList();
            }

            // Remove any duplicates
            if (editorPaths.Count > 0 && playerPaths.Count > 0)
                editorPaths = editorPaths.Where(e => !playerPaths.Any(p => p.Name == e.Name)).ToList();

            // Add Unity assemblies
            if ((CodeOwnerFlags & CodeOwnerFlags.Unity) != 0)
                FindUnityModuleDLLs(editorPaths, playerPaths);

            compiledEditorAssemblyPaths = editorPaths;
            compiledPlayerAssemblyPaths = playerPaths;
        }

        private void FindUnityModuleDLLs(List<AssemblyInfo> editorPaths, List<AssemblyInfo> playerPaths)
        {
            string rootModuleDirectory = Path.GetDirectoryName(InternalEditorUtility.GetEditorAssemblyPath());
            string modulePath = Path.Combine(rootModuleDirectory, "UnityEngine");

            if (!Directory.Exists(rootModuleDirectory))
            {
                Debug.LogError($"Unity Module root directory not found at: {rootModuleDirectory}");
                return;
            }
            if (!Directory.Exists(modulePath))
            {
                Debug.LogError($"Unity Module directory not found at: {modulePath}");
                return;
            }

            if ((CodeAnalysisFlags & CodeAnalysisFlags.Editor) != 0)
                editorPaths.Add(AssemblyInfoProvider.GetAssemblyInfoFromUnityAssemblyPath(Path.Combine(rootModuleDirectory, "UnityEditor.dll"), true));
            if ((CodeAnalysisFlags & CodeAnalysisFlags.Player) != 0)
                playerPaths.Add(AssemblyInfoProvider.GetAssemblyInfoFromUnityAssemblyPath(Path.Combine(rootModuleDirectory, "UnityEngine.dll"), false));

            string[] dllFiles = Directory.GetFiles(modulePath, "*.dll");
            if (dllFiles.Length == 0)
            {
                Debug.LogError($"No Unity Module DLLs found in: {modulePath}");
                return;
            }

            foreach (string dllFile in dllFiles)
            {
                string assemblyName = Path.GetFileNameWithoutExtension(dllFile);
                bool editorAssembly = assemblyName.StartsWith("UnityEditor");

                if (editorAssembly)
                {
                    if ((CodeAnalysisFlags & CodeAnalysisFlags.Editor) != 0)
                        editorPaths.Add(AssemblyInfoProvider.GetAssemblyInfoFromUnityAssemblyPath(dllFile, true));
                }
                else
                {
                    if ((CodeAnalysisFlags & CodeAnalysisFlags.Player) != 0)
                        playerPaths.Add(AssemblyInfoProvider.GetAssemblyInfoFromUnityAssemblyPath(dllFile, false));
                }
            }
        }

        void GetAssemblies(out IReadOnlyCollection<Assembly> editorAssemblies, out IReadOnlyCollection<Assembly> playerAssemblies)
        {
            if ((CodeAnalysisFlags & CodeAnalysisFlags.Editor) != 0)
            {
                editorAssemblies = UnityEditor.Compilation.CompilationPipeline.GetAssemblies(AssembliesType.Editor);
                editorAssemblies = editorAssemblies.Where(a => (a.flags & AssemblyFlags.EditorAssembly) != 0).ToArray();

                if ((CodeAnalysisFlags & CodeAnalysisFlags.Tests) == 0)
                {
                    var result = new List<Assembly>(editorAssemblies.Count);
                    foreach (var assembly in editorAssemblies)
                    {
                        var info = AssemblyInfoProvider.GetAssemblyInfoFromAssemblyPath(assembly.outputPath, (assembly.flags & AssemblyFlags.EditorAssembly) != 0);
                        if (!info.IsTestAssembly)
                            result.Add(assembly);
                    }
                    editorAssemblies = result;
                }
            }
            else
            {
                editorAssemblies = Array.Empty<Assembly>();
            }

            if ((CodeAnalysisFlags & CodeAnalysisFlags.Player) != 0)
            {
                if ((CodeAnalysisFlags & CodeAnalysisFlags.Tests) != 0)
                    playerAssemblies = UnityEditor.Compilation.CompilationPipeline.GetAssemblies(AssembliesType.Player);
                else
                    playerAssemblies = UnityEditor.Compilation.CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies);
            }
            else
            {
                playerAssemblies = Array.Empty<Assembly>();
            }
        }

        IReadOnlyCollection<Assembly> CollectAssemblyDependencies(IReadOnlyCollection<Assembly> assemblies)
        {
            var assembliesAndDependencies = new List<Assembly>();
            foreach (var assembly in assemblies.Where(a => AssemblyNames.Contains(a.name)))
                CollectAssemblyDependenciesRecursive(assembly, assembliesAndDependencies);
            return assembliesAndDependencies;
        }

        static void CollectAssemblyDependenciesRecursive(Assembly assembly, List<Assembly> assembliesAndDependencies)
        {
            if (!assembliesAndDependencies.Contains(assembly))
                assembliesAndDependencies.Add(assembly);

            var missingDependencies = assembly.assemblyReferences.Where(d => !assembliesAndDependencies.Contains(d));
            foreach (var dependency in missingDependencies)
                CollectAssemblyDependenciesRecursive(dependency, assembliesAndDependencies);
        }

        IEnumerable<string> CompilePlayerAssemblies(Assembly[] assemblies, IProgress progress = null)
        {
            if (progress != null)
            {
                var numAssemblies = assemblies.Length;
                progress.Start("Assembly Compilation", "Compiling project scripts",
                    numAssemblies);
            }

            m_OutputFolder = FileUtil.GetUniqueTempPathInProject();

            if (!Directory.Exists(m_OutputFolder))
                Directory.CreateDirectory(m_OutputFolder);

            PrepareAssemblyBuilders(assemblies, progress);

            UpdateAssemblyBuilders(progress);

            if (progress?.IsCancelled ?? false)
                return Array.Empty<string>();

            if (progress != null)
                progress.Clear();

            return m_AssemblyCompilationTasks.Where(pair => pair.Value.IsCompletedSuccessfully).Select(task => task.Value.AssemblyPath);
        }

        void PrepareAssemblyBuilders(Assembly[] assemblies, IProgress progress = null)
        {
            m_AssemblyCompilationTasks = new Dictionary<string, AssemblyCompilationTask>();
            // first pass: create all compilation tasks
            foreach (var assembly in assemblies)
            {
                var filename = Path.GetFileName(assembly.outputPath);
                var assemblyName = Path.GetFileNameWithoutExtension(assembly.outputPath);
                var assemblyPath = Path.Combine(m_OutputFolder, filename);
#pragma warning disable 618 // disable warning for obsolete AssemblyBuilder
                var assemblyBuilder = new AssemblyBuilder(assemblyPath, assembly.sourceFiles);
#pragma warning restore 618
                assemblyBuilder.buildTarget = Platform;
                assemblyBuilder.buildTargetGroup = BuildPipeline.GetBuildTargetGroup(Platform);
                assemblyBuilder.compilerOptions = new ScriptCompilerOptions
                {
                    AdditionalCompilerArguments = assembly.compilerOptions.AdditionalCompilerArguments,
                    AllowUnsafeCode = assembly.compilerOptions.AllowUnsafeCode,
                    ApiCompatibilityLevel = assembly.compilerOptions.ApiCompatibilityLevel,
                    CodeOptimization = CodeOptimization == CodeOptimization.Release ? UnityEditor.Compilation.CodeOptimization.Release : UnityEditor.Compilation.CodeOptimization.Debug, // assembly.compilerOptions.CodeOptimization,
                    RoslynAnalyzerDllPaths = RoslynAnalyzers ?? Array.Empty<string>()
                };

                // add asmdef-specific defines
                var additionalDefines = new List<string>(assembly.defines.Except(assemblyBuilder.defaultDefines));

                // DEVELOPMENT_BUILD
                assemblyBuilder.flags = AssemblyBuilderFlags.None;
                if ((CodeAnalysisFlags & CodeAnalysisFlags.DevelopmentBuild) != 0)
                    assemblyBuilder.flags |= AssemblyBuilderFlags.DevelopmentBuild;
                else
                    additionalDefines.Remove("DEVELOPMENT_BUILD"); // Checking Development Build in the Build Profile, can cause this define to appear in assembly.defines!

                // temp fix for UWP compilation error (failing to find references to Windows SDK assemblies)
                additionalDefines.Remove("ENABLE_WINMD_SUPPORT");
                additionalDefines.Remove("WINDOWS_UWP");

                additionalDefines.Add("ENABLE_UNITY_COLLECTIONS_CHECKS");
                assemblyBuilder.additionalDefines = additionalDefines.ToArray();

                // add references to assemblies we need to build
                assemblyBuilder.additionalReferences = assembly.assemblyReferences.Select(r => Path.Combine(m_OutputFolder, Path.GetFileName(r.outputPath))).ToArray();

                // exclude all assemblies that we are building ourselves to a Temp folder
                assemblyBuilder.excludeReferences =
                    assemblyBuilder.defaultReferences.Where(r => r.StartsWith("Library")).ToArray();

                assemblyBuilder.referencesOptions = ReferencesOptions.UseEngineModules;

                bool editorAssembly = (assembly.flags & AssemblyFlags.EditorAssembly) != 0;
                m_AssemblyCompilationTasks.Add(assemblyName, new AssemblyCompilationTask(assemblyBuilder, editorAssembly, CodeAnalysisFlags, CodeOwnerFlags)
                {
                    OnCompilationFinished = (result =>
                    {
                        progress?.Advance(assemblyName);

                        OnAssemblyCompilationFinished?.Invoke(result);
                    })
                });
            }

            // second pass: find all assembly reference builders
            foreach (var assembly in assemblies)
            {
                var dependencies = new List<AssemblyCompilationTask>();
                foreach (var referenceName in assembly.assemblyReferences.Select(r => Path.GetFileNameWithoutExtension(r.outputPath)))
                {
                    dependencies.Add(m_AssemblyCompilationTasks[referenceName]);
                }

                m_AssemblyCompilationTasks[assembly.name].AddDependencies(dependencies.ToArray());
            }
        }

        void UpdateAssemblyBuilders(IProgress progress)
        {
            while (true)
            {
                if (progress?.IsCancelled ?? false)
                    return; // compilation of assemblies will continue but we won't wait for it

                var pendingTasks = m_AssemblyCompilationTasks.Select(pair => pair.Value).Where(task => !task.IsCompleted).ToArray();
                if (!pendingTasks.Any())
                    break;
                foreach (var task in pendingTasks)
                {
                    task.Update();
                }
                System.Threading.Thread.Sleep(10);
            }
        }
    }
}
