// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEditor.Compilation;

namespace Unity.ProjectAuditor.Editor.AssemblyUtils
{
    class AssemblyCompilationResult
    {
        public string AssemblyName;
        public string AssemblyPath;
        public string[] DependentAssemblyNames;
        public long DurationInMs;
        public CompilerMessage[] Messages;
        public CompilationStatus Status;
        public bool EditorAssembly;
    }

    class AssemblyCompilationTask
    {
        string m_AssemblyName => Path.GetFileNameWithoutExtension(m_Builder.assemblyPath);

#pragma warning disable 618 // disable warning for obsolete AssemblyBuilder
        readonly AssemblyBuilder m_Builder;
#pragma warning restore 618

        CompilationStatus m_CompilationStatus = CompilationStatus.NotStarted;
        AssemblyCompilationTask[] m_Dependencies;
        CompilerMessage[] m_Messages = Array.Empty<CompilerMessage>();
        Stopwatch m_StopWatch;

        private bool m_EditorAssembly;
        private CodeAnalysisFlags m_CodeAnalysisFlags;
        private CodeOwnerFlags m_CodeOwnerFlags;

        public string AssemblyPath => m_Builder.assemblyPath;

        public bool IsCompleted
        {
            get
            {
                switch (m_CompilationStatus)
                {
                    case CompilationStatus.Compiled:
                    case CompilationStatus.MissingDependency:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool IsCompletedSuccessfully
        {
            get
            {
                if (m_CompilationStatus != CompilationStatus.Compiled)
                    return false;
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                return m_Messages.All(message => message.Type != CompilerMessageType.Error);
#pragma warning restore UA2001
            }
        }

        public Action<AssemblyCompilationResult> OnCompilationFinished;

#pragma warning disable 618 // disable warning for obsolete AssemblyBuilder
        public AssemblyCompilationTask(AssemblyBuilder assemblyBuilder, bool editorAssembly, CodeAnalysisFlags codeAnalysisFlags, CodeOwnerFlags codeOwnerFlags)
        {
            m_Builder = assemblyBuilder;
            m_Builder.buildFinished += OnAssemblyBuilderFinished;
            m_EditorAssembly = editorAssembly;
            m_CodeAnalysisFlags = codeAnalysisFlags;
            m_CodeOwnerFlags = codeOwnerFlags;
        }

#pragma warning restore 618

        public void AddDependencies(AssemblyCompilationTask[] dependencies)
        {
            m_Dependencies = dependencies;
        }

        public void Update()
        {
            switch (m_Builder.status)
            {
                case AssemblyBuilderStatus.NotStarted:
                    // check if all dependencies are built
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    if (m_Dependencies.All(dep => dep.IsCompleted))
#pragma warning restore UA2001
                    {
                        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        if (m_Dependencies.All(dep => dep.IsCompletedSuccessfully))
#pragma warning restore UA2001
                        {
                            m_StopWatch = Stopwatch.StartNew();
                            m_Builder.Build(); // all references are built, we can kick off this builder
                        }
                        else
                        {
                            // this assembly won't be built since it's missing dependencies
                            m_CompilationStatus = CompilationStatus.MissingDependency;

                            OnCompilationFinished(new AssemblyCompilationResult
                            {
                                AssemblyName = m_AssemblyName,
                                AssemblyPath = AssemblyPath,
                                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                                DependentAssemblyNames = m_Dependencies.Select(d => d.m_AssemblyName).ToArray(),
#pragma warning restore UA2001
                                DurationInMs = 0,
                                Messages = m_Messages,
                                Status = m_CompilationStatus,
                                EditorAssembly = m_EditorAssembly
                            });
                        }
                    }
                    break;
                case AssemblyBuilderStatus.IsCompiling:
                    m_CompilationStatus = CompilationStatus.IsCompiling;
                    break;
                case AssemblyBuilderStatus.Finished:
                    m_CompilationStatus = CompilationStatus.Compiled;
                    OnCompilationFinished(new AssemblyCompilationResult
                    {
                        AssemblyName = m_AssemblyName,
                        AssemblyPath = AssemblyPath,
                        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        DependentAssemblyNames = m_Dependencies.Select(d => d.m_AssemblyName).ToArray(),
#pragma warning restore UA2001
                        DurationInMs = m_StopWatch.ElapsedMilliseconds,
                        Messages = m_Messages,
                        Status = m_CompilationStatus,
                        EditorAssembly = m_EditorAssembly
                    });
                    break;
            }
        }

        void OnAssemblyBuilderFinished(string path, UnityEditor.Compilation.CompilerMessage[] originalMessages)
        {
            m_StopWatch.Stop();

            var results = new List<CompilerMessage>(originalMessages.Length);
            foreach (var msg in originalMessages)
            {
                var unityMsg = UnityCompilerMessageToProjectAuditorCompilerMessage(msg);
                if (CodeModule.PathPackageFilter(unityMsg.File, m_CodeAnalysisFlags, m_CodeOwnerFlags))
                    results.Add(unityMsg);
            }

            m_Messages = results.ToArray();
        }

        internal static CompilerMessage UnityCompilerMessageToProjectAuditorCompilerMessage(UnityEditor.Compilation.CompilerMessage originalMessage)
        {
            var messageStartIndex = originalMessage.message.LastIndexOf("):");
            if (messageStartIndex != -1)
            {
                var messageWithCode = originalMessage.message.Substring(messageStartIndex + 2);
                var messageParts = messageWithCode.Split(new[] { ' ', ':' }, 2,
                    StringSplitOptions.RemoveEmptyEntries);
                if (messageParts.Length < 2)
                    return DefaultCompilerMessage(ref originalMessage);

                var messageType = messageParts[0];
                if (messageParts[1].IndexOf(':') == -1)
                    return DefaultCompilerMessage(ref originalMessage);

                messageParts = messageParts[1].Split(':');
                if (messageParts.Length < 2)
                    return DefaultCompilerMessage(ref originalMessage);

                var messageBody = messageWithCode.Substring(messageWithCode.IndexOf(": ", StringComparison.Ordinal) + 2);
                var result = new CompilerMessage
                {
                    Message = messageBody,
                    File = originalMessage.file,
                    Line = originalMessage.line,
                    Code = messageParts[0]
                };

                // disregard originalMessages[i].type because it does not support CompilerMessageType.Info in 2020.x
                switch (messageType)
                {
                    case "error":
                        result.Type = CompilerMessageType.Error;
                        break;
                    case "warning":
                        result.Type = CompilerMessageType.Warning;
                        break;
                    case "info":
                        result.Type = CompilerMessageType.Info;
                        break;
                    default:
                        result.Type = originalMessage.type;
                        break;
                }

                return result;
            }

            return DefaultCompilerMessage(ref originalMessage);
        }

        private static CompilerMessage DefaultCompilerMessage(ref UnityEditor.Compilation.CompilerMessage originalMessage)
        {
            // Copy messages that don't have the standard format. We can't extract a code string from these.
            return new CompilerMessage
            {
                Message = originalMessage.message,
                File = string.IsNullOrEmpty(originalMessage.file) ? ProjectAuditor.ProjectPath : originalMessage.file,
                Line = originalMessage.line,
                Code = "<Unity>",
                Type = originalMessage.type
            };
        }
    }
}
