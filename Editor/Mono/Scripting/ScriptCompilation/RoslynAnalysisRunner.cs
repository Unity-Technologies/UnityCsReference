// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Utils;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditor.Scripting.ScriptCompilation
{
    [InitializeOnLoad]
    internal static class RoslynAnalysisRunner
    {
        private const string BuildOutputDirectory = "Temp/RoslynAnalysisRunner";
        private static string RoslynAnalysisRunnerId => nameof(RunRoslynAnalyzers);
        private static bool RunRoslynAnalyzers =>
            !EditorCompilationInstance.IsRunningRoslynAnalysisSynchronously
            && EditorCompilationInstance.PrecompiledAssemblyProvider.GetRoslynAnalyzerPaths().Any()
            && PlayerSettings.EnableRoslynAnalyzers;

        private static CompilationTask CompilationTask { get; set; }
        public static bool IsTerminated { get; private set; }

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
                if (RunRoslynAnalyzers)
                {
                    if (!CompiledAssemblyCache.Contains(RoslynAnalysisRunnerId))
                    {
                        // Add to CompiledAssemblyCache instead of SessionState.
                        // SessionState is part of a public API, so if we add this as
                        // a SessionState key, users will be able to clear/change it.
                        CompiledAssemblyCache.AddPath(RoslynAnalysisRunnerId);
                    }
                }
                else
                {
                    CompiledAssemblyCache.RemovePath(RoslynAnalysisRunnerId);
                }
                string assemblyName = assembly.ConvertSeparatorsToUnity().Split('/').Last();
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
                CompilationTask = null;
                Profiler.EndSample();
                return false;
            }
            if (!Directory.Exists(BuildOutputDirectory))
            {
                Directory.CreateDirectory(BuildOutputDirectory);
            }

            CompilationTask = CreateCompilationTask(assembliesToAnalyze);
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

        private static CompilationTask CreateCompilationTask(ScriptAssembly[] assemblies)
        {
            OnNewCompilationTaskStarted?.Invoke(assemblies.Select(a => a.Filename).ToArray());

            CompilationTask compilationTask = CompilationTask.RoslynAnalysis(assemblies, BuildOutputDirectory);
            compilationTask.OnCompilationTaskFinished += _ =>
            {
                foreach (string artifact in Directory.GetFiles(BuildOutputDirectory))
                {
                    File.Delete(artifact);
                }

                Console.WriteLine($"A compilation task started by {nameof(RoslynAnalysisRunner)} has completed. These assemblies were compiled:");

                foreach (ScriptAssembly scriptAssembly in compilationTask.CompilerMessages.Keys)
                {
                    CompiledAssemblyCache.RemovePath(scriptAssembly.Filename);
                    Console.WriteLine(scriptAssembly.Filename);
                }

                CompilerMessage[] compilerMessages = compilationTask.CompilerMessages.Values.SelectMany(m => m).ToArray();
                PrintCompilerMessagesToConsole(compilerMessages);

                compilationTask.Dispose();

                CompilationTask = null;
                OnCurrentCompilationTaskFinished?.Invoke(compilerMessages);
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
            if (CompilationTask == null)
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
                CompilationTask.Poll();
            }
            Profiler.EndSample();
        }
    }
}
