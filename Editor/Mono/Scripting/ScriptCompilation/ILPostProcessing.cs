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
    internal class ILPostProcessing
    {
        public ILPostProcessor[] ILPostProcessors { get; set; }

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
                        assemblyName = message.File,
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

        public static ILPostProcessor[] FindAllPostProcessors()
        {
            TypeCache.TypeCollection typesDerivedFrom = TypeCache.GetTypesDerivedFrom<ILPostProcessor>();
            ILPostProcessor[] localILPostProcessors = new ILPostProcessor[typesDerivedFrom.Count];

            for (int i = 0; i < typesDerivedFrom.Count; i++)
            {
                try
                {
                    localILPostProcessors[i] = (ILPostProcessor)Activator.CreateInstance(typesDerivedFrom[i]);
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Could not create ILPostProcessor ({typesDerivedFrom[i].FullName}):{Environment.NewLine}{exception.StackTrace}");
                }
            }

            return localILPostProcessors;
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
                var ilPostProcessResult = ilPostProcessorInstance.Process(ilPostProcessCompiledAssembly);
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
