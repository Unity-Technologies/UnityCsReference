// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnityEditor.Scripting.Compilers;
using System.IO;

namespace UnityEditor.Scripting.ScriptCompilation
{
    [Flags]
    enum CompilationTaskOptions
    {
        None = 0,
        StopOnFirstError = (1 << 0),
        RunPostProcessors = (1 << 1)
    }

    // CompilationTask represents one complete rebuild of all the ScriptAssembly's that are passed in the constructor.
    // The ScriptAssembly's are built in correct order according to their ScriptAssembly dependencies.
    class CompilationTask
    {
        enum CompilionTaskState
        {
            Started = 0,
            Running = 1,
            Finished = 2
        }

        HashSet<ScriptAssembly> pendingAssemblies;
        Dictionary<ScriptAssembly, CompilerMessage[]> compiledAssemblies = new Dictionary<ScriptAssembly, CompilerMessage[]>();
        Dictionary<ScriptAssembly, CompilerMessage[]> processedAssemblies = new Dictionary<ScriptAssembly, CompilerMessage[]>();
        Dictionary<ScriptAssembly, ScriptCompilerBase> compilerTasks = new Dictionary<ScriptAssembly, ScriptCompilerBase>();
        List<PostProcessorTask> postProcessorTasks = new List<PostProcessorTask>();
        List<PostProcessorTask> pendingPostProcessorTasks = new List<PostProcessorTask>();

        string buildOutputDirectory;
        object context;
        int compilePhase = 0;
        EditorScriptCompilationOptions options;
        CompilationTaskOptions compilationTaskOptions;
        int maxConcurrentCompilers;
        CompilionTaskState state = CompilionTaskState.Started;
        IILPostProcessing ilPostProcessing;

        public event Action<object> OnCompilationTaskStarted;
        public event Action<object> OnCompilationTaskFinished;
        public event Action<ScriptAssembly, int> OnBeforeCompilationStarted;
        public event Action<ScriptAssembly, int> OnCompilationStarted;
        public event Action<ScriptAssembly> OnPostProcessingStarted;
        public event Action<ScriptAssembly, List<CompilerMessage>> OnCompilationFinished;

        public bool Stopped { get; private set; }
        public bool CompileErrors { get; private set; }

        public CompilationTask(ScriptAssembly[] scriptAssemblies,
                               string buildOutputDirectory,
                               object context,
                               EditorScriptCompilationOptions options,
                               CompilationTaskOptions compilationTaskOptions,
                               int maxConcurrentCompilers,
                               IILPostProcessing ilPostProcessing)
        {
            pendingAssemblies = new HashSet<ScriptAssembly>(scriptAssemblies);
            CompileErrors = false;
            this.buildOutputDirectory = buildOutputDirectory;
            this.context = context;
            this.options = options;
            this.compilationTaskOptions = compilationTaskOptions;
            this.maxConcurrentCompilers = maxConcurrentCompilers;
            this.ilPostProcessing = ilPostProcessing;
        }

        public bool IsCompiling
        {
            get { return pendingAssemblies.Count > 0 || compilerTasks.Count > 0 || postProcessorTasks.Count > 0; }
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

        void HandleOnCompilationTaskStarted()
        {
            if (state == CompilionTaskState.Started)
            {
                if (OnCompilationTaskStarted != null)
                    OnCompilationTaskStarted(context);

                state = CompilionTaskState.Running;
            }
        }

        void HandleOnCompilationTaskFinished()
        {
            if (state == CompilionTaskState.Running)
            {
                if (OnCompilationTaskFinished != null)
                    OnCompilationTaskFinished(context);

                state = CompilionTaskState.Finished;
            }
        }

        // Returns true when compilation is finished due to one of these reasons
        // * Was stopped (CompilationTask.Stopped will be true)
        // * Compilation had errors (CompilationTask.CompileErrors will be true)
        // * Compilation succesfully completed without errors.
        public bool Poll()
        {
            HandleOnCompilationTaskStarted();

            if (Stopped)
            {
                HandleOnCompilationTaskFinished();
                return true;
            }

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
                    var messagesList = messages.ToList();

                    compiledAssemblies.Add(assembly, messagesList.ToArray());

                    if (RunPostProcessors && !messagesList.Any(m => m.type == CompilerMessageType.Error))
                    {
                        var postProcessorTask = new PostProcessorTask(assembly, messagesList, buildOutputDirectory, ilPostProcessing.PostProcess);
                        pendingPostProcessorTasks.Add(postProcessorTask);
                    }
                    else
                    {
                        // OnCompilationFinished callbacks might add more compiler messages
                        OnCompilationFinished?.Invoke(assembly, messagesList);
                        processedAssemblies.Add(assembly, messagesList.ToArray());
                    }

                    if (!CompileErrors)
                        CompileErrors = messagesList.Any(m => m.type == CompilerMessageType.Error);

                    compilerTasks.Remove(assembly);
                    compiler.Dispose();
                }

            List<PostProcessorTask> startedPostProcessorTasks = null;

            // Check if any pending post processors can be run
            foreach (var postProcessorTask in pendingPostProcessorTasks)
            {
                var assembly = postProcessorTask.Assembly;

                // We break out of this loop instead to continuing to ensure that
                // OnCompilationFinished events are emitted in the same order as
                // they finished compiling.
                if (IsAnyProcessUsingAssembly(assembly))
                    break;

                if (startedPostProcessorTasks == null)
                {
                    startedPostProcessorTasks = new List<PostProcessorTask>();
                }

                startedPostProcessorTasks.Add(postProcessorTask);

                var assemblySourcePath = AssetPath.Combine(buildOutputDirectory, assembly.Filename);
                var pdbSourcePath = AssetPath.Combine(buildOutputDirectory, assembly.PdbFilename);

                try
                {
                    File.Copy(assemblySourcePath, assembly.FullPath, true);
                    File.Copy(pdbSourcePath, assembly.PdbFullPath, true);

                    postProcessorTask.Poll();
                    OnPostProcessingStarted?.Invoke(assembly);

                    postProcessorTasks.Add(postProcessorTask);
                }
                catch (IOException e)
                {
                    var messagesList = postProcessorTask.CompilerMessages;

                    UnityEngine.Debug.LogError($"Fail to copy {assemblySourcePath} or {pdbSourcePath} to {AssetPath.GetDirectoryName(assembly.FullPath)} before post processing the assembly. Skipping post processing.\n{e}");
                    // OnCompilationFinished callbacks might add more compiler messages
                    OnCompilationFinished?.Invoke(assembly, messagesList);
                    processedAssemblies.Add(assembly, messagesList.ToArray());
                }
            }

            if (startedPostProcessorTasks != null)
                foreach (var postProcessorTask in startedPostProcessorTasks)
                {
                    pendingPostProcessorTasks.Remove(postProcessorTask);
                }

            HashSet<PostProcessorTask> finishedPostProcessorTasks = null;

            foreach (var postProcessorTask in postProcessorTasks)
            {
                // We break out of this loop instead to continuing to ensure that
                // OnCompilationFinished events are emitted in the same order as
                // they finished compiling.

                if (!postProcessorTask.Poll())
                    break;

                // Do not copy the post processed assembly in OnCompilationFinished
                // if any of the running compilers have a reference to the assembly.
                // As we might copy it while the compiler has the assembly open.
                if (IsAnyProcessUsingAssembly(postProcessorTask.Assembly))
                    break;

                var messagesList = postProcessorTask.CompilerMessages;

                // OnCompilationFinished callbacks might add more compiler messages
                OnCompilationFinished?.Invoke(postProcessorTask.Assembly, messagesList);
                processedAssemblies.Add(postProcessorTask.Assembly, messagesList.ToArray());

                if (!CompileErrors)
                    CompileErrors = messagesList.Any(m => m.type == CompilerMessageType.Error);

                if (finishedPostProcessorTasks == null)
                    finishedPostProcessorTasks = new HashSet<PostProcessorTask>();

                finishedPostProcessorTasks.Add(postProcessorTask);
            }

            if (finishedPostProcessorTasks != null)
                foreach (var finishedPostProcessorTask in finishedPostProcessorTasks)
                {
                    postProcessorTasks.Remove(finishedPostProcessorTask);
                }

            // If StopOnFirstError is set, do not queue assemblies for compilation in case of compile errors.
            bool stopOnFirstError = (compilationTaskOptions & CompilationTaskOptions.StopOnFirstError) == CompilationTaskOptions.StopOnFirstError;

            if (stopOnFirstError && CompileErrors)
            {
                pendingAssemblies.Clear();

                if (FinishedCompilation)
                {
                    HandleOnCompilationTaskFinished();
                }

                return FinishedCompilation;
            }

            // Queue pending assemblies for compilation if we have no running compilers or if compilers have finished.
            if (compilerTasks.Count == 0 || (finishedCompilerTasks != null && finishedCompilerTasks.Count > 0))
                QueuePendingAssemblies();

            if (FinishedCompilation)
            {
                HandleOnCompilationTaskFinished();
            }

            return FinishedCompilation;
        }

        bool RunPostProcessors
        {
            get
            {
                return (compilationTaskOptions & CompilationTaskOptions.RunPostProcessors) > 0;
            }
        }

        bool FinishedCompilation
        {
            get
            {
                return pendingAssemblies.Count == 0 && compilerTasks.Count == 0 && postProcessorTasks.Count == 0;
            }
        }

        bool IsAnyProcessUsingAssembly(ScriptAssembly assembly)
        {
            if (AnyRunningCompilerHasReference(assembly))
                return true;

            if (ilPostProcessing != null &&
                ilPostProcessing.IsAnyRunningPostProcessorUsingAssembly(assembly))
                return true;

            return false;
        }

        bool AnyRunningCompilerHasReference(ScriptAssembly assembly)
        {
            foreach (var compilerTask in compilerTasks)
            {
                var compilerAssembly = compilerTask.Key;

                if (compilerAssembly.ScriptAssemblyReferences.Contains(assembly))
                    return true;
            }

            return false;
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

                    if (!compiledAssemblies.TryGetValue(reference, out messages))
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

            // Begin compiling any queued assemblies
            foreach (var assembly in assemblyCompileQueue)
            {
                pendingAssemblies.Remove(assembly);

                if (assembly.CallOnBeforeCompilationStarted && OnBeforeCompilationStarted != null)
                    OnBeforeCompilationStarted(assembly, compilePhase);

                var compiler = ScriptCompilers.CreateCompilerInstance(assembly, options, buildOutputDirectory);

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
