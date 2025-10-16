// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                return m_Messages.All(message => message.Type != CompilerMessageType.Error);
            }
        }

        public Action<AssemblyCompilationResult> OnCompilationFinished;

#pragma warning disable 618 // disable warning for obsolete AssemblyBuilder
        public AssemblyCompilationTask(AssemblyBuilder assemblyBuilder)
        {
            m_Builder = assemblyBuilder;
            m_Builder.buildFinished += OnAssemblyBuilderFinished;
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
                    if (m_Dependencies.All(dep => dep.IsCompleted))
                    {
                        if (m_Dependencies.All(dep => dep.IsCompletedSuccessfully))
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
                                DependentAssemblyNames = m_Dependencies.Select(d => d.m_AssemblyName).ToArray(),
                                DurationInMs = 0,
                                Messages = m_Messages,
                                Status = m_CompilationStatus
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
                        DependentAssemblyNames = m_Dependencies.Select(d => d.m_AssemblyName).ToArray(),
                        DurationInMs = m_StopWatch.ElapsedMilliseconds,
                        Messages = m_Messages,
                        Status = m_CompilationStatus
                    });
                    break;
            }
        }

        void OnAssemblyBuilderFinished(string path, UnityEditor.Compilation.CompilerMessage[] originalMessages)
        {
            m_StopWatch.Stop();

            m_Messages = new CompilerMessage[originalMessages.Length];
            for (int i = 0; i < originalMessages.Length; i++)
            {
                var messageStartIndex = originalMessages[i].message.LastIndexOf("):");
                if (messageStartIndex != -1)
                {
                    var messageWithCode = originalMessages[i].message.Substring(messageStartIndex + 2);
                    var messageParts = messageWithCode.Split(new[] {' ', ':'}, 2,
                        StringSplitOptions.RemoveEmptyEntries);
                    if (messageParts.Length < 2)
                        continue;

                    var messageType = messageParts[0];
                    if (messageParts[1].IndexOf(':') == -1)
                        continue;

                    messageParts = messageParts[1].Split(':');
                    if (messageParts.Length < 2)
                        continue;

                    var messageBody = messageWithCode.Substring(messageWithCode.IndexOf(": ", StringComparison.Ordinal) + 2);
                    m_Messages[i] = new CompilerMessage
                    {
                        Message = messageBody,
                        File = originalMessages[i].file,
                        Line = originalMessages[i].line,
                        Code = messageParts[0]
                    };

                    // disregard originalMessages[i].type because it does not support CompilerMessageType.Info in 2020.x
                    switch (messageType)
                    {
                        case "error":
                            m_Messages[i].Type = CompilerMessageType.Error;
                            break;
                        case "warning":
                            m_Messages[i].Type = CompilerMessageType.Warning;
                            break;
                        case "info":
                            m_Messages[i].Type = CompilerMessageType.Info;
                            break;
                    }
                }
                else
                {
                    // Copy messages that don't have the standard format. We can't extract a code string from these.
                    m_Messages[i] = new CompilerMessage
                    {
                        Message = originalMessages[i].message,
                        File = String.IsNullOrEmpty(originalMessages[i].file) ? ProjectAuditor.ProjectPath : originalMessages[i].file,
                        Line = originalMessages[i].line,
                        Code = "<Unity>"
                    };

                    switch (originalMessages[i].type)
                    {
                        case UnityEditor.Compilation.CompilerMessageType.Error:
                            m_Messages[i].Type = CompilerMessageType.Error;
                            break;
                        case UnityEditor.Compilation.CompilerMessageType.Warning:
                            m_Messages[i].Type = CompilerMessageType.Warning;
                            break;
                        case UnityEditor.Compilation.CompilerMessageType.Info:
                            m_Messages[i].Type = CompilerMessageType.Info;
                            break;
                    }
                }
            }
        }
    }
}
