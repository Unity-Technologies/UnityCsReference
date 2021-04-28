// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UnityEditor.Scripting.Compilers
{
    internal abstract class CompilerOutputParserBase
    {
        protected static CompilerMessage CreateInternalCompilerErrorMessage(string[] compileroutput)
        {
            return new CompilerMessage
            {
                file = "",
                message = String.Join(Environment.NewLine, compileroutput),
                type = CompilerMessageType.Error,
                line = 0,
                column = 0,
            };
        }

        protected internal static CompilerMessage CreateCompilerMessageFromMatchedRegex(string line, Match m, string errorId, string informationId = null)
        {
            CompilerMessage message = new CompilerMessage();

            if (m.Groups["filename"].Success)
            {
                message.file = m.Groups["filename"].Value;
            }

            if (m.Groups["line"].Success)
            {
                message.line = Int32.Parse(m.Groups["line"].Value);
            }
            if (m.Groups["column"].Success)
            {
                message.column = Int32.Parse(m.Groups["column"].Value);
            }

            message.message = line;

            string messageType = m.Groups["type"].Value;

            if (messageType == errorId)
            {
                message.type = CompilerMessageType.Error;
            }
            else if (!string.IsNullOrEmpty(informationId) && messageType == informationId)
            {
                message.type = CompilerMessageType.Information;
            }
            else
            {
                message.type = CompilerMessageType.Warning;
            }

            return message;
        }

        public virtual IEnumerable<CompilerMessage> Parse(string[] errorOutput, bool compilationHadFailure)
        {
            return Parse(errorOutput, new string[0], compilationHadFailure);
        }

        /* we want to remove the assemblyName_unused argument, but today burst uses internalsvisibleto and inherits from this class :( so we cannot change this signature*/
        public virtual IEnumerable<CompilerMessage> Parse(string[] errorOutput, string[] standardOutput, bool compilationHadFailure, string assemblyName_unused = null)
        {
            var hasErrors = false;
            var msgs = new List<CompilerMessage>();
            var regex = GetOutputRegex();
            var internalErrorRegex = GetInternalErrorOutputRegex();


            foreach (var line in errorOutput)
            {
                //Jamplus can fail with enormous lines in the stdout, parsing of which can take 30! seconds.
                var line2 = line.Length > 1000 ? line.Substring(0, 100) : line;

                Match m = regex.Match(line2);
                if (!m.Success)
                {
                    if (internalErrorRegex != null)
                        m = internalErrorRegex.Match(line2);
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

        protected abstract string GetErrorIdentifier();

        protected virtual string GetInformationIdentifier()
        {
            return "info";
        }

        protected abstract Regex GetOutputRegex();
        protected virtual Regex GetInternalErrorOutputRegex() { return null; }
    }
}
