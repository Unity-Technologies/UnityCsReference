// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Bee.BeeDriver;
using NiceIO;
using ScriptCompilationBuildProgram.Data;

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class UnityBeeDriver
    {
        public static BeeDriver Make(RunnableProgram buildProgram, EditorCompilation editorCompilation, string dagName, string dagDirectory = null, bool useScriptUpdater = true)
        {
            var sourceFileUpdaters = useScriptUpdater
                ? new[] {new UnityScriptUpdater(editorCompilation.projectDirectory)}
            : Array.Empty<SourceFileUpdaterBase>();

            var processSourceFileUpdatersResult = new UnitySourceFileUpdatersResultHandler(editorCompilation.projectDirectory);

            var result = new BeeDriver(buildProgram, editorCompilation.projectDirectory, dagName, dagDirectory, sourceFileUpdaters, processSourceFileUpdatersResult, new UnityProgressAPI(), UnityBeeBackendProgram());
            result.DataForBuildProgram.Add(new ConfigurationData
            {
                editorContentsPath = EditorApplication.applicationContentsPath,
            });
            return result;
        }

        private static RunnableProgram UnityBeeBackendProgram()
        {
            var executable = new NPath($"{EditorApplication.applicationContentsPath}/bee_backend{BeeScriptCompilation.ExecutableExtension}").ToString();
            return new SystemProcessRunnableProgram(executable, alwaysEnvironmentVariables: new Dictionary<string, string>() {{ "BEE_CACHE_BEHAVIOUR", "_"}});
        }
    }
}
