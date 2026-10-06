// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text.RegularExpressions;
using UnityEngine.Bindings;

namespace Unity.SmartStrings;

[VisibleToOtherModules("UnityEditor.SmartStringsModule")]
static class StringExtensionMethods
{
    static readonly Regex s_WhitespaceRegex = new Regex(@"\s+");

    [VisibleToOtherModules("UnityEditor.SmartStringsModule")]
    public static string ReplaceWhiteSpaces(this string str, string replacement = "")
    {
        return s_WhitespaceRegex.Replace(str, replacement);
    }
}
