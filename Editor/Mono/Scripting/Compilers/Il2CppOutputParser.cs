// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace UnityEditor.Scripting.Compilers
{
    class Il2CppOutputParser : CompilerOutputParserBase
    {
        private const string _errorIdentifier = "IL2CPP error";
        private static readonly Regex sErrorRegexWithSourceInformation = new Regex(@"\s*(?<message>.*) in (?<filename>.*):(?<line>\d+)");

        public override IEnumerable<CompilerMessage> Parse(string[] errorOutput, string[] standardOutput, bool compilationHadFailure)
        {
            // This code is not unit tested, so modify it with caution.
            var messages = new List<CompilerMessage>();
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
                        type = CompilerMessageType.Error
                    });
                }
            }

            return messages;
        }

        protected override string GetErrorIdentifier()
        {
            return _errorIdentifier;
        }

        protected override Regex GetOutputRegex()
        {
            return sErrorRegexWithSourceInformation;
        }
    }
}
