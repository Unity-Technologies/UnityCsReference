// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text.RegularExpressions;

namespace UnityEditor.Scripting.Compilers
{
    class UnityScriptCompilerOutputParser : CompilerOutputParserBase
    {
        private static Regex sCompilerOutput = new Regex(@"\s*(?<filename>.*)\((?<line>\d+),(?<column>\d+)\):\s*[BU]C(?<type>W|E)(?<id>[^:]*):\s*(?<message>.*)", RegexOptions.ExplicitCapture);

        private static Regex sUnknownTypeOrNamespace = new Regex(@"[^']*'(?<type_name>[^']+)'.*", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        protected override string GetErrorIdentifier()
        {
            return "E";
        }

        protected override Regex GetOutputRegex()
        {
            return sCompilerOutput;
        }

        protected override NormalizedCompilerStatus NormalizedStatusFor(Match match)
        {
            var status = TryNormalizeCompilerStatus(match, "0018", sUnknownTypeOrNamespace, NormalizeSimpleUnknownTypeOfNamespaceError);
            if (status.code != NormalizedCompilerStatusCode.NotNormalized)
                return status;

            return TryNormalizeCompilerStatus(match, "0005", sUnknownTypeOrNamespace, NormalizeSimpleUnknownTypeOfNamespaceError);
        }
    }
}
