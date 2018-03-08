// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using UnityEditor.Utils;

namespace UnityEditor.Scripting.Compilers
{
    internal abstract class MonoScriptCompilerBase : ScriptCompilerBase
    {
        protected MonoScriptCompilerBase(MonoIsland island, bool runUpdater) : base(island, runUpdater)
        {
        }

        protected ManagedProgram StartCompiler(BuildTarget target, string compiler, List<string> arguments)
        {
            return StartCompiler(target, compiler, arguments, BuildPipeline.CompatibilityProfileToClassLibFolder(_island._api_compatibility_level));
        }

        protected ManagedProgram StartCompiler(BuildTarget target, string compiler, List<string> arguments, string profileDirectory)
        {
            AddCustomResponseFileIfPresent(arguments, Path.GetFileNameWithoutExtension(compiler) + ".rsp");

            var monoInstallation = (PlayerSettingsEditor.IsLatestApiCompatibility(_island._api_compatibility_level))
                ? MonoInstallationFinder.GetMonoBleedingEdgeInstallation()
                : MonoInstallationFinder.GetMonoInstallation();
            return StartCompiler(target, compiler, arguments, profileDirectory, true, monoInstallation);
        }

        protected ManagedProgram StartCompiler(BuildTarget target, string compiler, List<string> arguments, string profileDirectory, bool setMonoEnvironmentVariables, string monodistro)
        {
            var responseFile = CommandLineFormatter.GenerateResponseFile(arguments);

            RunAPIUpdaterIfRequired(responseFile);

            var program = new ManagedProgram(monodistro, profileDirectory, compiler, " @" + responseFile, setMonoEnvironmentVariables, null);
            program.Start();

            return program;
        }
    }
}
