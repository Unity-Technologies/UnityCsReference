// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using Bee.BinLog;
using Bee.Serialization;
using NiceIO;
using UnityEditor.Scripting.Compilers;
using UnityEngine;
using Unity.Collections;
using Unity.ScriptUpdater.Common;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal static class PotentiallyUpdatableErrorMessages
    {
        public static bool IsAnyPotentiallyUpdatable(CompilerMessage[] messages, NodeFinishedMessage nodeResult,
            ObjectsFromDisk dataFromBuildProgram)
        {
#pragma warning disable UAC2001 // Avoid Linq
            var matches = messages
#pragma warning restore UAC2001
                                    .Select(m => MicrosoftCSharpCompilerOutputParser.sCompilerOutput.Match(m.message))
                                    .Where(m => m.Success && UpdatableDiagnosticClassifier.IsPotentiallyUpdatableDiagnosticId(m.Groups["id"].Value))
                                    .ToArray();

            if (matches.Length == 0)
                return false;

            var language = CompilerMessageLanguageFor(Helpers.LocalizeCompilerMessages(dataFromBuildProgram));

#pragma warning disable UAC2001 // Avoid Linq
            var typeNames = matches.Select(m => UpdatableDiagnosticClassifier.TryGetMissingTypeName(m.Groups["id"].Value, m.Groups["message"].Value, language));
#pragma warning restore UAC2001

            var assemblyData = Helpers.FindOutputDataAssemblyInfoFor(nodeResult, dataFromBuildProgram);
            var lines = new NPath(assemblyData.MovedFromExtractorFile).ReadAllLines();

#pragma warning disable UAC2001 // Avoid Linq
#pragma warning disable UAC2006 // Avoid Linq
            return typeNames.Any(t => lines.Contains(t));
#pragma warning restore UAC2001
#pragma warning restore UAC2006
        }

        static CompilerMessageLanguage CompilerMessageLanguageFor(bool localizedCompilerMessages)
        {
            if (!localizedCompilerMessages)
                return CompilerMessageLanguage.English;

            switch (LocalizationDatabase.currentEditorLanguage)
            {
                case SystemLanguage.ChineseTraditional:
                    return CompilerMessageLanguage.ChineseTraditional;
                case SystemLanguage.ChineseSimplified:
                    return CompilerMessageLanguage.ChineseSimplified;
                case SystemLanguage.Korean:
                    return CompilerMessageLanguage.Korean;
                default:
                    return CompilerMessageLanguage.English;
            }
        }
    }
}
