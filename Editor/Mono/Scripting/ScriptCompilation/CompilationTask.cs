// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.Scripting.Compilers;
using UnityEngine.Profiling;
using Unity.Scripting.Compilation;

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
    class CompilationTask : IDisposable
    {
        enum CompilionTaskState
        {
            Started = 0,
            Running = 1,
            Finished = 2
        }

        List<ScriptAssembly> pendingAssemblies;
        HashSet<ScriptAssembly> codeGenAssemblies;
        HashSet<ScriptAssembly> compiledCodeGenAssemblies = new HashSet<ScriptAssembly>();
        HashSet<ScriptAssembly> notCompiledCodeGenAssemblies = new HashSet<ScriptAssembly>();
        Dictionary<ScriptAssembly, CompilerMessage[]> compiledAssemblies = new Dictionary<ScriptAssembly, CompilerMessage[]>();
        Dictionary<ScriptAssembly, CompilerMessage[]> processedAssemblies = new Dictionary<ScriptAssembly, CompilerMessage[]>();
        Dictionary<ScriptAssembly, ScriptCompilerBase> compilerTasks = new Dictionary<ScriptAssembly, ScriptCompilerBase>();
        HashSet<ScriptAssembly> unchangedCompiledAssemblies = new HashSet<ScriptAssembly>();
        List<PostProcessorTask> postProcessorTasks = new List<PostProcessorTask>();
        List<PostProcessorTask> pendingPostProcessorTasks = new List<PostProcessorTask>();

        ScriptAssembly[] scriptAssemblies;
        string buildOutputDirectory;
        object context;
        int compilePhase = 0;
        EditorScriptCompilationOptions options;
        CompilationTaskOptions compilationTaskOptions;
        int maxConcurrentCompilers;
        CompilionTaskState state = CompilionTaskState.Started;
        IILPostProcessing ilPostProcessing;
        private readonly CompilerFactory compilerFactory;
        StreamWriter logWriter;

        public event Action<object> OnCompilationTaskStarted;
        public event Action<object> OnCompilationTaskFinished;
        public event Action<ScriptAssembly, int> OnBeforeCompilationStarted;
        public event Action<ScriptAssembly, int> OnCompilationStarted;
        public event Action<ScriptAssembly> OnPostProcessingStarted;
        public event Action<ScriptAssembly, List<CompilerMessage>> OnCompilationFinished;

        public bool Stopped { get; private set; }
        public bool CompileErrors { get; private set; }

        bool BuildingForEditor
        {
            get
            {
                return (options & EditorScriptCompilationOptions.BuildingForEditor) == EditorScriptCompilationOptions.BuildingForEditor;
            }
        }

        bool UseReferenceAssemblies
        {
            get
            {
                return (options & EditorScriptCompilationOptions.BuildingUseReferenceAssemblies) == EditorScriptCompilationOptions.BuildingUseReferenceAssemblies;
            }
        }

        public CompilationTask(ScriptAssembly[] scriptAssemblies,
                               ScriptAssembly[] codeGenAssemblies,
                               string buildOutputDirectory,
                               object context,
                               EditorScriptCompilationOptions options,
                               CompilationTaskOptions compilationTaskOptions,
                               int maxConcurrentCompilers,
                               IILPostProcessing ilPostProcessing,
                               CompilerFactory compilerFactory)
        {
            this.scriptAssemblies = scriptAssemblies;
            pendingAssemblies = new List<ScriptAssembly>();

            if (codeGenAssemblies != null)
                this.codeGenAssemblies = new HashSet<ScriptAssembly>(codeGenAssemblies);
            else
                this.codeGenAssemblies = new HashSet<ScriptAssembly>();

            // Try to queue codegen assemblies for compilation first,
            // so they get compiled as soon as possible.
            if (codeGenAssemblies != null && codeGenAssemblies.Count() > 0)
                pendingAssemblies.AddRange(codeGenAssemblies);

            pendingAssemblies.AddRange(scriptAssemblies);

            CompileErrors = false;
            this.buildOutputDirectory = buildOutputDirectory;
            this.context = context;
            this.options = options;
            this.compilationTaskOptions = compilationTaskOptions;
            this.maxConcurrentCompilers = maxConcurrentCompilers;
            this.ilPostProcessing = ilPostProcessing;
            this.compilerFactory = compilerFactory;

            try
            {
                logWriter = File.CreateText(LogFilePath);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not create text file {LogFilePath}\n{e}");
            }
        }

        public ScriptAssembly[] ScriptAssemblies
        {
            get
            {
                return scriptAssemblies;
            }
        }

        public HashSet<ScriptAssembly> CodeGenAssemblies
        {
            get
            {
                return codeGenAssemblies;
            }
        }

        public bool AreAllCodegenAssembliesCompiled
        {
            get
            {
                return codeGenAssemblies.Count == compiledCodeGenAssemblies.Count;
            }
        }

        string LogFilePath
        {
            get
            {
                return AssetPath.Combine(buildOutputDirectory, "CompilationLog.txt");
            }
        }

        public void Dispose()
        {
            if (logWriter != null)
                logWriter.Dispose();
        }

        public bool IsCompiling
        {
            get { return pendingAssemblies.Count > 0 || compilerTasks.Count > 0 || pendingPostProcessorTasks.Count > 0 || postProcessorTasks.Count > 0; }
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

                    assembly.HasCompileErrors = messagesList.Any(m => m.type == CompilerMessageType.Error);
                    compiledAssemblies.Add(assembly, messagesList.ToArray());

                    bool havePostProcessors = ilPostProcessing != null && ilPostProcessing.HasPostProcessors;
                    bool isCodeGenAssembly = codeGenAssemblies.Contains(assembly);
                    bool hasCompileErrors = messagesList.Any(m => m.type == CompilerMessageType.Error);

                    if (isCodeGenAssembly)
                    {
                        if (hasCompileErrors)
                            notCompiledCodeGenAssemblies.Add(assembly);
                        else
                            compiledCodeGenAssemblies.Add(assembly);
                    }

                    if (havePostProcessors &&
                        notCompiledCodeGenAssemblies.Count == 0 &&
                        !hasCompileErrors &&
                        !isCodeGenAssembly)
                    {
                        var assemblySourcePath = AssetPath.Combine(buildOutputDirectory, assembly.Filename);
                        var pdbSourcePath = AssetPath.Combine(buildOutputDirectory, assembly.PdbFilename);

                        try
                        {
                            if (assemblySourcePath != assembly.FullPath)
                                File.Copy(assemblySourcePath, assembly.FullPath, true);

                            if (pdbSourcePath != assembly.PdbFullPath)
                                File.Copy(pdbSourcePath, assembly.PdbFullPath, true);

                            var postProcessorTask = new PostProcessorTask(assembly, messagesList, buildOutputDirectory, ilPostProcessing);
                            pendingPostProcessorTasks.Add(postProcessorTask);
                        }
                        catch (IOException e)
                        {
                            UnityEngine.Debug.LogError($"Fail to copy {assemblySourcePath} or {pdbSourcePath} to {AssetPath.GetDirectoryName(assembly.FullPath)} before post processing the assembly. Skipping post processing.\n{e}");
                            // OnCompilationFinished callbacks might add more compiler messages
                            OnCompilationFinished?.Invoke(assembly, messagesList);
                            processedAssemblies.Add(assembly, messagesList.ToArray());
                        }
                    }
                    else
                    {
                        // OnCompilationFinished callbacks might add more compiler messages
                        OnCompilationFinished?.Invoke(assembly, messagesList);
                        processedAssemblies.Add(assembly, messagesList.ToArray());
                    }

                    if (!CompileErrors)
                        CompileErrors = assembly.HasCompileErrors;

                    try
                    {
                        var referenceAssemblyPath = AssetPath.Combine(buildOutputDirectory, assembly.ReferenceAssemblyFilename);

                        // If a reference assembly is built, check if it is unchanged
                        // since the previous compilation.
                        if (UseReferenceAssemblies &&
                            BuildingForEditor &&
                            File.Exists(referenceAssemblyPath))
                        {
                            Profiler.BeginSample("ReferenceAssemblyHelpers.IsReferenceAssemblyUnchanged");
                            bool isUnchanged = ReferenceAssemblyHelpers.IsReferenceAssemblyUnchanged(assembly, buildOutputDirectory);
                            Profiler.EndSample();

                            if (isUnchanged)
                            {
                                unchangedCompiledAssemblies.Add(assembly);
                            }
                        }

                        File.Delete(referenceAssemblyPath);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }

                    // If a codgen / IL Post processor has compile errors, clear
                    // pending assemblies waiting for compilation and assemblies
                    // waiting to get post processed.
                    if (isCodeGenAssembly && hasCompileErrors)
                    {
                        pendingPostProcessorTasks.Clear();
                        pendingAssemblies.Clear();
                    }

                    compilerTasks.Remove(assembly);
                    compiler.Dispose();
                }


            if (ilPostProcessing != null && ilPostProcessing.HasPostProcessors)
            {
                PollPostProcessors();
            }

            // If StopOnFirstError is set, do not queue assemblies for compilation in case of compile errors.
            bool stopOnFirstError = (compilationTaskOptions & CompilationTaskOptions.StopOnFirstError) == CompilationTaskOptions.StopOnFirstError;

            if (stopOnFirstError && CompileErrors)
            {
                foreach (var pendingAssembly in pendingAssemblies)
                {
                    if (UnityCodeGenHelpers.IsCodeGen(pendingAssembly.Filename))
                        notCompiledCodeGenAssemblies.Add(pendingAssembly);
                }

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

        void PollPostProcessors()
        {
            if (pendingPostProcessorTasks.Count == 0 && postProcessorTasks.Count == 0)
                return;

            // Not all codegen assemblies have been compiled yet.
            if (!AreAllCodegenAssembliesCompiled)
            {
                // If any codegen assemblies are not getting compiled, clear
                // pending il post processing tasks.
                if (notCompiledCodeGenAssemblies.Any())
                    pendingPostProcessorTasks.Clear();

                return;
            }

            List<PostProcessorTask> startedPostProcessorTasks = null;

            // Check if any pending post processors can be run
            foreach (var postProcessorTask in pendingPostProcessorTasks)
            {
                if (RunningMaxConcurrentProcesses)
                    break;

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

                postProcessorTask.Start();
                LogStartInfo($"# Starting IL post processing on {assembly.Filename}", postProcessorTask.GetProcessStartInfo());

                OnPostProcessingStarted?.Invoke(assembly);

                postProcessorTasks.Add(postProcessorTask);
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

                var messagesList = postProcessorTask.GetCompilerMessages();

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
                    finishedPostProcessorTask.Dispose();
                    postProcessorTasks.Remove(finishedPostProcessorTask);
                }
        }

        bool FinishedCompilation
        {
            get
            {
                return pendingAssemblies.Count == 0 &&
                    compilerTasks.Count == 0 &&
                    postProcessorTasks.Count == 0 &&
                    pendingPostProcessorTasks.Count == 0;
            }
        }

        bool RunningMaxConcurrentProcesses
        {
            get
            {
                return (compilerTasks.Count + postProcessorTasks.Count) >= maxConcurrentCompilers;
            }
        }

        bool IsAnyProcessUsingAssembly(ScriptAssembly assembly)
        {
            if (IsAnyRunningCompilerUsingAssembly(assembly))
                return true;

            if (IsAnyRunningPostProcessorUsingAssembly(assembly))
                return true;

            return false;
        }

        bool IsAnyRunningCompilerUsingAssembly(ScriptAssembly assembly)
        {
            foreach (var compilerTask in compilerTasks)
            {
                var compilerAssembly = compilerTask.Key;

                if (compilerAssembly.ScriptAssemblyReferences.Contains(assembly))
                    return true;
            }

            return false;
        }

        bool IsAnyRunningPostProcessorUsingAssembly(ScriptAssembly assembly)
        {
            foreach (var postProcessorTask in postProcessorTasks)
            {
                if (postProcessorTask.IsFinished)
                    continue;

                var postProcessorAssembly = postProcessorTask.Assembly;

                if (postProcessorAssembly.ScriptAssemblyReferences.Contains(assembly))
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

                int unchangedAssemblyReferencesCount = 0;

                foreach (var reference in pendingAssembly.ScriptAssemblyReferences)
                {
                    CompilerMessage[] messages;

                    if (unchangedCompiledAssemblies.Contains(reference))
                    {
                        unchangedAssemblyReferencesCount++;
                        continue;
                    }

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

                // If the assembly exists and is up to date (no compile errors)
                // and it was dirtied because it is a reference to one or more
                // dirty assemblies and none of them changed, then we do not
                // need to recompile the assembly.
                // Most expensive checks at the bottom.
                if (UseReferenceAssemblies &&
                    BuildingForEditor &&
                    !pendingAssembly.HasCompileErrors &&
                    pendingAssembly.DirtySource == DirtySource.DirtyReference &&
                    unchangedAssemblyReferencesCount > 0 &&
                    unchangedAssemblyReferencesCount == pendingAssembly.ScriptAssemblyReferences.Length &&
                    File.Exists(pendingAssembly.FullPath))
                {
                    var assemblyOutputPath = AssetPath.Combine(pendingAssembly.OutputDirectory, pendingAssembly.Filename);

                    Console.WriteLine($"- Skipping compile {assemblyOutputPath} because all references are unchanged");

                    unchangedCompiledAssemblies.Add(pendingAssembly);

                    if (removePendingAssemblies == null)
                        removePendingAssemblies = new List<ScriptAssembly>();

                    removePendingAssemblies.Add(pendingAssembly);
                    compileAssembly = false;
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
                {
                    pendingAssemblies.Remove(assembly);

                    // If a codegen assembly was removed fro pending assemblies,
                    // clear all pending compilation and post processing.
                    if (codeGenAssemblies.Contains(assembly))
                    {
                        notCompiledCodeGenAssemblies.Add(assembly);
                        pendingPostProcessorTasks.Clear();
                        pendingAssemblies.Clear();
                        assemblyCompileQueue = null;
                        break;
                    }
                }

                // All pending assemblies were removed and no assemblies
                // were queued for compilation.
                if (assemblyCompileQueue == null)
                    return;
            }

            // No assemblies to compile, need to wait for more references to finish compiling.
            if (assemblyCompileQueue == null)
            {
                if (compilerTasks.Count == 0)
                    throw new Exception("No pending assemblies queued for compilation and no compilers running. Compilation will never finish.");
                return;
            }

            // Begin compiling any queued assemblies
            foreach (var assembly in assemblyCompileQueue)
            {
                pendingAssemblies.Remove(assembly);

                if (assembly.CallOnBeforeCompilationStarted && OnBeforeCompilationStarted != null)
                    OnBeforeCompilationStarted(assembly, compilePhase);

                var compiler = compilerFactory.Create(assembly, buildOutputDirectory);

                compilerTasks.Add(assembly, compiler);

                // Start compiler process
                compiler.BeginCompiling();

                LogStartInfo($"# Starting compiling {assembly.Filename}", compiler.GetProcessStartInfo());

                if (OnCompilationStarted != null)
                    OnCompilationStarted(assembly, compilePhase);

                if (RunningMaxConcurrentProcesses)
                    break;
            }

            compilePhase++;
        }

        void LogStartInfo(string message, ProcessStartInfo startInfo)
        {
            try
            {
                if (startInfo == null)
                {
                    logWriter.WriteLine(message);
                    logWriter.WriteLine($"Error: ProcessStartInfo is null");
                    logWriter.Flush();
                    return;
                }

                logWriter.WriteLine(message);
                logWriter.WriteLine($"{startInfo.FileName} {startInfo.Arguments}");
                logWriter.Flush();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Writing to {LogFilePath} falied\n{e}");
            }
        }
    }
}
