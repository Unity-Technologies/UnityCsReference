// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.Modules;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditor.Utils;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnityEditor.Scripting.Compilers
{
    internal class MicrosoftCSharpCompiler : ScriptCompilerBase
    {
        public static readonly string ReponseFilename = "csc.rsp";

        public MicrosoftCSharpCompiler(MonoIsland island, bool runUpdater) : base(island, runUpdater)
        {
        }

        BuildTarget BuildTarget => m_Island._target;

        private void FillCompilerOptions(List<string> arguments, out string argsPrefix)
        {
            // This will ensure that csc.exe won't include csc.rsp
            // csc.rsp references .NET 4.5 assemblies which cause conflicts for us
            argsPrefix = "/noconfig ";
            arguments.Add("/nostdlib+");

            // Case 755238: Always use english for outputing errors, the same way as Mono compilers do
            arguments.Add("/preferreduilang:en-US");
            arguments.Add("/langversion:latest");
        }

        private static void ThrowCompilerNotFoundException(string path)
        {
            throw new Exception(string.Format("'{0}' not found. Is your Unity installation corrupted?", path));
        }

        private Program StartCompilerImpl(List<string> arguments, string argsPrefix)
        {
            foreach (string dll in m_Island._references)
                arguments.Add("/reference:" + PrepareFileName(dll));

            foreach (string define in m_Island._defines.Distinct())
                arguments.Add("/define:" + define);

            var filePathMappings = new List<string>(m_Island._files.Length);
            foreach (var source in m_Island._files)
            {
                var f = PrepareFileName(source);
                f = Paths.UnifyDirectorySeparator(f);
                arguments.Add(f);

                if (f != source)
                    filePathMappings.Add(f + " => " + source);
            }

            var csc = Paths.Combine(EditorApplication.applicationContentsPath, "Tools", "RoslynScripts", "unity_csc");
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                csc += ".bat";
            }
            else
            {
                csc += ".sh";
            }

            csc = Paths.UnifyDirectorySeparator(csc);

            if (!File.Exists(csc))
                ThrowCompilerNotFoundException(csc);

            var responseFiles = m_Island._responseFiles?.ToDictionary(Path.GetFileName) ?? new Dictionary<string, string>();

            KeyValuePair<string, string> obsoleteResponseFile = responseFiles
                .SingleOrDefault(x => CompilerSpecificResponseFiles.MicrosoftCSharpCompilerObsolete.Contains(x.Key));
            if (!string.IsNullOrEmpty(obsoleteResponseFile.Key))
            {
                Debug.LogWarning($"Using obsolete custom response file '{obsoleteResponseFile.Key}'. Please use '{CompilerSpecificResponseFiles.MicrosoftCSharpCompiler}' instead.");
            }

            foreach (var file in responseFiles)
            {
                AddResponseFileToArguments(arguments, file.Value);
            }

            var responseFile = CommandLineFormatter.GenerateResponseFile(arguments);

            RunAPIUpdaterIfRequired(responseFile, filePathMappings);

            var psi = new ProcessStartInfo() { Arguments = argsPrefix + "@" + responseFile, FileName = csc, CreateNoWindow = true };
            var program = new Program(psi);
            program.Start();

            return program;
        }

        protected override Program StartCompiler()
        {
            var outputPath = PrepareFileName(m_Island._output);

            // Always build with "/debug:pdbonly", "/optimize+", because even if the assembly is optimized
            // it seems you can still succesfully debug C# scripts in Visual Studio
            var arguments = new List<string>
            {
                "/target:library",
                "/nowarn:0169",
                "/out:" + outputPath
            };

            if (m_Island._allowUnsafeCode)
                arguments.Add("/unsafe");

            arguments.Add("/debug:portable");

            var disableOptimizations = m_Island._development_player || (m_Island._editor && EditorPrefs.GetBool("AllowAttachedDebuggingOfEditor", true));
            if (!disableOptimizations)
            {
                arguments.Add("/optimize+");
            }
            else
            {
                arguments.Add("/optimize-");
            }

            string argsPrefix;
            FillCompilerOptions(arguments, out argsPrefix);
            return StartCompilerImpl(arguments, argsPrefix);
        }

        protected override string[] GetSystemReferenceDirectories()
        {
            return MonoLibraryHelpers.GetSystemReferenceDirectories(m_Island._api_compatibility_level);
        }

        protected override string[] GetStreamContainingCompilerMessages()
        {
            return GetStandardOutput();
        }

        protected override CompilerOutputParserBase CreateOutputParser()
        {
            return new MicrosoftCSharpCompilerOutputParser();
        }
    }
}
