// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor.Scripting.Compilers;

namespace UnityEditor.Scripting.ScriptCompilation
{
    [Flags]
    enum CompilationTaskOptions
    {
        None = 0,
        StopOnFirstError = (1 << 0)
    }

    // CompilationTask represents one complete rebuild of all the ScriptAssembly's that are passed in the constructor.
    // The ScriptAssembly's are built in correct order according to their ScriptAssembly dependencies.
    class CompilationTask
    {
        HashSet<ScriptAssembly> pendingAssemblies;
        Dictionary<ScriptAssembly, CompilerMessage[]> processedAssemblies = new Dictionary<ScriptAssembly, CompilerMessage[]>();
        Dictionary<ScriptAssembly, ScriptCompilerBase> compilerTasks = new Dictionary<ScriptAssembly, ScriptCompilerBase>();
        string buildOutputDirectory;
        int compilePhase = 0;
        EditorScriptCompilationOptions options;
        CompilationTaskOptions compilationTaskOptions;
        int maxConcurrentCompilers;

        public event Action<ScriptAssembly, int> OnCompilationStarted;
        public event Action<ScriptAssembly, List<CompilerMessage>> OnCompilationFinished;

        public bool Stopped { get; private set; }
        public bool CompileErrors { get; private set; }

        public CompilationTask(ScriptAssembly[] scriptAssemblies, string buildOutputDirectory, EditorScriptCompilationOptions options, CompilationTaskOptions compilationTaskOptions, int maxConcurrentCompilers)
        {
            pendingAssemblies = new HashSet<ScriptAssembly>(scriptAssemblies);
            CompileErrors = false;
            this.buildOutputDirectory = buildOutputDirectory;
            this.options = options;
            this.compilationTaskOptions = compilationTaskOptions;
            this.maxConcurrentCompilers = maxConcurrentCompilers;
        }

        ~CompilationTask()
        {
            Stop();
        }

        public bool IsCompiling
        {
            get { return pendingAssemblies.Count > 0 || compilerTasks.Count > 0; }
        }

        public Dictionary<ScriptAssembly, CompilerMessage[]> CompilerMessages
        {
            get { return processedAssemblies; }
        }

        public void Stop()
        {
            if (Stopped)
                return;

            foreach (var task in compilerTasks)
            {
                var compiler = task.Value;
                compiler.Dispose();
            }

            compilerTasks.Clear();

            Stopped = true;
        }

        // Returns true when compilation is finished due to one of these reasons
        // * Was stopped (CompilationTask.Stopped will be true)
        // * Compilation had errors (CompilationTask.CompileErrors will be true)
        // * Compilation succesfully completed without errors.
        public bool Poll()
        {
            if (Stopped)
                return true;

            Dictionary<ScriptAssembly, ScriptCompilerBase> finishedCompilerTasks = null;

            // Check if any compiler processes are finished.
            foreach (var task in compilerTasks)
            {
                var compiler = task.Value;

                // Did compiler task finish?
                if (compiler.Poll())
                {
                    if (finishedCompilerTasks == null)
                        finishedCompilerTasks = new Dictionary<ScriptAssembly, ScriptCompilerBase>();

                    var assembly = task.Key;
                    finishedCompilerTasks.Add(assembly, compiler);
                }
            }

            // Save compiler messages from finished compiler processes and check for compile errors.
            if (finishedCompilerTasks != null)
                foreach (var task in finishedCompilerTasks)
                {
                    var assembly = task.Key;
                    var compiler = task.Value;

                    var messages = compiler.GetCompilerMessages();

                    // Convert messages to list, OnCompilationFinished callbacks might add
                    // more messages
                    var messagesList = messages.ToList();

                    if (OnCompilationFinished != null)
                        OnCompilationFinished(assembly, messagesList);

                    processedAssemblies.Add(assembly, messagesList.ToArray());

                    if (!CompileErrors)
                        CompileErrors = messagesList.Any(m => m.type == CompilerMessageType.Error);

                    compilerTasks.Remove(assembly);
                    compiler.Dispose();
                }

            // If StopOnFirstError is set, do not queue assemblies for compilation in case of compile errors.
            bool stopOnFirstError = (compilationTaskOptions & CompilationTaskOptions.StopOnFirstError) == CompilationTaskOptions.StopOnFirstError;

            if (stopOnFirstError && CompileErrors)
            {
                // Set empty compiler messages for all pending assemblies.
                // Makes handling of messages easier in the editor.
                if (pendingAssemblies.Count > 0)
                {
                    foreach (var pendingAssembly in pendingAssemblies)
                        processedAssemblies.Add(pendingAssembly, new CompilerMessage[0]);

                    pendingAssemblies.Clear();
                }

                return compilerTasks.Count == 0;
            }

            // Queue pending assemblies for compilation if we have no running compilers or if compilers have finished.
            if (compilerTasks.Count == 0 || (finishedCompilerTasks != null && finishedCompilerTasks.Count > 0))
                QueuePendingAssemblies();

            return pendingAssemblies.Count == 0 && compilerTasks.Count == 0;
        }

        void QueuePendingAssemblies()
        {
            if (pendingAssemblies.Count == 0)
                return;

            List<ScriptAssembly> assemblyCompileQueue = null;
            List<ScriptAssembly> removePendingAssemblies = null;

            // Find assemblies that have all their references already compiled.
            foreach (var pendingAssembly in pendingAssemblies)
            {
                bool compileAssembly = true;

                foreach (var reference in pendingAssembly.ScriptAssemblyReferences)
                {
                    CompilerMessage[] messages;

                    if (!processedAssemblies.TryGetValue(reference, out messages))
                    {
                        // If a reference is not compiling and not pending
                        // also remove this assembly from pending.
                        if (!compilerTasks.ContainsKey(reference) && !pendingAssemblies.Contains(reference))
                        {
                            if (removePendingAssemblies == null)
                                removePendingAssemblies = new List<ScriptAssembly>();

                            removePendingAssemblies.Add(pendingAssembly);
                        }

                        compileAssembly = false;
                        break;
                    }

                    // If reference has compile errors, do not compile the pending assembly.
                    bool compileErrors = messages.Any(m => m.type == CompilerMessageType.Error);

                    if (compileErrors)
                    {
                        if (removePendingAssemblies == null)
                            removePendingAssemblies = new List<ScriptAssembly>();

                        removePendingAssemblies.Add(pendingAssembly);

                        compileAssembly = false;
                        break;
                    }
                }

                if (compileAssembly)
                {
                    if (assemblyCompileQueue == null)
                        assemblyCompileQueue = new List<ScriptAssembly>();

                    assemblyCompileQueue.Add(pendingAssembly);
                }
            }

            if (removePendingAssemblies != null)
            {
                foreach (var assembly in removePendingAssemblies)
                    pendingAssemblies.Remove(assembly);

                // All pending assemblies were removed and no assemblies
                // were queued for compilation.
                if (assemblyCompileQueue == null)
                    return;
            }

            // No assemblies to compile, need to wait for more references to finish compiling.
            if (assemblyCompileQueue == null)
            {
                if (compilerTasks.Count() == 0)
                    throw new Exception("No pending assemblies queued for compilation and no compilers running. Compilation will never finish.");
                return;
            }

            bool buildingForEditor = (options & EditorScriptCompilationOptions.BuildingForEditor) == EditorScriptCompilationOptions.BuildingForEditor;

            // Begin compiling any queued assemblies
            foreach (var assembly in assemblyCompileQueue)
            {
                pendingAssemblies.Remove(assembly);
                var island = assembly.ToMonoIsland(options, buildOutputDirectory);
                var compiler = ScriptCompilers.CreateCompilerInstance(island, buildingForEditor, island._target, assembly.RunUpdater);

                compilerTasks.Add(assembly, compiler);

                // Start compiler process
                compiler.BeginCompiling();

                if (OnCompilationStarted != null)
                    OnCompilationStarted(assembly, compilePhase);

                if (compilerTasks.Count == maxConcurrentCompilers)
                    break;
            }

            compilePhase++;
        }
    }
}
