// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor.Utils;

namespace UnityEditor.Scripting.Compilers
{
    internal abstract class ScriptCompilerBase : IDisposable
    {
        private Program process;
        private string _responseFile = null;
        private bool _runAPIUpdater;

        // ToDo: would be nice to move MonoIsland to MonoScriptCompilerBase
        protected MonoIsland _island;

        protected abstract Program StartCompiler();

        protected abstract CompilerOutputParserBase CreateOutputParser();

        protected ScriptCompilerBase(MonoIsland island, bool runAPIUpdater)
        {
            _island = island;
            _runAPIUpdater = runAPIUpdater;
        }

        protected string[] GetErrorOutput()
        {
            return process.GetErrorOutput();
        }

        protected string[] GetStandardOutput()
        {
            return process.GetStandardOutput();
        }

        public void BeginCompiling()
        {
            if (process != null)
                throw new InvalidOperationException("Compilation has already begun!");
            process = StartCompiler();
        }

        public virtual void Dispose()
        {
            if (process != null)
            {
                process.Dispose();
                process = null;
            }
            if (_responseFile != null)
            {
                File.Delete(_responseFile);
                _responseFile = null;
            }
        }

        public virtual bool Poll()
        {
            if (process == null)
                return true;

            return process.HasExited;
        }

        public void WaitForCompilationToFinish()
        {
            process.WaitForExit();
        }

        protected string GetMonoProfileLibDirectory()
        {
            var profile = BuildPipeline.CompatibilityProfileToClassLibFolder(_island._api_compatibility_level);
            var monoInstall = _island._api_compatibility_level == ApiCompatibilityLevel.NET_4_6
                ? MonoInstallationFinder.MonoBleedingEdgeInstallation
                : MonoInstallationFinder.MonoInstallation;

            return MonoInstallationFinder.GetProfileDirectory(profile, monoInstall);
        }

        protected bool AddCustomResponseFileIfPresent(List<string> arguments, string responseFileName)
        {
            var relativeCustomResponseFilePath = Path.Combine("Assets", responseFileName);

            if (!File.Exists(relativeCustomResponseFilePath))
                return false;

            arguments.Add("@" + relativeCustomResponseFilePath);

            return true;
        }

        protected string GenerateResponseFile(List<string> arguments)
        {
            _responseFile = CommandLineFormatter.GenerateResponseFile(arguments);
            return _responseFile;
        }

        public static string[] GetResponseFileDefinesFromFile(string responseFileName)
        {
            var relativeCustomResponseFilePath = Path.Combine("Assets", responseFileName);

            if (!File.Exists(relativeCustomResponseFilePath))
                return new string[0];

            var responseFileText = File.ReadAllText(relativeCustomResponseFilePath);

            return GetResponseFileDefinesFromText(responseFileText);
        }

        public static string[] GetResponseFileDefinesFromText(string responseFileText)
        {
            const string defineString = "-define:";
            var defineStringLength = defineString.Length;

            if (!responseFileText.Contains(defineString))
                return new string[0];

            List<string> result = new List<string>();

            var textLines = responseFileText.Split(' ', '\n');

            foreach (var line in textLines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith(defineString))
                {
                    var definesSubString = trimmedLine.Substring(defineStringLength);
                    var defines = definesSubString.Split(',', ';');
                    result.AddRange(defines);
                }
            }

            return result.ToArray();
        }

        protected static string PrepareFileName(string fileName)
        {
            return CommandLineFormatter.PrepareFileName(fileName);
        }

        //do not change the returntype, native unity depends on this one.
        public virtual CompilerMessage[] GetCompilerMessages()
        {
            if (!Poll())
                Debug.LogWarning("Compile process is not finished yet. This should not happen.");

            DumpStreamOutputToLog();

            return CreateOutputParser().Parse(GetStreamContainingCompilerMessages(), CompilationHadFailure()).ToArray();
        }

        protected bool CompilationHadFailure()
        {
            return (process.ExitCode != 0);
        }

        protected virtual string[] GetStreamContainingCompilerMessages()
        {
            List<string> errors = new List<string>();
            errors.AddRange(GetErrorOutput());
            errors.Add(string.Empty);
            errors.AddRange(GetStandardOutput());
            return errors.ToArray();
        }

        private void DumpStreamOutputToLog()
        {
            bool hadCompilationFailure = CompilationHadFailure();

            string[] errorOutput = GetErrorOutput();

            if (hadCompilationFailure || errorOutput.Length != 0)
            {
                Console.WriteLine("");
                Console.WriteLine("-----Compiler Commandline Arguments:");
                process.LogProcessStartInfo();

                string[] stdOutput = GetStandardOutput();

                Console.WriteLine("-----CompilerOutput:-stdout--exitcode: " + process.ExitCode + "--compilationhadfailure: " + hadCompilationFailure + "--outfile: " + _island._output);
                foreach (string line in stdOutput)
                    Console.WriteLine(line);

                Console.WriteLine("-----CompilerOutput:-stderr----------");
                foreach (string line in errorOutput)
                    Console.WriteLine(line);
                Console.WriteLine("-----EndCompilerOutput---------------");
            }
        }

        protected void RunAPIUpdaterIfRequired(string responseFile)
        {
            if (!_runAPIUpdater)
                return;

            APIUpdaterHelper.UpdateScripts(responseFile, _island.GetExtensionOfSourceFiles());
        }
    }

    /// Normalized 'status' code for a [[CompilerMessage]]
    internal enum NormalizedCompilerStatusCode
    {
        NotNormalized = 0,

        /// Maps to C# CS0117 and Boo BCE0019.
        MemberNotFound = 1, // details syntax: TypeNamespaceQualifiedName:MemberName

        // Maps to C# CS0246/CS0234 and Boo XXXX
        UnknownTypeOrNamespace  // details syntax: typename or namespace.typename
    }

    internal struct NormalizedCompilerStatus
    {
        public NormalizedCompilerStatusCode code;

        /// each normalized compiler status defines the syntax of the details
        public string details;
    }

    /// Marks the type of a [[CompilerMessage]]
    internal enum CompilerMessageType
    {
        /// The message is an error. The compilation has failed.
        Error = 0,
        /// The message is an warning only. If there are no error messages, the compilation has completed successfully.
        Warning = 1
    }

    /// This struct should be returned from GetCompilerMessages() on ScriptCompilerBase implementations
    internal struct CompilerMessage
    {
        /// The text of the error or warning message
        public string message;
        /// The path name of the file the message refers to
        public string file;
        /// The line in the source file the message refers to
        public int line;
        /// The column of the line the message refers to
        public int column;
        /// The type of the message. Either Error or Warning
        public CompilerMessageType type;

        /// The normalized status. Each class deriving from ScriptCompilerBase must map errors / warning #
        /// if it can be mapped to a NormalizedCompilerStatusCode.
        public NormalizedCompilerStatus normalizedStatus;
    }
}
