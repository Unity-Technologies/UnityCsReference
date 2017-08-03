// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using System.IO;
using System.Linq;

namespace UnityEditorInternal
{
    internal static class FileMirroring
    {
        public static void MirrorFile(string from, string to)
        {
            MirrorFile(from, to, CanSkipCopy);
        }

        public static void MirrorFile(string from, string to, Func<string, string, bool> comparer)
        {
            if (comparer(from, to))
                return;

            if (!File.Exists(from))
            {
                DeleteFileOrDirectory(to);
                return;
            }

            var parentDir = Path.GetDirectoryName(to);
            if (!Directory.Exists(parentDir))
                Directory.CreateDirectory(parentDir);

            File.Copy(from, to, true);
        }

        public static void MirrorFolder(string from, string to)
        {
            MirrorFolder(from, to, CanSkipCopy);
        }

        public static void MirrorFolder(string from, string to, Func<string, string, bool> comparer)
        {
            from = Path.GetFullPath(from);
            to = Path.GetFullPath(to);

            if (!Directory.Exists(from))
            {
                if (Directory.Exists(to))
                    Directory.Delete(to, true);
                return;
            }
            if (!Directory.Exists(to))
                Directory.CreateDirectory(to);

            var toFileEntries = Directory.GetFileSystemEntries(to).Select(s => StripPrefix(s, to));
            var fromFileEntries = Directory.GetFileSystemEntries(from).Select(s => StripPrefix(s, from));

            var shouldDeletes = toFileEntries.Except(fromFileEntries);
            foreach (var shouldDelete in shouldDeletes)
                DeleteFileOrDirectory(Path.Combine(to, shouldDelete));

            foreach (var file in fromFileEntries)
            {
                var absFrom = Path.Combine(from, file);
                var absTo = Path.Combine(to, file);

                var fromType = FileEntryTypeFor(absFrom);
                var toType = FileEntryTypeFor(absTo);

                if (fromType == FileEntryType.File && toType == FileEntryType.Directory)
                    DeleteFileOrDirectory(absTo);

                if (fromType == FileEntryType.Directory)
                {
                    if (toType == FileEntryType.File)
                        DeleteFileOrDirectory(absTo);

                    if (toType != FileEntryType.Directory)
                        Directory.CreateDirectory(absTo);

                    MirrorFolder(absFrom, absTo);
                }

                if (fromType == FileEntryType.File)
                    MirrorFile(absFrom, absTo, comparer);
            }
        }

        static void DeleteFileOrDirectory(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                return;
            }
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }

        static string StripPrefix(string s, string prefix)
        {
            return s.Substring(prefix.Length + 1);
        }

        enum FileEntryType
        {
            File,
            Directory,
            NotExisting
        }

        static FileEntryType FileEntryTypeFor(string fileEntry)
        {
            if (File.Exists(fileEntry))
                return FileEntryType.File;
            if (Directory.Exists(fileEntry))
                return FileEntryType.Directory;
            return FileEntryType.NotExisting;
        }

        public static bool CanSkipCopy(string from, string to)
        {
            bool noFrom = !File.Exists(from);
            bool noTo = !File.Exists(to);
            // Early out: true if neither files exist, false if only one exists
            if (noFrom || noTo)
                return noFrom && noTo;
            return AreFilesIdentical(from, to);
        }

        static bool AreFilesIdentical(string filePath1, string filePath2)
        {
            using (var file = File.OpenRead(filePath1))
                using (var file2 = File.OpenRead(filePath2))
                {
                    if (file.Length != file2.Length)
                        return false;

                    const int bufferSize = 0x10000;
                    int count;
                    var buffer = new byte[bufferSize];
                    var buffer2 = new byte[bufferSize];

                    while ((count = file.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        file2.Read(buffer2, 0, buffer2.Length);

                        for (int i = 0; i < count; i++)
                            if (buffer[i] != buffer2[i])
                                return false;
                    }
                }

            return true;
        }
    }
}
