// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;

namespace UnityEditor.Scripting.Compilers
{
    internal abstract class ScriptCompilerBase : IDisposable
    {
        public class CompilerOption
        {
            public string Arg;
            public string Value;
        }

        protected ScriptAssembly assembly;
        protected string tempOutputDirectory;

        protected ScriptCompilerBase(ScriptAssembly assembly, string tempOutputDirectory)
        {
            this.assembly = assembly;
            this.tempOutputDirectory = tempOutputDirectory;
        }

        public abstract void BeginCompiling();

        public abstract void Dispose();

        public abstract bool Poll();

        public abstract void WaitForCompilationToFinish();

        //do not change the return type, native unity depends on this one.
        public abstract CompilerMessage[] GetCompilerMessages();

        public abstract ProcessStartInfo GetProcessStartInfo();

        internal static void AddResponseFileToArguments(List<string> arguments, string responseFileName, ApiCompatibilityLevel apiCompatibilityLevel)
        {
            var systemReferencesDirectories = MonoLibraryHelpers.GetSystemReferenceDirectories(apiCompatibilityLevel);

            var responseFileData = MicrosoftResponseFileParser.ParseResponseFileFromFile(
                responseFileName,
                Directory.GetParent(Application.dataPath).FullName,
                systemReferencesDirectories);
            foreach (var error in responseFileData.Errors)
            {
                UnityEngine.Debug.LogError($"{responseFileName} Parse Error : {error}");
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

        protected static string PrepareFileName(string fileName)
        {
            return CommandLineFormatter.PrepareFileName(fileName);
        }
    }

    /// Normalized 'status' code for a [[CompilerMessage]]
    internal enum NormalizedCompilerStatusCode
    {
        NotNormalized = 0,

        /// Maps to C# CS0117
        MemberNotFound = 1, // details syntax: TypeNamespaceQualifiedName:MemberName

        // Maps to C# CS0246/CS0234
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
