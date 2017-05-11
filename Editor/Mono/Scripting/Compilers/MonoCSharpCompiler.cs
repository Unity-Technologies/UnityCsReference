// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor.Utils;
using System.Text;

namespace UnityEditor.Scripting.Compilers
{
    class MonoCSharpCompiler : MonoScriptCompilerBase
    {
        public static readonly string ReponseFilename = "mcs.rsp";

        public MonoCSharpCompiler(MonoIsland island, bool runUpdater) : base(island, runUpdater)
        {
        }

        override protected Program StartCompiler()
        {
            var arguments = new List<string>
            {
                "-debug",
                "-target:library",
                "-nowarn:0169",
                "-langversion:" + ((EditorApplication.scriptingRuntimeVersion == ScriptingRuntimeVersion.Latest) ? "6" : "4"),
                "-out:" + PrepareFileName(_island._output),
                "-unsafe"
            };

            if (!_island._development_player && !_island._editor)
                arguments.Add("-optimize");

            foreach (string dll in _island._references)
                arguments.Add("-r:" + PrepareFileName(dll));
            foreach (string define in _island._defines.Distinct())
                arguments.Add("-define:" + define);
            foreach (string source in _island._files)
                arguments.Add(PrepareFileName(source));

            // For .NET 2.0 profile, the new mcs.exe references class libraries out of 2.0-api folder (even though we run against 2.0 at runtime)
            string profileForReferences = _island._api_compatibility_level == ApiCompatibilityLevel.NET_2_0 ? "2.0-api" : GetMonoProfileLibDirectory();
            var referencesDirectory = MonoInstallationFinder.GetProfileDirectory(profileForReferences, MonoInstallationFinder.MonoBleedingEdgeInstallation);

            // If additional references are not used in C# files, they won't be added to final package
            foreach (string reference in GetAdditionalReferences())
            {
                string path = Path.Combine(referencesDirectory, reference);
                if (File.Exists(path)) arguments.Add("-r:" + PrepareFileName(path));
            }

            if (!AddCustomResponseFileIfPresent(arguments, ReponseFilename))
            {
                if (_island._api_compatibility_level == ApiCompatibilityLevel.NET_2_0_Subset && AddCustomResponseFileIfPresent(arguments, "smcs.rsp"))
                    Debug.LogWarning(string.Format("Using obsolete custom response file 'smcs.rsp'. Please use '{0}' instead.", ReponseFilename));
                else if (_island._api_compatibility_level == ApiCompatibilityLevel.NET_2_0 && AddCustomResponseFileIfPresent(arguments, "gmcs.rsp"))
                    Debug.LogWarning(string.Format("Using obsolete custom response file 'gmcs.rsp'. Please use '{0}' instead.", ReponseFilename));
            }
            return StartCompiler(_island._target, GetCompilerPath(arguments), arguments, false, MonoInstallationFinder.GetMonoInstallation(MonoInstallationFinder.MonoBleedingEdgeInstallation));
        }

        private string[] GetAdditionalReferences()
        {
            // Any references added here should also be added to
            // UnityEditor.VisualStudioIntegration.DefaultSolutionSynchronizationSettings.GetProjectHeaderTemplate
            return new[]
            {
                "System.Runtime.Serialization.dll",
                "System.Xml.Linq.dll",
                "UnityScript.dll",
                "UnityScript.Lang.dll",
                "Boo.Lang.dll",
            };
        }

        private string GetCompilerPath(List<string> arguments)
        {
            string dir = MonoInstallationFinder.GetProfileDirectory("4.5", MonoInstallationFinder.MonoBleedingEdgeInstallation);
            var compilerPath = Path.Combine(dir, "mcs.exe");
            if (File.Exists(compilerPath))
            {
                // While the folder for 4.6 profile is "4.5", mcs still expects -sdk argument to say 4.6.
                var sdkArgument = _island._api_compatibility_level == ApiCompatibilityLevel.NET_4_6 ? "4.6" : BuildPipeline.CompatibilityProfileToClassLibFolder(_island._api_compatibility_level);
                arguments.Add("-sdk:" + sdkArgument);
                return compilerPath;
            }

            throw new ApplicationException("Unable to find csharp compiler in " + dir);
        }

        protected override CompilerOutputParserBase CreateOutputParser()
        {
            return new MonoCSharpCompilerOutputParser();
        }

        public static string[] Compile(string[] sources, string[] references, string[] defines, string outputFile)
        {
            var island = new MonoIsland(BuildTarget.StandaloneWindows, ApiCompatibilityLevel.NET_2_0_Subset, sources, references, defines, outputFile);
            using (var c = new MonoCSharpCompiler(island, false))
            {
                c.BeginCompiling();
                while (!c.Poll())
                    System.Threading.Thread.Sleep(50);
                return c.GetCompilerMessages().Select(cm => cm.message).ToArray();
            }
        }
    }
}
