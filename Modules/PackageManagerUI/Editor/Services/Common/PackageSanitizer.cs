// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Globalization;
using System.Text.RegularExpressions;

namespace UnityEditor.PackageManager.UI.Internal;

internal static class PackageSanitizer
{
    public static string SanitizeDisplayName(string value)
    {
        // Removing invalid characters because Windows does not allow them in folder names
        return Regex.Replace(value, @"[\\/:*?""<>|]", "").Trim();
    }

    public static string SanitizePackageName(string value)
    {
        return Regex.Replace(value ?? string.Empty, @"[^a-zA-Z\-_.\d]", "").TrimEnd('.').ToLower(CultureInfo.InvariantCulture);
    }

    public static string SanitizeNamespace(string value)
    {
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Regex.Replace(Regex.Replace(value ?? string.Empty, @"[^a-zA-Z\d]", ""), @"^\d+", ""));
    }
}
