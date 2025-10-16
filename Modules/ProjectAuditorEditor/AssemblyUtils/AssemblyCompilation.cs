// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.AssemblyUtils
{
    enum CompilerMessageType
    {
        /// <summary>
        ///   <para>Error message.</para>
        /// </summary>
        Error,
        /// <summary>
        ///   <para>Warning message.</para>
        /// </summary>
        Warning,
        /// <summary>
        ///   <para>Info message.</para>
        /// </summary>
        Info
    }

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
        public CompilationMode CompilationMode = CompilationMode.Player;
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

        public AssemblyInfo[] Compile(IProgress progress = null)
        {
            var editorAssemblies = CompilationMode == CompilationMode.Editor || CompilationMode == CompilationMode.EditorPlayMode;
            var assemblies = GetAssemblies(editorAssemblies);

            if (AssemblyNames != null)
            {
                var assembliesAndDependencies = new List<Assembly>();
                foreach (var assembly in assemblies.Where(a => AssemblyNames.Contains(a.name)))
                {
                    CollectAssemblyDependencies(assembly, assembliesAndDependencies);
                }

                assemblies = assembliesAndDependencies.ToArray();
            }

            IEnumerable<string> compiledAssemblyPaths;
            if (editorAssemblies)
                compiledAssemblyPaths = GetEditorAssemblies(assemblies);
            else
                compiledAssemblyPaths = CompilePlayerAssemblies(assemblies, progress);

            return compiledAssemblyPaths.Select(AssemblyInfoProvider.GetAssemblyInfoFromAssemblyPath).ToArray();
        }

        static Assembly[] GetAssemblies(bool editorAssemblies)
        {
            var assemblies =
                UnityEditor.Compilation.CompilationPipeline.GetAssemblies(editorAssemblies
                    ? AssembliesType.Editor
                    : AssembliesType.PlayerWithoutTestAssemblies);

            return assemblies;
        }

        static void CollectAssemblyDependencies(Assembly assembly, List<Assembly> assembliesAndDependencies)
        {
            if (!assembliesAndDependencies.Contains(assembly))
                assembliesAndDependencies.Add(assembly);
            var missingDependencies = assembly.assemblyReferences.Where(d => !assembliesAndDependencies.Contains(d));
            foreach (var dependency in missingDependencies)
            {
                CollectAssemblyDependencies(dependency, assembliesAndDependencies);
            }
        }

        IEnumerable<string> GetEditorAssemblies(IEnumerable<Assembly> assemblies)
        {
            if (CompilationMode == CompilationMode.EditorPlayMode)
            {
                // exclude Editor-Only Assemblies
                assemblies = assemblies.Where(a => a.flags != AssemblyFlags.EditorAssembly);
            }
            return assemblies.Select(assembly => assembly.outputPath);
        }

        public static IEnumerable<string> GetAssemblyReferencePaths(CompilationMode compilationMode)
        {
            var editorAssemblies = compilationMode == CompilationMode.Editor || compilationMode == CompilationMode.EditorPlayMode;
            var paths = GetAssemblies(editorAssemblies)
                .SelectMany(a => a.compiledAssemblyReferences).Select(Path.GetDirectoryName).Distinct();
            return paths;
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

                switch (CompilationMode)
                {
                    case CompilationMode.Player:
                        assemblyBuilder.flags = AssemblyBuilderFlags.None;
                        break;
                    case CompilationMode.DevelopmentPlayer:
                        assemblyBuilder.flags = AssemblyBuilderFlags.DevelopmentBuild;
                        break;
                    case CompilationMode.Editor:
                        assemblyBuilder.flags = AssemblyBuilderFlags.EditorAssembly;
                        break;
                }

                // add asmdef-specific defines
                var additionalDefines = new List<string>(assembly.defines.Except(assemblyBuilder.defaultDefines));

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

                m_AssemblyCompilationTasks.Add(assemblyName, new AssemblyCompilationTask(assemblyBuilder)
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
