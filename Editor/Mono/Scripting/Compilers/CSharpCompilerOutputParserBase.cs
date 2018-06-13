// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UnityEditor.Scripting.Compilers
{
    internal abstract class CSharpCompilerOutputParserBase : CompilerOutputParserBase
    {
        private static Regex sMissingMember = new Regex(@"[^'`]*['`](?<type_name>[^']+)'[^'`]+['`](?<member_name>[^']+)'", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
        private static Regex sMissingType = new Regex(@"[^'`]*['`](?<type_name>[^']+)'[^'`]+['`](?<namespace>[^']+)'", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
        private static Regex sUnknownTypeOrNamespace = new Regex(@"[^'`]*['`](?<type_name>[^']+)'.*", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
        protected override NormalizedCompilerStatus NormalizedStatusFor(Match match)
        {
            var status = TryNormalizeCompilerStatus(match, "CS0117", sMissingMember, NormalizeMemberNotFoundError);
            if (status.code != NormalizedCompilerStatusCode.NotNormalized)
                return status;

            status = TryNormalizeCompilerStatus(match, "CS0246", sUnknownTypeOrNamespace, NormalizeSimpleUnknownTypeOfNamespaceError);
            if (status.code != NormalizedCompilerStatusCode.NotNormalized)
                return status;

            status = TryNormalizeCompilerStatus(match, "CS0234", sMissingType, NormalizeUnknownTypeMemberOfNamespaceError);
            if (status.code != NormalizedCompilerStatusCode.NotNormalized)
                return status;

            return TryNormalizeCompilerStatus(match, "CS0103", sUnknownTypeOrNamespace, NormalizeSimpleUnknownTypeOfNamespaceError);
        }
    }
}
