// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;

namespace UnityEditor.Scripting.Compilers
{
    class GendarmeRuleData
    {
        public int LastIndex = 0;
        public int Line = 0;
        public string File = "";
        public string Problem;
        public string Details;
        public string Severity;
        public string Source;
        public string Location;
        public string Target;
        public bool IsAssemblyError;
    }

    class GendarmeOutputParser : UnityScriptCompilerOutputParser
    {
        public override IEnumerable<CompilerMessage> Parse(string[] errorOutput, bool compilationHadFailure)
        {
            throw new ArgumentException("Gendarme Output Parser needs standard out");
        }

        public override IEnumerable<CompilerMessage> Parse(string[] errorOutput, string[] standardOutput, bool compilationHadFailure)
        {
            for (var i = 0; i < standardOutput.Length; i++)
            {
                if (!standardOutput[i].StartsWith("Problem:")) continue;
                var grd = GetGendarmeRuleDataFor(standardOutput, i);
                var compilerErrorFor = CompilerErrorFor(grd);
                yield return compilerErrorFor;
                i = grd.LastIndex + 1;
            }
        }

        private static CompilerMessage CompilerErrorFor(GendarmeRuleData gendarmeRuleData)
        {
            var messageBuffer = new StringBuilder();
            messageBuffer.AppendLine(gendarmeRuleData.Problem);
            messageBuffer.AppendLine(gendarmeRuleData.Details);

            messageBuffer.AppendLine(!string.IsNullOrEmpty(gendarmeRuleData.Location)
                ? gendarmeRuleData.Location
                : String.Format("{0} at line : {1}", gendarmeRuleData.Source, gendarmeRuleData.Line));

            var message = messageBuffer.ToString();
            return new CompilerMessage
            {
                type = CompilerMessageType.Error,
                message = message,
                file = gendarmeRuleData.File,
                line = gendarmeRuleData.Line,
                column = 1
            };
        }

        private static GendarmeRuleData GetGendarmeRuleDataFor(IList<string> output, int index)
        {
            var gendarmeRuleData = new GendarmeRuleData();
            //Read until we hit a non detail line.
            for (var i = index; i < output.Count; i++)
            {
                var currentLine = output[i];
                const string problemString = "Problem:";
                if (currentLine.StartsWith(problemString))
                {
                    gendarmeRuleData.Problem = currentLine.Substring(currentLine.LastIndexOf(problemString, StringComparison.Ordinal) + problemString.Length);
                }
                else if (currentLine.StartsWith("* Details"))
                {
                    gendarmeRuleData.Details = currentLine;
                }
                else if (currentLine.StartsWith("* Source"))
                {
                    gendarmeRuleData.IsAssemblyError = false;
                    gendarmeRuleData.Source = currentLine;
                    gendarmeRuleData.Line = GetLineNumberFrom(currentLine);
                    gendarmeRuleData.File = GetFileNameFrome(currentLine);
                }
                else if (currentLine.StartsWith("* Severity"))
                {
                    gendarmeRuleData.Severity = currentLine;
                }
                else if (currentLine.StartsWith("* Location"))
                {
                    gendarmeRuleData.IsAssemblyError = true;
                    gendarmeRuleData.Location = currentLine;
                }
                else if (currentLine.StartsWith("* Target"))
                {
                    gendarmeRuleData.Target = currentLine;
                }
                else
                {
                    gendarmeRuleData.LastIndex = i;
                    break;
                }
            }
            return gendarmeRuleData;
        }

        private static string GetFileNameFrome(string currentLine)
        {
            const string stringToCheck = "* Source:";
            var beginSourceFile = currentLine.LastIndexOf(stringToCheck) + stringToCheck.Length;
            var endSourceFile = currentLine.IndexOf("(");

            if (beginSourceFile != -1 && endSourceFile != -1)
                return currentLine.Substring(beginSourceFile, endSourceFile - beginSourceFile).Trim();

            return "";
        }

        private static int GetLineNumberFrom(string currentLine)
        {
            var beginLineNumber = currentLine.IndexOf("(") + 2;
            var endLineNumber = currentLine.IndexOf(")");
            if (beginLineNumber != -1 && endLineNumber != -1)
                return int.Parse(currentLine.Substring(beginLineNumber, endLineNumber - beginLineNumber));

            return 0;
        }
    }
}
