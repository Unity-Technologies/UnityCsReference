// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using Bee.BeeDriver;
using ScriptCompilationBuildProgram.Data;
using UnityEditor.Scripting.Compilers;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditor.Scripting.ScriptCompilation
{
    // When compiling a Unity project that contains Roslyn analyzers, we run two rounds of compilation.
    // The first round of compilation is run without Roslyn analyzers. This is to ensure that our users
    // do not experience any slow-down in iteration speeds when making code changes in their projects.
    // After the first round finishes, a domain reload happens, and we kick off a second round of compilation,
    // this time including Roslyn analyzers. The second round of compilation is run asynchronously in the background.
    [InitializeOnLoad]
    internal static class RoslynAnalysisRunner
    {
        private const string BuildOutputDirectory = "Temp/RoslynAnalysisRunner";

        private static DirectoryInfo BuildOutputDirectoryInfo = new DirectoryInfo(BuildOutputDirectory);
        private static string RoslynAnalysisRunnerId => nameof(RunRoslynAnalyzers);
        private static bool RunRoslynAnalyzers =>
            !EditorCompilationInstance.IsRunningRoslynAnalysisSynchronously
            && !AssetDatabase.IsAssetImportWorkerProcess()
            && EditorCompilationInstance.PrecompiledAssemblyProvider.GetRoslynAnalyzerPaths().Any()
            && PlayerSettings.EnableRoslynAnalyzers;

        private static BeeDriver ActiveBeeBuild { get; set; }

        // This field is set during testing to ensure that we do not invoke CompiledAssemblyCache.GetAllPaths(), which in turn invokes native code
        private static Func<string[]> GetCandidateAssembliesForRoslynAnalysis { get; set; }

        private static EditorCompilation _editorCompilationInstance;
        private static EditorCompilation EditorCompilationInstance
        {
            get => _editorCompilationInstance;
            set
            {
                _editorCompilationInstance = value;
                SubscribeToEvents();
            }
        }

        private static void SubscribeToEvents()
        {
            EditorCompilationInstance.compilationStarted += _ =>
            {
                // When the first round of compilation starts, we check whether we should run Roslyn analyzers in the project.
                if (RunRoslynAnalyzers)
                {
                    // If we should kick off a second round of compilation with Roslyn analyzers, then a tag is stored
                    // in the CompiledAssemblyCache, which is maintained in native code. The contents of the CompiledAssemblyCache
                    // are able to survive domain reload. After the first round of compilation finishes and the domain reloads, we
                    // will check whether this tag exists in the CompiledAssemblyCache. If it exists, then a second round of
                    // compilation is kicked off.
                    if (!CompiledAssemblyCache.Contains(RoslynAnalysisRunnerId))
                    {
                        CompiledAssemblyCache.AddPath(RoslynAnalysisRunnerId);
                    }
                }
                else
                {
                    CompiledAssemblyCache.RemovePath(RoslynAnalysisRunnerId);
                }
            };

            EditorCompilationInstance.assemblyCompilationFinished += (assembly, _) =>
            {
                if (RunRoslynAnalyzers && !assembly.HasCompileErrors && (assembly.Flags & AssemblyFlags.CandidateForCompilingWithRoslynAnalyzers) != 0)
                {
                    CompiledAssemblyCache.AddPath(assembly.Filename);
                    IsTerminated = false;
                }
            };
        }

        public static bool ShouldRun { get; private set; }
        public static bool IsTerminated { get; private set; }

        public static event Action OnTermination;

        public static event Action<CompilerMessage[]> OnCurrentCompilationTaskFinished;
        public static event Action<string[]> OnNewCompilationTaskStarted;

        public static bool IsRunning => ActiveBeeBuild != null;

        public const string KickOffCompilationProfilingName = "RoslynAnalysisRunner.KickOffCompilation";
        public const string PollProfilingName = "RoslynAnalysisRunner.Poll";

        static RoslynAnalysisRunner()
        {
            // We never want to run this on Asset Import worker processes. For one, doing so
            // would be a waste of resources, but it will also interfere with analyzers running
            // on the main process, as it can delete the temp directory while the other process
            // is still using it.
            if (AssetDatabase.IsAssetImportWorkerProcess())
                return;

            EditorApplication.update += Poll;
            EditorApplication.playModeStateChanged += _ =>
            {
                ActiveBeeBuild?.Dispose();
            };
            AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                ActiveBeeBuild?.Dispose();
            };
            EditorCompilationInstance = EditorCompilationInterface.Instance;
            CheckShouldRun();
        }

        // This method is also called from tests.
        internal static void CheckShouldRun()
        {
            ShouldRun = CompiledAssemblyCache.Contains(RoslynAnalysisRunnerId);
            CompiledAssemblyCache.RemovePath(RoslynAnalysisRunnerId);

            if (!ShouldRun)
            {
                Console.WriteLine($"{nameof(RoslynAnalysisRunner)} will not be running.");
                Terminate();
            }
        }

        internal static bool TryCreateCompilationTask()
        {
            Profiler.BeginSample(KickOffCompilationProfilingName);
            ScriptAssembly[] assembliesToAnalyze = GetScriptAssembliesToAnalyze();

            if (!assembliesToAnalyze.Any())
            {
                ActiveBeeBuild = null;
                Profiler.EndSample();
                return false;
            }

            Directory.CreateDirectory(BuildOutputDirectory);

            if (BuildOutputDirectoryInfo.Exists)
            {
                Console.WriteLine($"{BuildOutputDirectoryInfo.FullName} has been created.");
            }

            ActiveBeeBuild = CreateCompilationTask(assembliesToAnalyze);
            Console.WriteLine($"A new compilation task is started by {nameof(RoslynAnalysisRunner)} to compiled these assemblies:");

            foreach (ScriptAssembly assembly in assembliesToAnalyze)
            {
                Console.WriteLine(assembly.Filename);
            }
            Console.WriteLine();

            Profiler.EndSample();
            return true;
        }

        internal static ScriptAssembly[] GetScriptAssembliesToAnalyze()
        {
            string[] candidateAssemblies =
                GetCandidateAssembliesForRoslynAnalysis != null
                ? GetCandidateAssembliesForRoslynAnalysis()
                : CompiledAssemblyCache.GetAllPaths();

            return
                !candidateAssemblies.Any()
                ? new ScriptAssembly[0]
                : EditorCompilationInstance.GetScriptAssembliesForRoslynAnalysis(candidateAssemblies);
        }

        private static BeeDriver CreateCompilationTask(ScriptAssembly[] scriptAssemblies)
        {
            var config = "A";
            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var activeBeeBuild = UnityBeeDriver.Make(EditorCompilation.ScriptCompilationBuildProgram, EditorCompilationInstance, $"{(int)buildTarget}{config}", useScriptUpdater: false);

            BeeScriptCompilation.AddScriptCompilationData(activeBeeBuild, EditorCompilationInstance, scriptAssemblies, true, BuildOutputDirectory, buildTarget, true);

            activeBeeBuild.BuildAsync(Constants.ScriptAssembliesTarget);

            OnNewCompilationTaskStarted?.Invoke(scriptAssemblies.Select(a => a.Filename).ToArray());

            return activeBeeBuild;
        }

        private static void PrintCompilerMessagesToConsole(CompilerMessage[] compilerMessages)
        {
            foreach (CompilerMessage message in compilerMessages)
            {
                switch (message.type)
                {
                    case CompilerMessageType.Error:
                        Debug.LogError(message.message, message.file, message.line, message.column);
                        break;
                    case CompilerMessageType.Warning:
                        Debug.LogWarning(message.message, message.file, message.line, message.column);
                        break;
                    case CompilerMessageType.Information:
                        Debug.LogInfo(message.message, message.file, message.line, message.column);
                        break;
                }
            }
        }

        internal static void SetUpTestRun(EditorCompilation editorCompilation, Func<string[]> getCandidateAssembliesForRoslynAnalysis)
        {
            GetCandidateAssembliesForRoslynAnalysis = getCandidateAssembliesForRoslynAnalysis;
            EditorCompilationInstance = editorCompilation;
            OnTermination = null;
        }

        internal static void Terminate()
        {
            try
            {
                ActiveBeeBuild?.Dispose();

                if (Directory.Exists(BuildOutputDirectory))
                {
                    Directory.Delete(BuildOutputDirectory, recursive: true);
                    Console.WriteLine($"{BuildOutputDirectory} has been deleted.");
                }
            }
            finally
            {
                Console.WriteLine($"{nameof(RoslynAnalysisRunner)} has terminated.");

                IsTerminated = true;
                OnTermination?.Invoke();
            }
        }

        internal static void Poll()
        {
            Profiler.BeginSample(PollProfilingName);
            if (!ShouldRun || IsTerminated)
            {
                Profiler.EndSample();
                return;
            }

            // If there is no ongoing compilation task
            if (ActiveBeeBuild == null)
            {
                // Check whether there are assemblies that require compilation
                bool compile = TryCreateCompilationTask();

                // There are no outstanding assemblies to compile
                if (!compile)
                {
                    Terminate();
                }
            }
            else
            {
                var res = ActiveBeeBuild.Tick();
                if (res != null)
                {
                    var compilerMessages = BeeScriptCompilation.ParseAllNodeResultsIntoCompilerMessages(res.NodeResults, EditorCompilationInterface.Instance).SelectMany(m => m).ToArray();

                    PrintCompilerMessagesToConsole(compilerMessages);

                    try
                    {
                        OnCurrentCompilationTaskFinished?.Invoke(compilerMessages);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }

                    Terminate();
                    ActiveBeeBuild = null;
                }
            }

            Profiler.EndSample();
        }
    }
}
