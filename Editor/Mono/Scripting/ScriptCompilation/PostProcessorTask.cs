// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Scripting.Compilers;
using System.Threading;
using System.Collections.Generic;
using System;

namespace UnityEditor.Scripting.ScriptCompilation
{
    class PostProcessorTask
    {
        public ScriptAssembly Assembly { get; private set; }
        public List<CompilerMessage> CompilerMessages { get; private set; }
        string tempOutputDirectory;
        Func<ScriptAssembly, List<CompilerMessage>, string, List<CompilerMessage>> postProcessFunc;
        Thread postProcessingThread;

        public PostProcessorTask(ScriptAssembly assembly,
                                 List<CompilerMessage> compilerMessages,
                                 string tempOutputDirectory,
                                 Func<ScriptAssembly, List<CompilerMessage>, string, List<CompilerMessage>> postProcessFunc)
        {
            Assembly = assembly;
            CompilerMessages = compilerMessages;
            this.tempOutputDirectory = tempOutputDirectory;
            this.postProcessFunc = postProcessFunc;
        }

        public bool Poll()
        {
            if (postProcessingThread == null)
            {
                postProcessingThread = new Thread(() =>
                {
                    CompilerMessages = postProcessFunc(Assembly, CompilerMessages, tempOutputDirectory);
                });

                postProcessingThread.Start();
            }

            return !postProcessingThread.IsAlive;
        }
    }
}
