// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Utils;
using File = System.IO.File;
using SystemPath = System.IO.Path;


namespace UnityEditor.Scripting.ScriptCompilation
{
    static class AssetPath
    {
        public static readonly char Separator = '/';

        /// <summary>
        /// Compares two paths
        /// type of slashes is ignored, so different path separators is allowed
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool ComparePaths(string left, string right)
        {
            var lengthDiff = left.Length - right.Length;
            if (lengthDiff > 1 || lengthDiff < -1)
                return false;

            for (int i = 0; i < left.Length || i < right.Length; i++)
            {
                char leftCharLower = ' ';
                char rightCharLower= ' ';
                if (i < left.Length)
                {
                    leftCharLower = Utility.FastToLower(left[i]);
                    leftCharLower = Utility.IsPathSeparator(leftCharLower) ? ' ' : leftCharLower;
                }

                if (i < right.Length)
                {
                    rightCharLower = Utility.FastToLower(right[i]);
                    rightCharLower = Utility.IsPathSeparator(rightCharLower) ? ' ' : rightCharLower;
                }

                if (leftCharLower != rightCharLower)
                    return false;
            }

            return true;
        }

        public static string GetFullPath(string path)
        {
            return ReplaceSeparators(SystemPath.GetFullPath(path.NormalizePath()));
        }

        public static string Combine(params string[] paths)
        {
            return ReplaceSeparators(Paths.Combine(paths));
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
            int length = path.Length;

            var chars = new char[length];

            for (int i = 0; i < length; ++i)
            {
                if (path[i] == '\\')
                    chars[i] = Separator;
                else
                    chars[i] = path[i];
            }

            return new string(chars);
        }

        public static bool Exists(string path)
        {
            return File.Exists(path);
        }

        public static string GetAssemblyNameWithoutExtension(string assemblyName)
        {
            if (AssetPath.GetExtension(assemblyName) == ".dll")
                return SystemPath.GetFileNameWithoutExtension(assemblyName.NormalizePath());

            return SystemPath.GetFileName(assemblyName.NormalizePath());
        }
    }
}
