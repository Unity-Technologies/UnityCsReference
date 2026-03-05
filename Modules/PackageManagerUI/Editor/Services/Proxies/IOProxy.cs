// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    using NiceIO;

    internal interface IIOProxy : IService
    {
        void DirectoryCopy(string sourcePath, string destinationPath, bool makeWritable = false, Action<string, float> progressCallback = null);
        ulong DirectorySizeInBytes(string path);
        void RemovePathAndMeta(string path, bool removeEmptyParent = false);

        string CurrentDirectory { get; }

        bool IsDirectoryEmpty(string directoryPath);
        bool DirectoryExists(string directoryPath);
        string[] GetFiles(string directoryPath, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
        void CreateDirectory(string directoryPath);
        void DeleteDirectory(string directoryPath);
        string GetProjectDirectory();
        bool IsSamePackageDirectory(string a, string b);
        void MakeFileWritable(string filePath, bool writable);
        void CopyFile(string sourceFileName, string destFileName, bool overwrite);
        ulong GetFileSize(string filePath);
        void DeleteIfExists(string filePath);
        bool FileExists(string filePath);
        byte[] FileReadAllBytes(string filePath);
        string FileReadAllText(string filePath);
        void FileWriteAllBytes(string filePath, byte[] bytes);
        void FileWriteAllText(string filePath, string contents);
        string GetUniqueTempPathInProject();
        void SetFileAttributes(string file, FileAttributes attributes);
        FileAttributes GetFileAttributes(string file);
        void Move(string sourceDirName, string destinationDirName);
    }
    // Proxy class IO operations. Operations that are affected by or will affect the file system should go here.
    // Stateless functions like path and file name manipulations should go to IOUtils.
    [ExcludeFromCodeCoverage]
    internal class IOProxy : BaseService<IIOProxy>, IIOProxy
    {
        // Need to re-create this method since Unity's FileUtil equivalent (with overwrite) is internal only.
        // From: https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp
        public void DirectoryCopy(string sourcePath, string destinationPath, bool makeWritable = false, Action<string, float> progressCallback = null)
        {
            if (!DirectoryExists(destinationPath))
                CreateDirectory(destinationPath);

            // Now Create all the directories
            foreach (var dir in GetSubDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                var path = dir.Replace(sourcePath, destinationPath);
                if (!DirectoryExists(path))
                    CreateDirectory(path);
            }

            // Copy all the files & Replaces any files with the same name
            var files = GetFiles(sourcePath, "*", SearchOption.AllDirectories);
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

        public ulong DirectorySizeInBytes(string path)
        {
            ulong size = 0;
            foreach (var file in GetFiles(path, "*", SearchOption.AllDirectories))
                size += GetFileSize(file);
            return size;
        }

        public void RemovePathAndMeta(string path, bool removeEmptyParent = false)
        {
            while (true)
            {
                if (DirectoryExists(path))
                    DeleteDirectory(path);

                DeleteIfExists(path + ".meta");

                if (removeEmptyParent)
                {
                    var parent = IOUtils.GetParentDirectory(path);
                    if (DirectoryExists(parent) && IsDirectoryEmpty(parent))
                    {
                        path = parent;
                        continue;
                    }
                }
                break;
            }
        }

        public string CurrentDirectory => NPath.CurrentDirectory.ToString(SlashMode.Native);

        public bool IsDirectoryEmpty(string directoryPath) => new NPath(directoryPath).Contents().Length == 0;

        public bool DirectoryExists(string directoryPath) => new NPath(directoryPath).DirectoryExists();

        public string[] GetSubDirectories(string directoryPath, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
            => new NPath(directoryPath).Directories(searchPattern, searchOption == SearchOption.AllDirectories).SelectToNewArray(p => p.ToString(SlashMode.Native));

        public string[] GetFiles(string directoryPath, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
            => new NPath(directoryPath).Files(searchPattern, searchOption == SearchOption.AllDirectories).SelectToNewArray(p => p.ToString(SlashMode.Native));

        public void CreateDirectory(string directoryPath) => new NPath(directoryPath).CreateDirectory();

        public void DeleteDirectory(string directoryPath) => new NPath(directoryPath).DeleteIfExists();

        private NPath GetPackageAbsoluteDirectory(string relativePath)
        {
            var path = new NPath(relativePath);
            return path.IsRelative ?  path.MakeAbsolute(new NPath(IOUtils.PathsCombine(GetProjectDirectory(), "Packages"))) : path;
        }

        // The virtual keyword is needed for unit tests
        public virtual string GetProjectDirectory() => IOUtils.GetParentDirectory(Application.dataPath);

        public bool IsSamePackageDirectory(string a, string b) => GetPackageAbsoluteDirectory(a) == GetPackageAbsoluteDirectory(b);

        public void MakeFileWritable(string filePath, bool writable)
        {
            var path = new NPath(filePath);
            var attributes = path.Attributes;

            if (writable && (attributes & FileAttributes.ReadOnly) != 0)
                path.Attributes &= ~FileAttributes.ReadOnly;

            if (!writable && (attributes & FileAttributes.ReadOnly) == 0)
                path.Attributes |= FileAttributes.ReadOnly;
        }

        public void CopyFile(string sourceFileName, string destFileName, bool overwrite)
        {
            var path = new NPath(destFileName);
            if (!overwrite && path.FileExists())
                return;
            new NPath(sourceFileName).Copy(path);
        }

        public ulong GetFileSize(string filePath)
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

        public void DeleteIfExists(string filePath) => new NPath(filePath).DeleteIfExists();

        public bool FileExists(string filePath) => new NPath(filePath).FileExists();

        public byte[] FileReadAllBytes(string filePath) => new NPath(filePath).ReadAllBytes();

        public string FileReadAllText(string filePath) => new NPath(filePath).ReadAllText();

        public void FileWriteAllBytes(string filePath, byte[] bytes) => new NPath(filePath).WriteAllBytes(bytes);

        public void FileWriteAllText(string filePath, string contents) => new NPath(filePath).WriteAllText(contents);
        public string GetUniqueTempPathInProject() => FileUtil.GetUniqueTempPathInProject();

        public void SetFileAttributes(string file, FileAttributes attributes) => new NPath(file).Attributes = attributes;
        public FileAttributes GetFileAttributes(string file) => new NPath(file).Attributes;
        public void Move(string sourceDirName, string destinationDirName) => new NPath(sourceDirName).Move(destinationDirName);
    }
}
