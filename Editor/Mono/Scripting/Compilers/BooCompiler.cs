// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Utils;

namespace UnityEditor.Scripting.Compilers
{
    class BooCompiler : MonoScriptCompilerBase
    {
        const string k_UnityScriptProfileDirectory = "unityscript";

        public BooCompiler(MonoIsland island, bool runUpdater) : base(island, runUpdater)
        {
        }

        override protected Program StartCompiler()
        {
            var arguments = new List<string>
            {
                "-debug",
                "-target:library",
                "-out:" + m_Island._output,
                "-x-type-inference-rule-attribute:" + typeof(UnityEngineInternal.TypeInferenceRuleAttribute)
            };

            foreach (string dll in m_Island._references)
                arguments.Add("-r:" + PrepareFileName(dll));
            foreach (string define in m_Island._defines.Distinct())
                arguments.Add("-define:" + define);
            foreach (string source in m_Island._files)
                arguments.Add(PrepareFileName(source));

            string compilerPath = Path.Combine(GetBooCompilerDirectory(), "booc.exe");
            return StartCompiler(m_Island._target, compilerPath, arguments, GetBooProfileDirectory());
        }

        protected override CompilerOutputParserBase CreateOutputParser()
        {
            return new BooCompilerOutputParser();
        }

        protected override string[] GetSystemReferenceDirectories()
        {
            return new[] { GetBooCompilerDirectory() };
        }

        string GetBooCompilerDirectory()
        {
            if (PlayerSettings.scriptingRuntimeVersion == ScriptingRuntimeVersion.Legacy)
                return GetMonoProfileLibDirectory();

            return Path.Combine(MonoInstallationFinder.GetProfilesDirectory(MonoInstallationFinder.MonoBleedingEdgeInstallation), k_UnityScriptProfileDirectory);
        }

        string GetBooProfileDirectory()
        {
            if (PlayerSettings.scriptingRuntimeVersion == ScriptingRuntimeVersion.Legacy)
                return BuildPipeline.CompatibilityProfileToClassLibFolder(m_Island._api_compatibility_level);

            return k_UnityScriptProfileDirectory;
        }
    }
}
