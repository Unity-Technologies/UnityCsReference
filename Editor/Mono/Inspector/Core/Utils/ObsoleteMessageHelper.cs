// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text.RegularExpressions;

namespace UnityEditor
{
    static class ObsoleteMessageHelper
    {
        static Regex s_VersionTagRegex;
        // Matches version tags in the format #tagName(MAJOR.MINOR) where tagName is any latin letters.
        // Examples: #from(2022.3), #breakingFrom(6000.5)
        static Regex VersionTagRegex => s_VersionTagRegex ??=
            new Regex(@"#[a-zA-Z]+\(\d+\.\d+\)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        internal static string StripVersionTags(string message)
        {
            var match = VersionTagRegex.Match(message);
            return !match.Success ? message.Trim() : message.Substring(0, match.Index).Trim();
        }
    }
}
