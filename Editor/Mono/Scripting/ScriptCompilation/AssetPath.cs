// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Utils;
using SystemPath = System.IO.Path;

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class AssetPath
    {
        public static readonly char Separator = '/';

        public static string GetFullPath(string path)
        {
            return ReplaceSeparators(SystemPath.GetFullPath(path.NormalizePath()));
        }

        public static string Combine(string path1, string path2)
        {
            return ReplaceSeparators(SystemPath.Combine(path1, path2));
        }

        public static bool IsPathRooted(string path)
        {
            return SystemPath.IsPathRooted(path.NormalizePath());
        }

        public static string GetFileName(string path)
        {
            return SystemPath.GetFileName(path.NormalizePath());
        }

        public static string GetExtension(string path)
        {
            return SystemPath.GetExtension(path.NormalizePath());
        }

        public static string GetDirectoryName(string path)
        {
            return ReplaceSeparators(SystemPath.GetDirectoryName(path.NormalizePath()));
        }

        public static string ReplaceSeparators(string path)
        {
            return path.Replace('\\', Separator);
        }

        public static string GetAssemblyNameWithoutExtension(string assemblyName)
        {
            if (AssetPath.GetExtension(assemblyName) == ".dll")
                return SystemPath.GetFileNameWithoutExtension(assemblyName.NormalizePath());

            return SystemPath.GetFileName(assemblyName.NormalizePath());
        }
    }
}
