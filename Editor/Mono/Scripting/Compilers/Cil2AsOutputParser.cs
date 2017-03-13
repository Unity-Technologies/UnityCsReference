// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Text;

namespace UnityEditor.Scripting.Compilers
{
    class Cil2AsOutputParser : UnityScriptCompilerOutputParser
    {
        public override IEnumerable<CompilerMessage> Parse(string[] errorOutput, string[] standardOutput, bool compilationHadFailure)
        {
            var parsingError = false;
            var currentErrorBuffer = new StringBuilder();
            foreach (var str in errorOutput)
            {
                if (str.StartsWith("ERROR: "))
                {
                    if (parsingError)
                    {
                        yield return CompilerErrorFor(currentErrorBuffer);
                        currentErrorBuffer.Length = 0;
                    }
                    currentErrorBuffer.AppendLine(str.Substring("ERROR: ".Length));
                    parsingError = true;
                }
                else
                {
                    if (parsingError)
                        currentErrorBuffer.AppendLine(str);
                }
            }
            if (parsingError)
                yield return CompilerErrorFor(currentErrorBuffer);
        }

        private static CompilerMessage CompilerErrorFor(StringBuilder currentErrorBuffer)
        {
            return new CompilerMessage() { type = CompilerMessageType.Error, message = currentErrorBuffer.ToString() };
        }
    }
}
