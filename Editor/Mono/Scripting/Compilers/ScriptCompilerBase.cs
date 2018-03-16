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
        public class ResponseFileData
        {
            public class Reference
            {
                public String Alias;
                public String Assembly;
            }

            public string[] Defines;
            public Reference[] References;
            public bool Unsafe;
            public string[] Errors;
        }

        public class CompilerOption
        {
            public string Arg;
            public string Value;
        }

        static readonly char[] CompilerOptionArgumentSeperators = { ';', ',' };

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

            var monoInstall = (PlayerSettingsEditor.IsLatestApiCompatibility(_island._api_compatibility_level))
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

        public static ResponseFileData ParseResponseFileFromFile(string responseFileName)
        {
            var relativeCustomResponseFilePath = Path.Combine("Assets", responseFileName);

            if (!File.Exists(relativeCustomResponseFilePath))
            {
                var empty = new ResponseFileData
                {
                    Defines = new string[0],
                    References = new ResponseFileData.Reference[0],
                    Unsafe = false,
                    Errors = new string[0]
                };

                return empty;
            }

            var responseFileText = File.ReadAllText(relativeCustomResponseFilePath);

            return ParseResponseFileText(responseFileText);
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

        public static ResponseFileData ParseResponseFileText(string responseFileText)
        {
            var compilerOptions = new List<CompilerOption>();

            var responseFileStrings = ResponseFileTextToStrings(responseFileText);

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

            var defines = new List<string>();
            var references = new List<ResponseFileData.Reference>();
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

                        foreach (string reference in refs)
                        {
                            if (reference.Length == 0)
                                continue;

                            int index = reference.IndexOf('=');
                            if (index > -1)
                            {
                                string alias = reference.Substring(0, index);
                                string assembly = reference.Substring(index + 1);

                                references.Add(new ResponseFileData.Reference { Alias = alias, Assembly = assembly });
                            }
                            else
                            {
                                references.Add(new ResponseFileData.Reference { Alias = string.Empty, Assembly = reference });
                            }
                        }
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
                }
            }

            var responseFileData = new ResponseFileData
            {
                Defines = defines.ToArray(),
                References = references.ToArray(),
                Unsafe = unsafeDefined,
                Errors = errors.ToArray()
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

        public CompilerMessage(CompilerMessage cm)
        {
            message = cm.message;
            file = cm.file;
            line = cm.line;
            column = cm.column;
            type = cm.type;
            normalizedStatus = cm.normalizedStatus;
        }
    }
}
