// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.Modules;
using UnityEditor.Utils;

namespace UnityEditor.Scripting.Compilers
{
    internal class MicrosoftCSharpCompiler : ScriptCompilerBase
    {
        public MicrosoftCSharpCompiler(MonoIsland island, bool runUpdater) : base(island, runUpdater)
        {
        }

        private BuildTarget BuildTarget { get { return _island._target; } }

        private static string[] GetReferencesFromMonoDistribution()
        {
            return new[]
            {
                "mscorlib.dll",
                "System.dll",
                "System.Core.dll",
                "System.Runtime.Serialization.dll",
                "System.Xml.dll",
                "System.Xml.Linq.dll",
                "UnityScript.dll",
                "UnityScript.Lang.dll",
                "Boo.Lang.dll",
            };
        }

        private string[] GetClassLibraries()
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(BuildTarget);
            if (PlayerSettings.GetScriptingBackend(buildTargetGroup) != ScriptingImplementation.WinRTDotNET)
            {
                var monoAssemblyDirectory = GetMonoProfileLibDirectory();
                var classLibraries = new List<string>();
                classLibraries.AddRange(GetReferencesFromMonoDistribution().Select(dll => Path.Combine(monoAssemblyDirectory, dll)));

                if (PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup) == ApiCompatibilityLevel.NET_4_6)
                {
                    // Assemblies compiled against .NET 4.6 in Visual Studio might reference facade DLLs. Therefore we
                    // need to reference them too, as otherwise compiler will complain about them being missing.
                    var facadesDirectory = Path.Combine(monoAssemblyDirectory, "Facades");
                    classLibraries.AddRange(Directory.GetFiles(facadesDirectory, "*.dll"));
                }

                return classLibraries.ToArray();
            }

            if (BuildTarget != BuildTarget.WSAPlayer)
                throw new InvalidOperationException(string.Format("MicrosoftCSharpCompiler cannot build for .NET Scripting backend for BuildTarget.{0}.", BuildTarget));

            var resolver = new NuGetPackageResolver { ProjectLockFile = @"UWP\project.lock.json" };
            return resolver.Resolve();
        }

        private void FillCompilerOptions(List<string> arguments, out string argsPrefix)
        {
            // This will ensure that csc.exe won't include csc.rsp
            // csc.rsp references .NET 4.5 assemblies which cause conflicts for us
            argsPrefix = "/noconfig ";
            arguments.Add("/nostdlib+");

            // Case 755238: Always use english for outputing errors, the same way as Mono compilers do
            arguments.Add("/preferreduilang:en-US");

            var platformSupportModule = ModuleManager.FindPlatformSupportModule(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget));
            var compilationExtension = platformSupportModule.CreateCompilationExtension();

            arguments.AddRange(GetClassLibraries().Select(r => "/reference:\"" + r + "\""));
            arguments.AddRange(compilationExtension.GetAdditionalAssemblyReferences().Select(r => "/reference:\"" + r + "\""));
            arguments.AddRange(compilationExtension.GetWindowsMetadataReferences().Select(r => "/reference:\"" + r + "\""));
            arguments.AddRange(compilationExtension.GetAdditionalDefines().Select(d => "/define:" + d));
            arguments.AddRange(compilationExtension.GetAdditionalSourceFiles());
        }

        private static void ThrowCompilerNotFoundException(string path)
        {
            throw new Exception(string.Format("'{0}' not found. Is your Unity installation corrupted?", path));
        }

        private Program StartCompilerImpl(List<string> arguments, string argsPrefix)
        {
            foreach (string dll in _island._references)
                arguments.Add("/reference:" + PrepareFileName(dll));

            foreach (string define in _island._defines.Distinct())
                arguments.Add("/define:" + define);

            foreach (string source in _island._files)
            {
                arguments.Add(PrepareFileName(source).Replace('/', '\\'));
            }

            var coreRun = Paths.Combine(EditorApplication.applicationContentsPath, "Tools", "Roslyn", "CoreRun.exe").Replace('/', '\\');
            var csc = Paths.Combine(EditorApplication.applicationContentsPath, "Tools", "Roslyn", "csc.exe").Replace('/', '\\');

            if (!File.Exists(coreRun))
                ThrowCompilerNotFoundException(coreRun);

            if (!File.Exists(csc))
                ThrowCompilerNotFoundException(csc);

            AddCustomResponseFileIfPresent(arguments, "csc.rsp");

            var responseFile = CommandLineFormatter.GenerateResponseFile(arguments);

            RunAPIUpdaterIfRequired(responseFile);

            var psi = new ProcessStartInfo() { Arguments = "\"" + csc + "\" " + argsPrefix + "@" + responseFile, FileName = coreRun, CreateNoWindow = true };
            var program = new Program(psi);
            program.Start();
            return program;
        }

        protected override Program StartCompiler()
        {
            var outputPath = PrepareFileName(_island._output);

            // Always build with "/debug:pdbonly", "/optimize+", because even if the assembly is optimized
            // it seems you can still succesfully debug C# scripts in Visual Studio
            var arguments = new List<string>
            {
                "/target:library",
                "/nowarn:0169",
                "/unsafe",
                "/out:" + outputPath
            };

            if (!_island._development_player)
            {
                arguments.Add("/debug:pdbonly");
                arguments.Add("/optimize+");
            }
            else
            {
                arguments.Add("/debug:full");
                arguments.Add("/optimize-");
            }

            string argsPrefix;
            FillCompilerOptions(arguments, out argsPrefix);
            return StartCompilerImpl(arguments, argsPrefix);
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
