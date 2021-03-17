// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Text.RegularExpressions;
using Bee.BeeDriver;
using NiceIO;
using ScriptCompilationBuildProgram.Data;
using UnityEditor.Scripting.Compilers;

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class PotentiallyUpdatableErrorMessages
    {
        public static bool IsAnyPotentiallyUpdatable(CompilerMessage[] messages, NodeResult nodeResult, BeeDriver beeDriver)
        {
            var matches = messages.Select(m => MicrosoftCSharpCompilerOutputParser.sCompilerOutput.Match(m.message)).Where(m => m.Success).ToArray();
            var typeNames = matches.Select(MissingTypeNameFor).Where(t => t != null).ToArray();

            if (!typeNames.Any())
                return false;

            var assemblyData = Helpers.FindOutputDataAssemblyInfoFor(nodeResult, beeDriver);
            var lines = new NPath(assemblyData.MovedFromExtractorFile).ReadAllLines();

            return typeNames.Any(t => lines.Contains(t));
        }

        private static readonly Regex sMissingMember = new Regex(@"[^'`]*['`](?<type_name>[^']+)'[^'`]+['`](?<member_name>[^']+)'", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
        private static readonly Regex sMissingType = new Regex(@"[^'`]*['`](?<type_name>[^']+)'[^'`]+['`](?<namespace>[^']+)'", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
        private static readonly Regex sUnknownTypeOrNamespace = new Regex(@"[^'`]*['`](?<type_name>[^']+)'.*", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        private static string MissingTypeNameFor(Match match)
        {
            Regex TypeNameParserFor(string compilerErrorCode)
            {
                switch (compilerErrorCode)
                {
                    case "CS0117":
                        return sMissingMember;
                    case "CS0246":
                    case "CS0103":
                        return sUnknownTypeOrNamespace;
                    case "CS0234":
                        return sMissingType;
                    default:
                        return null;
                }
            }

            var id = match.Groups["id"].Value;
            var typeNameParser = TypeNameParserFor(id);
            if (typeNameParser == null)
                return null;

            var matchedMessage = typeNameParser.Match(match.Groups["message"].Value);
            return !matchedMessage.Success ? null : matchedMessage.Groups["type_name"].Value;
        }
    }
}
