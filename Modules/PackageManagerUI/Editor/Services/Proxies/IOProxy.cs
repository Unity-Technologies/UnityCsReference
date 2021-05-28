// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    using NiceIO;

    internal class IOProxy
    {
        // Need to re-create this method since Unity's FileUtil equivalent (with overwrite) is internal only.
        // From: https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp
        public virtual void DirectoryCopy(string sourcePath, string destinationPath, bool makeWritable = false, Action<string, float> progressCallback = null)
        {
            if (!DirectoryExists(destinationPath))
                CreateDirectory(destinationPath);

            //Now Create all of the directories
            foreach (var dir in DirectoryGetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                var path = dir.Replace(sourcePath, destinationPath);
                if (!DirectoryExists(path))
                    CreateDirectory(path);
            }

            //Copy all the files & Replaces any files with the same name
            var files = DirectoryGetFiles(sourcePath, "*", SearchOption.AllDirectories);
            float count = 0;
            foreach (var source in files)
            {
                var dest = source.Replace(sourcePath, destinationPath);
                progressCallback?.Invoke(source, count / files.Length);
                CopyFile(source, dest, true);
                if (makeWritable)
                    MakeFileWritable(dest, true);
                count++;
            }
        }

        public virtual ulong DirectorySizeInBytes(string path)
        {
            return DirectoryGetFiles(path, "*", SearchOption.AllDirectories).
                Aggregate<string, ulong>(0, (current, file) => current + GetFileSize(file));
        }

        public virtual void RemovePathAndMeta(string path, bool removeEmptyParent = false)
        {
            while (true)
            {
                if (DirectoryExists(path))
                    DeleteDirectory(path);

                if (FileExists(path + ".meta"))
                    DeleteFile(path + ".meta");

                if (removeEmptyParent)
                {
                    var parent = GetParentDirectory(path);
                    if (DirectoryExists(parent) && IsDirectoryEmpty(parent))
                    {
                        path = parent;
                        continue;
                    }
                }
                break;
            }
        }

        public virtual string PathsCombine(params string[] components)
        {
            return components.Where(s => !string.IsNullOrEmpty(s)).
                Aggregate((path1, path2) => new NPath(path1).Combine(path2).ToString(SlashMode.Native));
        }

        public virtual string CurrentDirectory => NPath.CurrentDirectory.ToString(SlashMode.Native);

        public virtual string GetDirectoryName(string path) => new NPath(path).FileName;

        public virtual string GetParentDirectory(string directoryPath) => new NPath(directoryPath).Parent.ToString(SlashMode.Native);

        public virtual bool IsDirectoryEmpty(string directoryPath) => new NPath(directoryPath).Contents().Length == 0;

        public virtual bool DirectoryExists(string directoryPath) => new NPath(directoryPath).DirectoryExists();

        public virtual string[] DirectoryGetDirectories(string directoryPath, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
            => new NPath(directoryPath).Directories(searchPattern, searchOption == SearchOption.AllDirectories).Select(p => p.ToString(SlashMode.Native)).ToArray();

        public virtual string[] DirectoryGetFiles(string directoryPath, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
            => new NPath(directoryPath).Files(searchPattern, searchOption == SearchOption.AllDirectories).Select(p => p.ToString(SlashMode.Native)).ToArray();

        public virtual void CreateDirectory(string directoryPath) => new NPath(directoryPath).CreateDirectory();

        public virtual void DeleteDirectory(string directoryPath) => new NPath(directoryPath).DeleteIfExists();

        public virtual void MakeFileWritable(string filePath, bool writable)
        {
            var npath = new NPath(filePath);
            var attributes = npath.Attributes;

            if (writable && (attributes & FileAttributes.ReadOnly) != 0)
                npath.Attributes &= ~FileAttributes.ReadOnly;

            if (!writable && (attributes & FileAttributes.ReadOnly) == 0)
                npath.Attributes |= FileAttributes.ReadOnly;
        }

        public virtual void CopyFile(string sourceFileName, string destFileName, bool overwrite)
        {
            var npath = new NPath(destFileName);
            if (!overwrite && npath.FileExists())
                return;

            new NPath(sourceFileName).Copy(npath);
        }

        public virtual ulong GetFileSize(string filePath)
        {
            try
            {
                return (ulong)new NPath(filePath).GetFileSize();
            }
            catch (Exception e)
            {
                Debug.Log($"Cannot get file size for {filePath} : {e.Message}");
                return 0;
            }
        }

        public virtual string GetFileName(string filePath) => new NPath(filePath).FileName;

        public virtual void DeleteFile(string filePath) => new NPath(filePath).DeleteIfExists();

        public virtual bool FileExists(string filePath) => new NPath(filePath).FileExists();

        public virtual byte[] FileReadAllBytes(string filePath) => new NPath(filePath).ReadAllBytes();

        public virtual string FileReadAllText(string filePath) => new NPath(filePath).ReadAllText();

        public virtual void FileWriteAllBytes(string filePath, byte[] bytes) => new NPath(filePath).WriteAllBytes(bytes);

        public virtual void FileWriteAllText(string filePath, string contents) => new NPath(filePath).WriteAllText(contents);
    }
}
