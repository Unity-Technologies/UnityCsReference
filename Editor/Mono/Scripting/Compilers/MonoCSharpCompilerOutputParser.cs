// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text.RegularExpressions;

namespace UnityEditor.Scripting.Compilers
{
    internal class MonoCSharpCompilerOutputParser : CSharpCompilerOutputParserBase
    {
        private static Regex sCompilerOutput = new Regex(@"\s*(?<filename>.*)\((?<line>\d+),(?<column>\d+)(\+{1})?\):\s*(?<type>warning|error)\s*(?<id>[^:]*):\s*(?<message>.*)", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
        private static Regex sInternalErrorCompilerOutput = new Regex(@"\s*(?<message>Internal compiler (?<type>error)) at\s*(?<filename>.*)\((?<line>\d+),(?<column>\d+)\):\s*(?<id>.*)", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        protected override Regex GetOutputRegex()
        {
            return sCompilerOutput;
        }

        protected override Regex GetInternalErrorOutputRegex()
        {
            return sInternalErrorCompilerOutput;
        }

        protected override string GetErrorIdentifier()
        {
            return "error";
        }
    }
}
