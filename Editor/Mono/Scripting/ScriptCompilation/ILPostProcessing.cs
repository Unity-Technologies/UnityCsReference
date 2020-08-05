// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using UnityEditor.Scripting.Compilers;
using UnityEngine.Profiling;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal struct ILPostProcessorData
    {
        public ILPostProcessor postProcessor;
        public ScriptAssembly scriptAssembly;
    }

    internal interface IILPostProcessing
    {
        List<CompilerMessage> PostProcess(ScriptAssembly assembly, List<CompilerMessage> messages, string outputTempPath);
        bool IsAnyRunningPostProcessorUsingAssembly(ScriptAssembly assembly);
    }

    class ConcurrentPostProcessors
    {
        object lockObject = new object();
        HashSet<ILPostProcessorData> postProcessors = new HashSet<ILPostProcessorData>();

        public int Count
        {
            get
            {
                lock (lockObject)
                {
                    return postProcessors.Count;
                }
            }
        }

        public void Add(ILPostProcessorData postProcessor)
        {
            lock (lockObject)
            {
                postProcessors.Add(postProcessor);
            }
        }

        public void Remove(ILPostProcessorData postProcessor)
        {
            lock (lockObject)
            {
                postProcessors.Remove(postProcessor);
            }
        }

        public ILPostProcessorData[] ToArray()
        {
            lock (lockObject)
            {
                return postProcessors.ToArray();
            }
        }
    }

    internal class ILPostProcessing : IILPostProcessing
    {
        public ILPostProcessor[] ILPostProcessors { get; set; }
        public ConcurrentPostProcessors RunningPostProcessors { get;   }

        public ILPostProcessing()
        {
            RunningPostProcessors = new ConcurrentPostProcessors();
        }

        public bool HasPostProcessors
        {
            get
            {
                return ILPostProcessors != null && ILPostProcessors.Length > 0;
            }
        }

        public List<CompilerMessage> PostProcess(ScriptAssembly assembly, List<CompilerMessage> messages, string outputTempPath)
        {
            var hasCompileError = messages.Any(m => m.type == CompilerMessageType.Error);

            if (hasCompileError)
                return messages;

            if (UnityCodeGenHelpers.IsCodeGen(assembly.Filename))
                return messages;

            if (!HasPostProcessors)
                return messages;

            try
            {
                List<DiagnosticMessage> diagnostics = RunILPostProcessors(assembly, outputTempPath);
                foreach (var message in diagnostics)
                {
                    if (message.DiagnosticType == DiagnosticType.Error)
                    {
                        hasCompileError = true;
                    }
                    messages.Add(new CompilerMessage
                    {
                        file = message.File,
                        column = message.Column,
                        line = message.Line,
                        message = message.MessageData,
                        type = message.DiagnosticType == DiagnosticType.Error ? CompilerMessageType.Error : CompilerMessageType.Warning,
                    });
                }
            }
            catch (Exception exception)
            {
                messages.Add(new CompilerMessage
                {
                    assemblyName = assembly.Filename,
                    message = $"Something went wrong while Post Processing the assembly ({assembly.Filename}) : {Environment.NewLine} {exception.Message} {Environment.NewLine}{exception.StackTrace}",
                    type = CompilerMessageType.Error,
                });
            }

            return messages;
        }

        public bool IsAnyRunningPostProcessorUsingAssembly(ScriptAssembly assembly)
        {
            if (RunningPostProcessors == null ||
                RunningPostProcessors.Count == 0)
                return false;

            var runningPostProcessorsArray = RunningPostProcessors.ToArray();

            foreach (var postProcessor in runningPostProcessorsArray)
            {
                var postProcessorAssembly = postProcessor.scriptAssembly;

                if (postProcessorAssembly.ScriptAssemblyReferences.Contains(assembly))
                    return true;

                if (postProcessorAssembly.References.Any(r => AssetPath.GetFileName(r) == assembly.Filename))
                    return true;
            }

            return false;
        }

        public static ILPostProcessor[] FindAllPostProcessors()
        {
            TypeCache.TypeCollection typesDerivedFrom = TypeCache.GetTypesDerivedFrom<ILPostProcessor>();
            var localILPostProcessors = new List<ILPostProcessor>(typesDerivedFrom.Count);

            for (int i = 0; i < typesDerivedFrom.Count; i++)
            {
                try
                {
                    localILPostProcessors.Add((ILPostProcessor)Activator.CreateInstance(typesDerivedFrom[i]));
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Could not create ILPostProcessor ({typesDerivedFrom[i].FullName}):{Environment.NewLine}{exception.StackTrace}");
                }
            }

            // Default sort by type fullname
            localILPostProcessors.Sort((left, right) => string.Compare(left.GetType().FullName, right.GetType().FullName, StringComparison.Ordinal));

            return localILPostProcessors.ToArray();
        }

        List<DiagnosticMessage> RunILPostProcessors(ScriptAssembly assembly, string outputTempPath)
        {
            Profiler.BeginSample("CompilationPipeline.RunILPostProcessors");
            var assemblyPath = Path.Combine(outputTempPath, assembly.Filename);

            var resultMessages = new List<DiagnosticMessage>();

            if (!File.Exists(assemblyPath))
            {
                resultMessages.Add(new DiagnosticMessage
                {
                    File = assemblyPath,
                    MessageData = $"Could not find {assemblyPath} for post processing",
                    DiagnosticType = DiagnosticType.Error,
                });
            }

            bool isILProcessed = false;
            var ilPostProcessCompiledAssembly = new ILPostProcessCompiledAssembly(assembly, outputTempPath);

            InMemoryAssembly postProcessedInMemoryAssembly = null;
            foreach (var ilPostProcessor in ILPostProcessors)
            {
                Profiler.BeginSample($"{ilPostProcessor.GetType().FullName}.Process({assembly.Filename})");

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                Console.WriteLine($"  - Starting ILPostProcessor '{ilPostProcessor.GetType().FullName}' on {assembly.Filename}");

                var ilPostProcessorInstance = ilPostProcessor.GetInstance();

                var ilPostProcessorData = new ILPostProcessorData { postProcessor = ilPostProcessorInstance, scriptAssembly = assembly };
                RunningPostProcessors.Add(ilPostProcessorData);

                ILPostProcessResult ilPostProcessResult;
                try
                {
                    ilPostProcessResult = ilPostProcessorInstance.Process(ilPostProcessCompiledAssembly);
                }
                finally
                {
                    RunningPostProcessors.Remove(ilPostProcessorData);
                }

                stopwatch.Stop();

                Profiler.EndSample();

                var elapsed = stopwatch.Elapsed;

                Console.WriteLine($"  - Finished ILPostProcessor '{ilPostProcessor.GetType().FullName}' on {assembly.Filename} in {elapsed.TotalSeconds:0.######} seconds");
                postProcessedInMemoryAssembly = ilPostProcessResult?.InMemoryAssembly;

                if (ilPostProcessResult?.InMemoryAssembly != null)
                {
                    isILProcessed = true;
                    ilPostProcessCompiledAssembly.InMemoryAssembly = postProcessedInMemoryAssembly;
                }

                if (ilPostProcessResult?.Diagnostics != null)
                {
                    resultMessages.AddRange(ilPostProcessResult.Diagnostics);
                }
            }

            if (isILProcessed)
            {
                ilPostProcessCompiledAssembly.WriteAssembly();
            }

            Profiler.EndSample();

            return resultMessages;
        }
    }
}
