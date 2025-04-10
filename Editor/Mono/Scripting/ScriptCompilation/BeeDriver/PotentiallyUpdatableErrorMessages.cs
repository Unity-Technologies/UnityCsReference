// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Text.RegularExpressions;
using Bee.BinLog;
using Bee.Serialization;
using NiceIO;
using UnityEditor.Scripting.Compilers;
using UnityEngine;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal static class PotentiallyUpdatableErrorMessages
    {
        public static bool IsAnyPotentiallyUpdatable(CompilerMessage[] messages, NodeFinishedMessage nodeResult,
            ObjectsFromDisk dataFromBuildProgram)
        {
            var matches = messages
                                    .Select(m => MicrosoftCSharpCompilerOutputParser.sCompilerOutput.Match(m.message))
                                    .Where(m => m.Success && IsPotentiallyUpdatableDiagnostic(m))
                                    .ToArray();

            if (matches.Length == 0)
                return false;

            var localizedCompilerMessages = Helpers.LocalizeCompilerMessages(dataFromBuildProgram);
            var compilerMessageParser = GetCompilerMessageParser();

            var typeNames = matches.Select(m => MissingTypeNameFor(m, compilerMessageParser));

            var assemblyData = Helpers.FindOutputDataAssemblyInfoFor(nodeResult, dataFromBuildProgram);
            var lines = new NPath(assemblyData.MovedFromExtractorFile).ReadAllLines();

            return typeNames.Any(t => lines.Contains(t));

            CompilerMessageParser GetCompilerMessageParser()
            {
                if (!localizedCompilerMessages) return EnglishMessageParser;

                return AlternativeLanguageMessageParser.TargetLanguages.Any(l => l == LocalizationDatabase.currentEditorLanguage)
                    ? AlternativeLanguageMessageParser
                    : EnglishMessageParser;
            }

            static bool IsPotentiallyUpdatableDiagnostic(Match match) => EnglishMessageParser.For(match.Groups["id"].Value) != null;
        }

        private static string MissingTypeNameFor(Match match, CompilerMessageParser compilerMessageParser)
        {
            var typeNameParser = compilerMessageParser.For(match.Groups["id"].Value);
            if (typeNameParser == null)
                return null;

            var matchedMessage = typeNameParser.Match(match.Groups["message"].Value);
            return !matchedMessage.Success ? null : matchedMessage.Groups["type_name"].Value;
        }

        private static CompilerMessageParser EnglishMessageParser = new(
            //error CS0117: 'type_name' does not contain a definition for 'member_name'
            new Regex("[^'`“]*['`“](?<type_name>[^'`”]+)['`”][^'`“]+['`“](?<member_name>[^'`”]+)['`”]", RegexOptions.ExplicitCapture | RegexOptions.Compiled),

            //error CS0234: The type or namespace name 'type_name' does not exist in the namespace 'namespace' (are you missing an assembly reference?)
            new Regex("[^'`]*['`](?<type_name>[^']+)'[^'`]+['`](?<namespace>[^']+)'",RegexOptions.ExplicitCapture | RegexOptions.Compiled),

            //error CS0246: The type or namespace name 'type_name' could not be found (are you missing a using directive or an assembly reference?)
            //error CS0103: The name 'type_name' does not exist in the current context
            new Regex("[^'`“]*['`“](?<type_name>[^'`”]+)['`”].*", RegexOptions.ExplicitCapture | RegexOptions.Compiled),
            new[] { SystemLanguage.English }
        );

        // Parses compilers messages for languages that reports CS0234 in the format: text `name space name` text `type name` (for instance, simplified chinese)
        private static CompilerMessageParser AlternativeLanguageMessageParser = EnglishMessageParser with
        {
            MissingType = new Regex("([^'`“]*['`“](?<namespace>[^'`”]+)['`”][^'`“]+['`“](?<type_name>[^'`”]+)['`”])", RegexOptions.ExplicitCapture | RegexOptions.Compiled),
            TargetLanguages = new[] { SystemLanguage.ChineseTraditional, SystemLanguage.ChineseSimplified, SystemLanguage.Korean }
        };

        // We have a `CompilerMessageParser` for each language that produces the errors listed in the `EnglishMessageParser` one above
        // in a different format. For now we only have the `AlternativeLanguageMessageParser` that covers Simplified Chinese,
        // Traditional Chinese and Korean
        private record struct CompilerMessageParser(Regex MissingMember, Regex MissingType, Regex UnknownTypeOrNamespace, params SystemLanguage[] TargetLanguages)
        {
            public Regex For(string diagnosticId) => diagnosticId switch
            {
                "CS0117" => MissingMember,
                "CS0246" => UnknownTypeOrNamespace,
                "CS0103" => UnknownTypeOrNamespace,
                "CS0234" => MissingType,
                _ => null
            };
        }
    }
}
