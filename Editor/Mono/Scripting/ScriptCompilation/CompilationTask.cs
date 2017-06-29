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
        int maxConcurrentCompilers;

        public event Action<ScriptAssembly, int> OnCompilationStarted;
        public event Action<ScriptAssembly, List<CompilerMessage>> OnCompilationFinished;

        public bool Stopped { get; private set; }
        public bool CompileErrors { get; private set; }

        public CompilationTask(ScriptAssembly[] scriptAssemblies, string buildOutputDirectory, EditorScriptCompilationOptions options, int maxConcurrentCompilers)
        {
            pendingAssemblies = new HashSet<ScriptAssembly>(scriptAssemblies);
            CompileErrors = false;
            this.buildOutputDirectory = buildOutputDirectory;
            this.options = options;
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
                        CompileErrors = messages.Any(m => m.type == CompilerMessageType.Error);

                    compilerTasks.Remove(assembly);
                    compiler.Dispose();
                }

            // Do not queue assemblies for compilation in case of compile errors.
            if (CompileErrors)
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

            // Find assemblies that have all their references already compiled.
            foreach (var pendingAssembly in pendingAssemblies)
            {
                bool compileAssembly = true;

                foreach (var reference in pendingAssembly.ScriptAssemblyReferences)
                {
                    if (!processedAssemblies.ContainsKey(reference))
                    {
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

            // No assemblies to compile, need to wait for more references to finish compiling.
            if (assemblyCompileQueue == null)
            {
                Debug.Assert(compilerTasks.Count > 0, "No pending assemblies queued for compilation and no compilers running. Compilation will never finish.");
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
