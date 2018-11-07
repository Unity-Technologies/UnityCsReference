// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor.Utils;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.Scripting.Compilers
{
    class MonoCSharpCompiler : MonoScriptCompilerBase
    {
        public static readonly string ResponseFilename = "mcs.rsp";

        public MonoCSharpCompiler(MonoIsland island, bool runUpdater) : base(island, runUpdater)
        {
        }

        protected override Program StartCompiler()
        {
            var arguments = new List<string>
            {
                "-debug",
                "-target:library",
                "-nowarn:0169",
                "-langversion:" + ((EditorApplication.scriptingRuntimeVersion == ScriptingRuntimeVersion.Latest) ? "6" : "4"),
                "-out:" + PrepareFileName(m_Island._output),
                "-nostdlib",
            };

            if (m_Island._allowUnsafeCode)
                arguments.Add("-unsafe");

            if (!m_Island._development_player && !m_Island._editor)
                arguments.Add("-optimize");

            foreach (string dll in m_Island._references)
                arguments.Add("-r:" + PrepareFileName(dll));
            foreach (string define in m_Island._defines.Distinct())
                arguments.Add("-define:" + define);

            var pathMappings = new List<string>(m_Island._files.Length);
            foreach (string source in m_Island._files)
            {
                var preparedFileName = PrepareFileName(source);
                if (preparedFileName != source)
                    pathMappings.Add(preparedFileName + " => " + source);

                arguments.Add(preparedFileName);
            }

            var responseFiles  = m_Island._responseFiles?.ToDictionary(Path.GetFileName) ?? new Dictionary<string, string>();
            KeyValuePair<string, string> obsoleteResponseFile = responseFiles
                .SingleOrDefault(x => CompilerSpecificResponseFiles.MonoCSharpCompilerObsolete.Contains(x.Key));
            if (!string.IsNullOrEmpty(obsoleteResponseFile.Key))
            {
                if (m_Island._api_compatibility_level == ApiCompatibilityLevel.NET_2_0_Subset)
                {
                    Debug.LogWarning($"Using obsolete custom response file '{obsoleteResponseFile.Key}'. Please use '{ResponseFilename}' instead.");
                }
                else
                {
                    responseFiles.Remove(obsoleteResponseFile.Key);
                }
            }

            foreach (var responseFile in responseFiles)
            {
                AddResponseFileToArguments(arguments, responseFile.Value);
            }

            return StartCompiler(
                m_Island._target,
                GetCompilerPath(),
                arguments,
                BuildPipeline.CompatibilityProfileToClassLibFolder(m_Island._api_compatibility_level),
                false,
                MonoInstallationFinder.GetMonoInstallation(MonoInstallationFinder.MonoBleedingEdgeInstallation),
                pathMappings
            );
        }

        static string GetCompilerPath()
        {
            string dir = MonoInstallationFinder.GetProfileDirectory("4.5", MonoInstallationFinder.MonoBleedingEdgeInstallation);
            var compilerPath = Path.Combine(dir, "mcs.exe");
            if (File.Exists(compilerPath))
            {
                return compilerPath;
            }

            throw new ApplicationException("Unable to find csharp compiler in " + dir);
        }

        protected override CompilerOutputParserBase CreateOutputParser()
        {
            return new MonoCSharpCompilerOutputParser();
        }

        protected override string[] GetSystemReferenceDirectories()
        {
            return MonoLibraryHelpers.GetSystemReferenceDirectories(m_Island._api_compatibility_level);
        }

        public static string[] Compile(string[] sources, string[] references, string[] defines, string outputFile, bool allowUnsafeCode)
        {
            var island = new MonoIsland(BuildTarget.StandaloneWindows, ApiCompatibilityLevel.NET_2_0_Subset, allowUnsafeCode, sources, references, defines, outputFile);
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
