// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text.RegularExpressions;
using UnityEngine.Bindings;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Utilities for normalizing USS variable names.
/// </summary>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal static class VariableNameUtilities
{
    public static readonly string UssVariablePrefix = "--";
    public static readonly string USSVariablePattern = @"[^a-z0-9A-Z_-]";
    public static readonly string USSVariableInvalidCharFiller = "-";

    public static string GetCleanVariableName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var cleanName = Regex.Replace(value.Trim(),
            USSVariablePattern,
            USSVariableInvalidCharFiller);

        if (!cleanName.StartsWith(UssVariablePrefix))
            cleanName = UssVariablePrefix + cleanName;

        return cleanName;
    }
}
