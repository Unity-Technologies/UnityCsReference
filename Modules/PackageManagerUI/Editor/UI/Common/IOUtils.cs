// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using NiceIO;

namespace UnityEditor.PackageManager.UI.Internal
{
    // Utility class for file and path operations. Stateless functions go here.
    // Operations that are affected by or will affect the file system should go to IOProxy.
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

        public static string PathsCombine(params string[] components)
        {
            if (components == null || components.Length == 0)
                return string.Empty;
            var path = new NPath(components[0]);
            for (var i = 1; i < components.Length; i++)
                path = path.Combine(components[i]);
            return path.ToString(SlashMode.Native);
        }

        public static string GetFileName(string filePath) => new NPath(filePath).FileName;

        public static string GetParentDirectory(string path) => new NPath(path).Parent.ToString(SlashMode.Native);
    }
}
