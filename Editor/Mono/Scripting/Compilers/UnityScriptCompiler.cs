// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor.Utils;

namespace UnityEditor.Scripting.Compilers
{
    class UnityScriptCompiler : MonoScriptCompilerBase
    {
        private static readonly Regex UnityEditorPattern = new Regex(@"UnityEditor\.dll$", RegexOptions.ExplicitCapture);
        const string k_UnityScriptProfileDirectory = "unityscript";

        public UnityScriptCompiler(MonoIsland island, bool runUpdater) : base(island, runUpdater)
        {
        }

        protected override CompilerOutputParserBase CreateOutputParser()
        {
            return new UnityScriptCompilerOutputParser();
        }

        protected override Program StartCompiler()
        {
            var arguments = new List<string>
            {
                "-debug",
                "-target:library",
                "-i:UnityEngine",
                "-i:System.Collections",
                "-base:UnityEngine.MonoBehaviour",
                "-nowarn:BCW0016",
                "-nowarn:BCW0003",
                "-method:Main",
                "-out:" + m_Island._output,
                "-x-type-inference-rule-attribute:" + typeof(UnityEngineInternal.TypeInferenceRuleAttribute)
            };

            if (StrictBuildTarget())
                arguments.Add("-pragmas:strict,downcast");

            foreach (var define in m_Island._defines.Distinct())
                arguments.Add("-define:" + define);

            foreach (var dll in m_Island._references)
                arguments.Add("-r:" + PrepareFileName(dll));

            var compilingEditorScripts = Array.Exists(m_Island._references, UnityEditorPattern.IsMatch);
            if (compilingEditorScripts)
                arguments.Add("-i:UnityEditor");
            else if (!BuildPipeline.IsUnityScriptEvalSupported(m_Island._target))
                arguments.Add($"-disable-eval:eval is not supported on the current build target ({m_Island._target}).");

            foreach (string source in m_Island._files)
                arguments.Add(PrepareFileName(source));

            var compilerPath = Path.Combine(GetUnityScriptCompilerDirectory(), "us.exe");
            return StartCompiler(m_Island._target, compilerPath, arguments, GetUnityScriptProfileDirectory());
        }

        string GetUnityScriptCompilerDirectory()
        {
            if (PlayerSettings.scriptingRuntimeVersion == ScriptingRuntimeVersion.Legacy)
                return GetMonoProfileLibDirectory();

            return Path.Combine(MonoInstallationFinder.GetProfilesDirectory(MonoInstallationFinder.MonoBleedingEdgeInstallation), k_UnityScriptProfileDirectory);
        }

        string GetUnityScriptProfileDirectory()
        {
            if (PlayerSettings.scriptingRuntimeVersion == ScriptingRuntimeVersion.Legacy)
                return BuildPipeline.CompatibilityProfileToClassLibFolder(m_Island._api_compatibility_level);

            return k_UnityScriptProfileDirectory;
        }

        private bool StrictBuildTarget()
        {
            return Array.IndexOf(m_Island._defines, "ENABLE_DUCK_TYPING") == -1;
        }

        protected override string[] GetSystemReferenceDirectories()
        {
            return new[] { GetUnityScriptCompilerDirectory() };
        }

        protected override string[] GetStreamContainingCompilerMessages()
        {
            return GetStandardOutput();
        }
    }
}
