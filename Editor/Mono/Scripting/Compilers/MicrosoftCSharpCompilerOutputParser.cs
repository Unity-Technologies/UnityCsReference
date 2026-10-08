// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;

namespace UnityEditor.Scripting.Compilers
{
    internal class MicrosoftCSharpCompilerOutputParser : CompilerOutputParserBase
    {
        public static readonly Regex sCompilerOutput = new Regex(@"(?<filename>.+)(\((?<line>\d+),(?<column>\d+)\)):\s*(?<type>warning|error|info)\s*(?<id>[^:]*):\s*(?<message>.*)", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        // Compilation-level diagnostics such as CS8785 (source generator failed) carry no file or
        // line, so sCompilerOutput never matches them and they would be dropped silently.
        public static readonly Regex sFilelessCompilerOutput = new Regex(@"^\s*(?<type>warning|error|info)\s+(?<id>CS\d+):\s*(?<message>.*)$", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        protected override bool ShouldParseLine(string line)
        {
            return line.Contains("warning", StringComparison.Ordinal) ||
                   line.Contains("error", StringComparison.Ordinal) ||
                   line.Contains("info", StringComparison.Ordinal);
        }

        protected override string GetInformationIdentifier()
        {
            return "info";
        }

        protected override Regex GetOutputRegex()
        {
            return sCompilerOutput;
        }

        protected override Regex GetInternalErrorOutputRegex()
        {
            return sFilelessCompilerOutput;
        }

        protected override string GetErrorIdentifier()
        {
            return "error";
        }
    }
}
