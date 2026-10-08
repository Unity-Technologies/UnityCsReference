// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    static class SearchFilterUtility
    {
        // A value containing spaces has to be quoted, which is how the filter dropdown writes it: area="Build Size"
        internal static Regex CreateFilterRegex(string key)
        {
            return new Regex($"{key}=(?:\"([^\"]*)\"|([^;\\s]+))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        internal static string GetFilterValue(Match filterMatch)
        {
            return filterMatch.Groups[1].Success ? filterMatch.Groups[1].Value : filterMatch.Groups[2].Value;
        }

        // Accepts the spacing ObjectNames.NicifyVariableName adds to the names shown in the UI, e.g. "Asset Issue"
        internal static bool TryParseDisplayedEnum<T>(string value, out T result) where T : struct
        {
            return Enum.TryParse(value.Replace(" ", ""), true, out result);
        }
    }
}
