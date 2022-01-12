// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class IOUtils
    {
        public static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            // Remove additional special characters that Unity doesn't like
            foreach (char c in "/:?<>*|\\~")
                name = name.Replace(c, '_');
            return name.Trim();
        }

        public static string EscapeBackslashes(this string path)
        {
            return path.Replace(@"\", @"\\");
        }
    }
}
