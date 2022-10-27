// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;

namespace Unity.GraphToolsFoundation
{
    static class StringExtensions_Internal
    {
        static readonly Regex k_CodifyRegex = new Regex("[^a-zA-Z0-9]", RegexOptions.Compiled);

        internal static string CodifyString_Internal(this string str)
        {
            return k_CodifyRegex.Replace(str, "_");
        }
    }
}
