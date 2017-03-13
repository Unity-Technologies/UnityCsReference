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
        static protected CompilerMessage CreateInternalCompilerErrorMessage(string[] compileroutput)
        {
            CompilerMessage message;
            message.file = "";
            message.message = String.Join("\n", compileroutput);
            message.type = CompilerMessageType.Error;
            message.line = 0;
            message.column = 0;
            message.normalizedStatus = default(NormalizedCompilerStatus);

            return message;
        }

        internal static protected CompilerMessage CreateCompilerMessageFromMatchedRegex(string line, Match m, string erroridentifier)
        {
            CompilerMessage message;
            message.file = m.Groups["filename"].Value;
            message.message = line;
            message.line = Int32.Parse(m.Groups["line"].Value);
            message.column = Int32.Parse(m.Groups["column"].Value);
            message.type = (m.Groups["type"].Value == erroridentifier) ? CompilerMessageType.Error : CompilerMessageType.Warning;
            message.normalizedStatus = default(NormalizedCompilerStatus);

            return message;
        }

        public virtual IEnumerable<CompilerMessage> Parse(string[] errorOutput, bool compilationHadFailure)
        {
            return Parse(errorOutput, new string[0], compilationHadFailure);
        }

        public virtual IEnumerable<CompilerMessage> Parse(string[] errorOutput, string[] standardOutput, bool compilationHadFailure)
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
                CompilerMessage message = CreateCompilerMessageFromMatchedRegex(line, m, GetErrorIdentifier());
                message.normalizedStatus = NormalizedStatusFor(m);

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

        protected virtual NormalizedCompilerStatus NormalizedStatusFor(Match match)
        {
            return default(NormalizedCompilerStatus);
        }

        protected abstract string GetErrorIdentifier();

        protected abstract Regex GetOutputRegex();
        protected virtual Regex GetInternalErrorOutputRegex() { return null; }

        protected static NormalizedCompilerStatus TryNormalizeCompilerStatus(Match match, string idToCheck, Regex messageParser, Func<Match, Regex, NormalizedCompilerStatus> normalizer)
        {
            var id = match.Groups["id"].Value;
            var ret = default(NormalizedCompilerStatus);
            if (id != idToCheck)
                return ret;

            return normalizer(match, messageParser);
        }

        protected static NormalizedCompilerStatus NormalizeMemberNotFoundError(Match outputMatch, Regex messageParser)
        {
            NormalizedCompilerStatus ret;
            ret.code = NormalizedCompilerStatusCode.MemberNotFound;

            var dm = messageParser.Match(outputMatch.Groups["message"].Value);
            ret.details = dm.Groups["type_name"].Value + "%" + dm.Groups["member_name"].Value;

            return ret;
        }

        protected static NormalizedCompilerStatus NormalizeSimpleUnknownTypeOfNamespaceError(Match outputMatch, Regex messageParser)
        {
            NormalizedCompilerStatus ret;
            ret.code = NormalizedCompilerStatusCode.UnknownTypeOrNamespace;

            var dm = messageParser.Match(outputMatch.Groups["message"].Value);
            ret.details = "EntityName=" + dm.Groups["type_name"].Value + "\n"
                + "Script=" + outputMatch.Groups["filename"].Value + "\n"
                + "Line=" + outputMatch.Groups["line"].Value + "\n"
                + "Column=" + outputMatch.Groups["column"].Value;

            return ret;
        }

        protected static NormalizedCompilerStatus NormalizeUnknownTypeMemberOfNamespaceError(Match outputMatch, Regex messageParser)
        {
            NormalizedCompilerStatus ret;
            ret.code = NormalizedCompilerStatusCode.UnknownTypeOrNamespace;

            var dm = messageParser.Match(outputMatch.Groups["message"].Value);
            ret.details = "EntityName=" + dm.Groups["namespace"].Value + "." + dm.Groups["type_name"].Value + "\n"
                + "Script=" + outputMatch.Groups["filename"].Value + "\n"
                + "Line=" + outputMatch.Groups["line"].Value + "\n"
                + "Column=" + outputMatch.Groups["column"].Value;

            return ret;
        }
    }
}
