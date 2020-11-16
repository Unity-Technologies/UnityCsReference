// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Utils;
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
            && EditorCompilationInstance.PrecompiledAssemblyProvider.GetRoslynAnalyzerPaths().Any()
            && PlayerSettings.EnableRoslynAnalyzers;

        private static CompilationTask CompilationTask { get; set; }

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
            EditorCompilationInstance.assemblyCompilationStarted += assembly =>
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
                string assemblyName = assembly.ConvertSeparatorsToUnity().Split('/').Last();

                // Since there are code changes in this assembly, we want to exclude it from the current round of compilation with Roslyn analyzers.
                CompilationTask?.TryRemovePendingAssembly(assemblyName);
            };

            EditorCompilationInstance.assemblyCompilationFinished += (assembly, m, o) =>
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

        public static bool IsRunning => CompilationTask != null;

        public const string KickOffCompilationProfilingName = "RoslynAnalysisRunner.KickOffCompilation";
        public const string PollProfilingName = "RoslynAnalysisRunner.Poll";

        static RoslynAnalysisRunner()
        {
            Console.WriteLine($"Invoked {nameof(RoslynAnalysisRunner)} static constructor.");

            EditorApplication.update += Poll;
            EditorApplication.playModeStateChanged += _ =>
            {
                CompilationTask?.Stop();
                CompilationTask?.Dispose();
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

        internal static CompilationTask TryCreateCompilationTask()
        {
            Profiler.BeginSample(KickOffCompilationProfilingName);
            ScriptAssembly[] assembliesToAnalyze = GetScriptAssembliesToAnalyze();

            if (!assembliesToAnalyze.Any())
            {
                Profiler.EndSample();
                return null;
            }

            Directory.CreateDirectory(BuildOutputDirectory);

            if (BuildOutputDirectoryInfo.Exists)
            {
                Console.WriteLine($"{BuildOutputDirectoryInfo.FullName} has been created.");
            }

            var compilationTask = CreateCompilationTask(assembliesToAnalyze);
            Console.WriteLine($"A new compilation task is started by {nameof(RoslynAnalysisRunner)} to compile these assemblies:");

            foreach (ScriptAssembly assembly in assembliesToAnalyze)
            {
                Console.WriteLine(assembly.Filename);
            }
            Console.WriteLine();

            Profiler.EndSample();
            return compilationTask;
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

        private static CompilationTask CreateCompilationTask(ScriptAssembly[] assemblies)
        {
            OnNewCompilationTaskStarted?.Invoke(assemblies.Select(a => a.Filename).ToArray());

            CompilationTask compilationTask = CompilationTask.RoslynAnalysis(assemblies, BuildOutputDirectory);
            compilationTask.OnCompilationTaskFinished += _ =>
            {
                Console.WriteLine($"A compilation task started by {nameof(RoslynAnalysisRunner)} has completed. These assemblies were compiled:");

                foreach (ScriptAssembly scriptAssembly in compilationTask.CompilerMessages.Keys)
                {
                    CompiledAssemblyCache.RemovePath(scriptAssembly.Filename);
                    Console.WriteLine(scriptAssembly.Filename);
                }

                CompilerMessage[] compilerMessages = compilationTask.CompilerMessages.Values.SelectMany(m => m).ToArray();
                PrintCompilerMessagesToConsole(compilerMessages);

                OnCurrentCompilationTaskFinished?.Invoke(compilerMessages);

                Directory.Delete(BuildOutputDirectory, recursive: true);
                Console.WriteLine($"{BuildOutputDirectoryInfo.FullName} has been deleted.");
            };
            return compilationTask;
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
                CompilationTask?.Stop();
                CompilationTask?.Dispose();

                CompilationTask = null;
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
            if (CompilationTask == null)
            {
                // Check whether there are assemblies that require compilation
                CompilationTask = TryCreateCompilationTask();
            }
            else
            {
                bool compilationFinished = CompilationTask.Poll();

                if (compilationFinished)
                {
                    Terminate();
                }
            }
            Profiler.EndSample();
        }
    }
}
