// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using File = System.IO.File;
using SystemPath = System.IO.Path;

namespace Unity.ProjectAuditor.Editor.Utils
{
    internal static class PathUtils
    {
        public const char Separator = '/';

        static readonly char k_DirectorySeparatorChar = SystemPath.DirectorySeparatorChar;
        static readonly char k_AltDirectorySeparatorChar = SystemPath.AltDirectorySeparatorChar;
        static readonly char k_VolumeSeparatorChar = SystemPath.VolumeSeparatorChar;

        public static string Combine(params string[] parts)
        {
            return string.Join(Char.ToString(Separator), parts);
        }

        public static string Combine(string path1, string path2)
        {
            return ReplaceSeparators(SystemPath.Combine(path1, path2));
        }

        public static string GetDirectoryName(string path)
        {
            return ReplaceSeparators(SystemPath.GetDirectoryName(path));
        }

        public static string GetFullPath(string path)
        {
            return ReplaceSeparators(SystemPath.GetFullPath(path));
        }

        public static int GetExtensionIndexFromPath(string path)
        {
            var length = path.Length;

            if (length == 0)
                return 0;

            var num = length;
            while (--num >= 0)
            {
                var c = path[num];
                if (c == '.')
                {
                    if (num != length - 1)
                    {
                        return num;
                    }

                    return length - 1;
                }

                if (c == k_DirectorySeparatorChar || c == k_AltDirectorySeparatorChar || c == k_VolumeSeparatorChar)
                {
                    return length - 1;
                }
            }
            return length - 1;
        }

        public static int GetFilenameIndexFromPath(string path)
        {
            var length = path.Length;
            var num = length;
            while (--num >= 0)
            {
                var c = path[num];
                if (c == k_DirectorySeparatorChar || c == k_AltDirectorySeparatorChar || c == k_VolumeSeparatorChar)
                {
                    return num + 1;
                }
            }
            return 0;
        }

        public static string ReplaceSeparators(string path)
        {
            var length = path.Length;

            var chars = new char[length];

            for (var i = 0; i < length; ++i)
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

        public static string ReplaceInvalidChars(string path)
        {
            return path.Replace('|', '_').Replace(":", string.Empty);
        }

        public static string[] Split(string path)
        {
            return path.Split(Separator);
        }
    }
}
