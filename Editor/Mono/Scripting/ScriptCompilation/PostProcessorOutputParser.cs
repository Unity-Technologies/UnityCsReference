// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor.Scripting.Compilers;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal class PostProcessorOutputParser : CompilerOutputParserBase
    {
        private static Regex sCompilerOutput = new Regex(@"(?<filename>[^:]*)\((?<line>\d+),(?<column>\d+)\):\s*(?<type>warning|error)\s*(?<message>.*)", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        protected override bool ShouldParseLine(string line)
        {
            return line.Contains("warning", StringComparison.Ordinal) ||
                   line.Contains("error", StringComparison.Ordinal);
        }

        public override IEnumerable<CompilerMessage> Parse(string[] errorOutput, string[] standardOutput, bool compilationHadFailure, string assemblyName_unused = null)
        {
            var hasErrors = false;
            var msgs = new List<CompilerMessage>();
            var regex = GetOutputRegex();
            var internalErrorRegex = GetInternalErrorOutputRegex();

            var isWarningError = false;
            var completeWarningErrors = new List<string>();
            for (int i = 0; i < errorOutput.Length; i++)
            {
                if (!ShouldParseLine(errorOutput[i]))
                {
                    if (isWarningError)
                        completeWarningErrors[completeWarningErrors.Count - 1] += "\n" + errorOutput[i];

                    continue;
                }

                if (regex.Match(errorOutput[i]).Success || (internalErrorRegex != null && internalErrorRegex.Match(errorOutput[i]).Success))
                {
                    completeWarningErrors.Add(errorOutput[i]);
                    isWarningError = true;
                }
                else if (completeWarningErrors.Count > 0)
                    completeWarningErrors[completeWarningErrors.Count - 1] += "\n" + errorOutput[i];
            }

            foreach (var line in completeWarningErrors)
            {
                //Jamplus can fail with enormous lines in the stdout, parsing of which can take 30! seconds.
                var line2 = line.Length > 1000 ? line.Substring(0, 100) : line;
                var regrexLineToMatch = line2.Split("\n");
                Match m = regex.Match(regrexLineToMatch[0]);
                if (!m.Success)
                {
                    if (internalErrorRegex != null)
                        m = internalErrorRegex.Match(regrexLineToMatch[0]);
                    if (!m.Success)
                        continue;
                }
                CompilerMessage message = CreateCompilerMessageFromMatchedRegex(line, m, GetErrorIdentifier(), GetInformationIdentifier());

                if (message.type == CompilerMessageType.Error)
                    hasErrors = true;

                msgs.Add(message);
            }
            if (compilationHadFailure && !hasErrors)
            {
                msgs.Add(CreateInternalCompilerErrorMessage(errorOutput));
            }
            return msgs;
        }

        protected override string GetInformationIdentifier()
        {
            return default;
        }

        protected override Regex GetOutputRegex()
        {
            return sCompilerOutput;
        }

        protected override string GetErrorIdentifier()
        {
            return "error";
        }
    }
}
