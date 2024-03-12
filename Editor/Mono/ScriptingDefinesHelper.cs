// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor;

internal static class ScriptingDefinesHelper
{
    private static readonly char[] DefineSeparators = { ';', ',', ' ' };

    internal static string ConvertScriptingDefineArrayToString(string[] defines)
    {
        if (defines == null)
        {
            throw new ArgumentNullException(nameof(defines));
        }

        var flattenedDefines = new List<string>();
        foreach (var define in defines)
        {
            flattenedDefines.AddRange(define.Split(DefineSeparators, StringSplitOptions.RemoveEmptyEntries));
        }

        var distinctDefines = new HashSet<string>();
        foreach (var define in flattenedDefines)
        {
            distinctDefines.Add(define);
        }

        return string.Join(";", distinctDefines);
    }

    internal static string[] ConvertScriptingDefineStringToArray(string defines)
    {
        var splitDefines = string.IsNullOrEmpty(defines)
            ? Array.Empty<string>()
            : defines.Split(DefineSeparators, StringSplitOptions.RemoveEmptyEntries);

        var distinctDefines = new HashSet<string>();
        foreach (var define in splitDefines)
        {
            distinctDefines.Add(define);
        }

        var distinctDefinesArray = new string[distinctDefines.Count];
        distinctDefines.CopyTo(distinctDefinesArray);
        return distinctDefinesArray;
    }
}
