// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.Compilation
{
    public enum AssemblyBuilderStatus
    {
        NotStarted = 0,
        IsCompiling = 1,
        Finished = 2,
    }

    [Flags]
    public enum AssemblyBuilderFlags
    {
        None = 0,
        EditorAssembly = 1,
        DevelopmentBuild = 2,
    };

    public class AssemblyBuilder
    {
        public event Action<string> buildStarted;
        public event Action<string, CompilerMessage[]> buildFinished;

        public string[] scriptPaths { get; private set; }
        public string assemblyPath { get; private set; }
        public string[] additionalDefines { get; set; }
        public string[] additionalReferences { get; set; }
        public string[] excludeReferences { get; set; }

        public AssemblyBuilderFlags flags { get; set; }
        public BuildTargetGroup buildTargetGroup { get; set; }
        public BuildTarget buildTarget { get; set; }

        CompilationTask compilationTask;

        public AssemblyBuilder(string assemblyPath, params string[] scriptPaths)
        {
            if (string.IsNullOrEmpty(assemblyPath))
                throw new ArgumentException("assemblyPath cannot be null or empty");

            if (scriptPaths == null || scriptPaths.Length == 0)
                throw new ArgumentException("scriptPaths cannot be null or empty");

            this.scriptPaths = scriptPaths;
            this.assemblyPath = assemblyPath;

            flags = AssemblyBuilderFlags.None;
            buildTargetGroup = EditorUserBuildSettings.activeBuildTargetGroup;
            buildTarget = EditorUserBuildSettings.activeBuildTarget;
        }

        public bool Build()
        {
            return Build(EditorCompilationInterface.Instance);
        }

        internal bool Build(EditorCompilation editorCompilation)
        {
            if (editorCompilation.IsCompilationTaskCompiling())
                return false;

            if (status != AssemblyBuilderStatus.NotStarted)
                throw new Exception(string.Format("Cannot start AssemblyBuilder with status {0}. Expected {1}", status, AssemblyBuilderStatus.NotStarted));

            var scriptAssembly = editorCompilation.CreateScriptAssembly(this);

            compilationTask = new CompilationTask(new ScriptAssembly[] { scriptAssembly }, scriptAssembly.OutputDirectory,
                    EditorScriptCompilationOptions.BuildingEmpty, 1);

            compilationTask.OnCompilationStarted += OnCompilationStarted;
            compilationTask.OnCompilationFinished += OnCompilationFinished;
            compilationTask.Poll();

            editorCompilation.AddAssemblyBuilder(this);

            return true;
        }

        public AssemblyBuilderStatus status
        {
            get
            {
                if (compilationTask == null)
                    return AssemblyBuilderStatus.NotStarted;

                if (compilationTask.IsCompiling)
                    return compilationTask.Poll() ? AssemblyBuilderStatus.Finished : AssemblyBuilderStatus.IsCompiling;

                return AssemblyBuilderStatus.Finished;
            }
        }

        CompilerMessage[] ConvertCompilerMessages(List<UnityEditor.Scripting.Compilers.CompilerMessage> messages)
        {
            var newMessages = new CompilerMessage[messages.Count];

            int index = 0;
            foreach (var message in messages)
            {
                var newMessage = new CompilerMessage();

                newMessage.message = message.message;
                newMessage.file = message.file;
                newMessage.line = message.line;
                newMessage.column = message.column;

                switch (message.type)
                {
                    case UnityEditor.Scripting.Compilers.CompilerMessageType.Error:
                        newMessage.type = CompilerMessageType.Error;
                        break;

                    case UnityEditor.Scripting.Compilers.CompilerMessageType.Warning:
                        newMessage.type = CompilerMessageType.Warning;
                        break;
                }

                newMessages[index++] = newMessage;
            }

            return newMessages;
        }

        void OnCompilationStarted(ScriptAssembly assembly, int phase)
        {
            if (buildStarted == null)
                return;

            try
            {
                buildStarted(assemblyPath);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        void OnCompilationFinished(ScriptAssembly assembly, List<UnityEditor.Scripting.Compilers.CompilerMessage> messages)
        {
            if (buildFinished == null)
                return;

            var convertedMessages = ConvertCompilerMessages(messages);

            try
            {
                buildFinished(assemblyPath, convertedMessages);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }
    }
}
