// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;
using UnityEditor.Utils;

namespace UnityEditor.Scripting.Compilers
{
    internal abstract class ScriptCompilerBase : IDisposable
    {
        public class ResponseFileData
        {
            public string[] Defines;
            public string[] FullPathReferences;
            public bool Unsafe;
            public string[] Errors;
            public string[] OtherArguments;
        }

        public class CompilerOption
        {
            public string Arg;
            public string Value;
        }

        static readonly char[] CompilerOptionArgumentSeperators = { ';', ',' };

        private Program process;
        private bool _runAPIUpdater;

        // ToDo: would be nice to move MonoIsland to MonoScriptCompilerBase
        protected MonoIsland m_Island;

        protected abstract Program StartCompiler();

        protected abstract CompilerOutputParserBase CreateOutputParser();

        protected ScriptCompilerBase(MonoIsland island, bool runAPIUpdater)
        {
            m_Island = island;
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
            var profile = BuildPipeline.CompatibilityProfileToClassLibFolder(m_Island._api_compatibility_level);

            var monoInstall = PlayerSettingsEditor.IsLatestApiCompatibility(m_Island._api_compatibility_level)
                ? MonoInstallationFinder.MonoBleedingEdgeInstallation
                : MonoInstallationFinder.MonoInstallation;

            return MonoInstallationFinder.GetProfileDirectory(profile, monoInstall);
        }

        protected abstract string[] GetSystemReferenceDirectories();

        protected bool AddCustomResponseFileIfPresent(List<string> arguments, string responseFileName)
        {
            var relativeCustomResponseFilePath = Path.Combine("Assets", responseFileName);

            if (!File.Exists(relativeCustomResponseFilePath))
                return false;
            AddResponseFileToArguments(arguments, relativeCustomResponseFilePath);
            return true;
        }

        protected void AddResponseFileToArguments(List<string> arguments, string responseFileName)
        {
            var responseFileData = ParseResponseFileFromFile(
                responseFileName,
                Application.dataPath,
                GetSystemReferenceDirectories());
            foreach (var error in responseFileData.Errors)
            {
                Debug.LogError($"{responseFileName} Parse Error : {error}");
            }

            arguments.AddRange(responseFileData.Defines.Distinct().Select(define => "/define:" + define));
            arguments.AddRange(responseFileData.FullPathReferences.Select(reference =>
                "/reference:" + PrepareFileName(reference)));
            if (responseFileData.Unsafe)
            {
                arguments.Add("/unsafe");
            }

            arguments.AddRange(responseFileData.OtherArguments);
        }

        public static ResponseFileData ParseResponseFileFromFile(
            string responseFilePath,
            string projectDirectory,
            string[] systemReferenceDirectories)
        {
            var relativeResponseFilePath = GetRelativePath(responseFilePath, projectDirectory);
            var responseFile = AssetDatabase.LoadAssetAtPath<TextAsset>(relativeResponseFilePath);

            if (!responseFile && File.Exists(responseFilePath))
            {
                var responseFileText = File.ReadAllText(responseFilePath);
                return ParseResponseFileText(
                    responseFileText,
                    responseFilePath,
                    projectDirectory,
                    systemReferenceDirectories);
            }

            if (!responseFile)
            {
                var empty = new ResponseFileData
                {
                    Defines = new string[0],
                    FullPathReferences = new string[0],
                    Unsafe = false,
                    Errors = new string[0],
                    OtherArguments = new string[0],
                };

                return empty;
            }

            return ParseResponseFileText(
                responseFile.text,
                responseFile.name,
                projectDirectory,
                systemReferenceDirectories);
        }

        static string GetRelativePath(string responseFilePath, string projectDirectory)
        {
            if (Path.IsPathRooted(responseFilePath) && responseFilePath.Contains(projectDirectory))
            {
                responseFilePath = responseFilePath.Substring(Directory.GetParent(Application.dataPath).FullName.Length + 1);
            }
            return responseFilePath;
        }

        // From:
        // https://github.com/mono/mono/blob/c106cdc775792ceedda6da58de7471f9f5c0b86c/mcs/mcs/settings.cs
        //
        // settings.cs: All compiler settings
        //
        // Author: Miguel de Icaza (miguel@ximian.com)
        //            Ravi Pratap  (ravi@ximian.com)
        //            Marek Safar  (marek.safar@gmail.com)
        //
        //
        // Dual licensed under the terms of the MIT X11 or GNU GPL
        //
        // Copyright 2001 Ximian, Inc (http://www.ximian.com)
        // Copyright 2004-2008 Novell, Inc
        // Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
        static string[] ResponseFileTextToStrings(string responseFileText)
        {
            var args = new List<string>();

            var sb = new System.Text.StringBuilder();

            var textLines = responseFileText.Split('\n', '\r');

            foreach (var line in textLines)
            {
                int t = line.Length;

                for (int i = 0; i < t; i++)
                {
                    char c = line[i];

                    if (c == '"' || c == '\'')
                    {
                        char end = c;

                        for (i++; i < t; i++)
                        {
                            c = line[i];

                            if (c == end)
                                break;
                            sb.Append(c);
                        }
                    }
                    else if (c == ' ')
                    {
                        if (sb.Length > 0)
                        {
                            args.Add(sb.ToString());
                            sb.Length = 0;
                        }
                    }
                    else
                        sb.Append(c);
                }
                if (sb.Length > 0)
                {
                    args.Add(sb.ToString());
                    sb.Length = 0;
                }
            }

            return args.ToArray();
        }

        static ResponseFileData ParseResponseFileText(
            string fileContent,
            string fileName,
            string projectDirectory,
            string[] systemReferenceDirectories)
        {
            var compilerOptions = new List<CompilerOption>();

            var responseFileStrings = ResponseFileTextToStrings(fileContent);

            foreach (var line in responseFileStrings)
            {
                int idx = line.IndexOf(':');
                string arg, value;

                if (idx == -1)
                {
                    arg = line;
                    value = "";
                }
                else
                {
                    arg = line.Substring(0, idx);
                    value = line.Substring(idx + 1);
                }

                if (!string.IsNullOrEmpty(arg) && arg[0] == '-')
                    arg = '/' + arg.Substring(1);

                compilerOptions.Add(new CompilerOption { Arg = arg, Value = value });
            }

            var responseArguments = new List<string>();
            var defines = new List<string>();
            var references = new List<string>();
            bool unsafeDefined = false;
            var errors = new List<string>();

            foreach (var option in compilerOptions)
            {
                var arg = option.Arg;
                var value = option.Value;

                switch (arg)
                {
                    case "/d":
                    case "/define":
                    {
                        if (value.Length == 0)
                        {
                            errors.Add("No value set for define");
                            break;
                        }

                        var defs = value.Split(CompilerOptionArgumentSeperators);
                        foreach (string define in defs)
                            defines.Add(define.Trim());
                    }
                    break;

                    case "/r":
                    case "/reference":
                    {
                        if (value.Length == 0)
                        {
                            errors.Add("No value set for reference");
                            break;
                        }

                        string[] refs = value.Split(CompilerOptionArgumentSeperators);

                        if (refs.Length != 1)
                        {
                            errors.Add("Cannot specify multiple aliases using single /reference option");
                            break;
                        }

                        var reference = refs[0];
                        if (reference.Length == 0)
                        {
                            continue;
                        }

                        var responseReference = reference;

                        int index = reference.IndexOf('=');
                        if (index > -1)
                        {
                            var assembly = reference.Substring(index + 1);

                            responseReference = assembly;
                        }

                        var fullPathReference = responseReference;
                        if (!Path.IsPathRooted(responseReference))
                        {
                            foreach (var directory in systemReferenceDirectories)
                            {
                                var systemReferencePath = Paths.Combine(directory, responseReference);
                                if (File.Exists(systemReferencePath))
                                {
                                    fullPathReference = systemReferencePath;
                                    break;
                                }
                            }

                            var userPath = Paths.Combine(projectDirectory, responseReference);
                            if (File.Exists(userPath))
                            {
                                fullPathReference = userPath;
                            }
                        }

                        if (fullPathReference == "")
                        {
                            errors.Add($"{fileName}: not parsed correctly: {responseReference} could not be found as a system library.\n" +
                                "If this was meant as a user reference please provide the relative path from project root (parent of the Assets folder) in the response file.");
                            continue;
                        }

                        responseReference = fullPathReference.Replace('\\', '/');
                        references.Add(responseReference);
                    }
                    break;

                    case "/unsafe":
                    case "/unsafe+":
                    {
                        unsafeDefined = true;
                    }
                    break;

                    case "/unsafe-":
                    {
                        unsafeDefined = false;
                    }
                    break;
                    default:
                        var valueWithColon = value.Length == 0 ? "" : ":" + value;
                        responseArguments.Add(arg + valueWithColon);
                        break;
                }
            }

            var responseFileData = new ResponseFileData
            {
                Defines = defines.ToArray(),
                FullPathReferences = references.ToArray(),
                Unsafe = unsafeDefined,
                Errors = errors.ToArray(),
                OtherArguments = responseArguments.ToArray(),
            };

            return responseFileData;
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

            if (process == null)
            {
                return new CompilerMessage[0];
            }

            DumpStreamOutputToLog();

            return CreateOutputParser().Parse(
                GetStreamContainingCompilerMessages(),
                CompilationHadFailure(),
                Path.GetFileName(m_Island._output)
                ).ToArray();
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

                Console.WriteLine(
                    "-----CompilerOutput:-stdout--exitcode: " + process.ExitCode
                    + "--compilationhadfailure: " + hadCompilationFailure
                    + "--outfile: " + m_Island._output
                );
                foreach (string line in stdOutput)
                    Console.WriteLine(line);

                Console.WriteLine("-----CompilerOutput:-stderr----------");
                foreach (string line in errorOutput)
                    Console.WriteLine(line);
                Console.WriteLine("-----EndCompilerOutput---------------");
            }
        }

        protected void RunAPIUpdaterIfRequired(string responseFile, IList<string> pathMappings)
        {
            if (!_runAPIUpdater)
                return;

            var pathMappingsFilePath = Path.GetTempFileName();
            if (pathMappings != null)
                File.WriteAllLines(pathMappingsFilePath, pathMappings.ToArray());

            APIUpdaterHelper.UpdateScripts(
                responseFile,
                m_Island.GetExtensionOfSourceFiles(),
                PrepareFileName(pathMappingsFilePath)
            );
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

        public string assemblyName;

        public CompilerMessage(CompilerMessage cm)
        {
            message = cm.message;
            file = cm.file;
            line = cm.line;
            column = cm.column;
            type = cm.type;
            normalizedStatus = cm.normalizedStatus;
            assemblyName = cm.assemblyName;
        }
    }
}
