// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Scripting.Compilers
{
    sealed class Il2CppOutputParser : CompilerOutputParserBase
    {
        private const string _errorIdentifier = "IL2CPP error";
        private static readonly Regex sErrorRegexWithSourceInformation = new Regex(@"\s*(?<message>.*) in (?<filename>.*):(?<line>\d+)");

        private readonly string _jsonFileName;

        public Il2CppOutputParser(string jsonFileName)
        {
            _jsonFileName = jsonFileName;
        }

        public override IEnumerable<CompilerMessage> Parse(string[] errorOutput, string[] standardOutput, bool compilationHadFailure, string assemblyName)
        {
            var messages = new List<CompilerMessage>();
            if (File.Exists(_jsonFileName))
            {
                var jsonText = File.ReadAllText(_jsonFileName);
                var data = JsonUtility.FromJson<Il2CppToEditorData>(jsonText);
                foreach (var message in data.Messages)
                    messages.Add(new CompilerMessage
                        {message = message.Text, type = ToCompilerMessageType(message.Type)});
            }
            else
            {
                ParseMessageFromStandardOutput(standardOutput, assemblyName, messages);
            }

            return messages;
        }

        private static void ParseMessageFromStandardOutput(string[] standardOutput, string assemblyName, List<CompilerMessage> messages)
        {
            for (int i = 0; i < standardOutput.Length; ++i)
            {
                var line = standardOutput[i];
                if (line.StartsWith(_errorIdentifier))
                {
                    var sourceFile = string.Empty;
                    var sourceLine = 0;
                    var errorMessage = new StringBuilder();

                    var match = sErrorRegexWithSourceInformation.Match(line);
                    if (match.Success)
                    {
                        sourceFile = match.Groups["filename"].Value;
                        sourceLine = int.Parse(match.Groups["line"].Value);
                        errorMessage.AppendFormat("{0} in {1}:{2}", match.Groups["message"].Value, Path.GetFileName(sourceFile), sourceLine);
                    }
                    else
                    {
                        errorMessage.Append(line);
                    }

                    if (i + 1 < standardOutput.Length && standardOutput[i + 1].StartsWith("Additional information:"))
                    {
                        errorMessage.AppendFormat("{0}{1}", Environment.NewLine, standardOutput[i + 1]);
                        ++i;
                    }
                    messages.Add(new CompilerMessage
                    {
                        file = sourceFile,
                        line = sourceLine,
                        message = errorMessage.ToString(),
                        type = CompilerMessageType.Error,
                        assemblyName = assemblyName
                    });
                }
            }
        }

        CompilerMessageType ToCompilerMessageType(Il2CppMessageType il2cppMessageType)
        {
            if (il2cppMessageType == Il2CppMessageType.Warning)
                return CompilerMessageType.Warning;
            return CompilerMessageType.Error;
        }

        protected override string GetErrorIdentifier()
        {
            return _errorIdentifier;
        }

        protected override string GetInformationIdentifier()
        {
            return default;
        }

        protected override Regex GetOutputRegex()
        {
            return sErrorRegexWithSourceInformation;
        }
    }
}
