// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor
{
    public partial class FileUtil
    {
        internal static void ReplaceText(string path, params string[] input)
        {
            path = NiceWinPath(path);
            string[] data = File.ReadAllLines(path);

            for (int i = 0; i < input.Length; i += 2)
            {
                for (int q = 0; q < data.Length; ++q)
                {
                    data[q] = data[q].Replace(input[i], input[i + 1]);
                }
            }

            File.WriteAllLines(path, data);
        }

        internal static bool ReplaceTextRegex(string path, params string[] input)
        {
            bool res = false;
            path = NiceWinPath(path);
            string[] data = File.ReadAllLines(path);

            for (int i = 0; i < input.Length; i += 2)
            {
                for (int q = 0; q < data.Length; ++q)
                {
                    string s = data[q];
                    data[q] = Regex.Replace(s, input[i], input[i + 1]);

                    if (s != (string)data[q])
                        res = true;
                }
            }

            File.WriteAllLines(path, data);
            return res;
        }

        internal static bool AppendTextAfter(string path, string find, string append)
        {
            bool res = false;
            path = NiceWinPath(path);
            var data = new List<string>(File.ReadAllLines(path));

            for (int q = 0; q < data.Count; ++q)
            {
                if (data[q].Contains(find))
                {
                    data.Insert(q + 1, append);
                    res = true;
                    break;
                }
            }

            File.WriteAllLines(path, data.ToArray());
            return res;
        }

        internal static void CopyDirectoryRecursive(string source, string target)
        {
            CopyDirectoryRecursive(source, target, false, false);
        }

        internal static void CopyDirectoryRecursiveIgnoreMeta(string source, string target)
        {
            CopyDirectoryRecursive(source, target, false, true);
        }

        internal static void CopyDirectoryRecursive(string source, string target, bool overwrite)
        {
            CopyDirectoryRecursive(source, target, overwrite, false);
        }

        internal static void CopyDirectory(string source, string target, bool overwrite)
        {
            CopyDirectoryFiltered(source, target, overwrite, f => true, false);
        }

        internal static void CopyDirectoryRecursive(string source, string target, bool overwrite, bool ignoreMeta)
        {
            CopyDirectoryRecursiveFiltered(source, target, overwrite, ignoreMeta ? @"\.meta$" : null);
        }

        internal static void CopyDirectoryRecursiveForPostprocess(string source, string target, bool overwrite)
        {
            CopyDirectoryRecursiveFiltered(source, target, overwrite, @".*/\.+|\.meta$");
        }

        internal static void CopyDirectoryRecursiveFiltered(string source, string target, bool overwrite, string regExExcludeFilter)
        {
            CopyDirectoryFiltered(source, target, overwrite, regExExcludeFilter, true);
        }

        internal static void CopyDirectoryFiltered(string source, string target, bool overwrite, string regExExcludeFilter, bool recursive)
        {
            Regex exclude = null;
            try
            {
                if (regExExcludeFilter != null)
                    exclude = new Regex(regExExcludeFilter);
            }
            catch (ArgumentException)
            {
                Debug.Log("CopyDirectoryRecursive: Pattern '" + regExExcludeFilter + "' is not a correct Regular Expression. Not excluding any files.");
                return;
            }

            Func<string, bool> includeCallback = file => (exclude == null || !exclude.IsMatch(file));

            CopyDirectoryFiltered(source, target, overwrite, includeCallback, recursive);
        }

        internal static void CopyDirectoryFiltered(string source, string target, bool overwrite, Func<string, bool> includeCallback, bool recursive)
        {
            // Check if the target directory exists, if not, create it.
            if (Directory.Exists(target) == false)
            {
                Directory.CreateDirectory(target);
                overwrite = false; // no reason to perform this on subdirs
            }

            // Copy each file into the new directory.
            foreach (string fi in Directory.GetFiles(source))
            {
                if (!includeCallback(fi))
                    continue;

                string fname = Path.GetFileName(fi);
                string targetfname = Path.Combine(target, fname);

                UnityFileCopy(fi, targetfname, overwrite);
            }

            if (!recursive)
                return;

            // Copy each subdirectory recursively.
            foreach (string di in Directory.GetDirectories(source))
            {
                if (!includeCallback(di))
                    continue;

                string fname = Path.GetFileName(di);

                CopyDirectoryFiltered(Path.Combine(source, fname), Path.Combine(target, fname), overwrite, includeCallback, recursive);
            }
        }

        internal static void UnityDirectoryDelete(string path)
        {
            UnityDirectoryDelete(path, false);
        }

        internal static void UnityDirectoryDelete(string path, bool recursive)
        {
            Directory.Delete(NiceWinPath(path), recursive);
        }

        // set the System.IO.FileAttributes.Normal recursively on all files in target_dir
        internal static void UnityDirectoryRemoveReadonlyAttribute(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, System.IO.FileAttributes.Normal);
            }

            foreach (string dir in dirs)
            {
                UnityDirectoryRemoveReadonlyAttribute(dir);
            }
        }

        internal static void MoveFileIfExists(string src, string dst)
        {
            if (File.Exists(src))
            {
                DeleteFileOrDirectory(dst);
                MoveFileOrDirectory(src, dst);
                File.SetLastWriteTime(dst, DateTime.Now);
            }
        }

        internal static void CopyFileIfExists(string src, string dst, bool overwrite)
        {
            if (File.Exists(src))
            {
                UnityFileCopy(src, dst, overwrite);
            }
        }

        internal static void UnityFileCopy(string from, string to, bool overwrite)
        {
            File.Copy(NiceWinPath(from), NiceWinPath(to), overwrite);
        }

        internal static string NiceWinPath(string unityPath)
        {
            // IO functions do not like mixing of \ and / slashes, esp. for windows network paths (\\path)
            return Application.platform == RuntimePlatform.WindowsEditor ? unityPath.Replace("/", @"\") : unityPath;
        }

        internal static string UnityGetFileNameWithoutExtension(string path)
        {
            // this is because on Windows \\ means network path, in unity all \ are converted to /
            // network paths become // and Path class functions think it's the same as /
            return Path.GetFileNameWithoutExtension(path.Replace("//", @"\\")).Replace(@"\\", "//");
        }

        internal static string UnityGetFileName(string path)
        {
            // this is because on Windows \\ means network path, in unity all \ are converted to /
            // network paths become // and Path class functions think it's the same as /
            return Path.GetFileName(path.Replace("//", @"\\")).Replace(@"\\", "//");
        }

        internal static string UnityGetDirectoryName(string path)
        {
            // this is because on Windows \\ means network path, in unity all \ are converted to /
            // network paths become // and Path class functions think it's the same as /
            return Path.GetDirectoryName(path.Replace("//", @"\\")).Replace(@"\\", "//");
        }

        internal static void UnityFileCopy(string from, string to)
        {
            UnityFileCopy(from, to, false);
        }

        internal static void CreateOrCleanDirectory(string dir)
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
        }

        internal static string RemovePathPrefix(string fullPath, string prefix)
        {
            var partsOfFull = fullPath.Split(Path.DirectorySeparatorChar);
            var partsOfPrefix = prefix.Split(Path.DirectorySeparatorChar);
            int index = 0;

            if (partsOfFull[0] == string.Empty)
                index = 1;

            while (index < partsOfFull.Length
                   && index < partsOfPrefix.Length
                   && partsOfFull[index] == partsOfPrefix[index])
                ++index;

            if (index == partsOfFull.Length)
                return "";

            return string.Join(Path.DirectorySeparatorChar.ToString(),
                partsOfFull, index, partsOfFull.Length - index);
        }

        internal static string CombinePaths(params string[] paths)
        {
            if (null == paths)
                return string.Empty;
            return string.Join(Path.DirectorySeparatorChar.ToString(), paths);
        }

        internal static List<string> GetAllFilesRecursive(string path)
        {
            List<string> files = new List<string>();
            WalkFilesystemRecursively(path,
                (p) => { files.Add(p); },
                (p) => { return true; });
            return files;
        }

        internal static void WalkFilesystemRecursively(string path,
            Action<string> fileCallback,
            Func<string, bool> directoryCallback)
        {
            foreach (string file in Directory.GetFiles(path))
                fileCallback(file);
            foreach (string subdir in Directory.GetDirectories(path))
            {
                if (directoryCallback(subdir))
                    WalkFilesystemRecursively(subdir, fileCallback, directoryCallback);
            }
        }

        internal static long GetDirectorySize(string path)
        {
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            long filesSize = 0;
            foreach (var file in files)
            {
                var info = new FileInfo(file);
                filesSize += info.Length;
            }
            return filesSize;
        }
    }
}
