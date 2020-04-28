// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;

namespace UnityEditor.PackageManager.UI
{
    internal class IOProxy
    {
        // Need to re-create this method since Unity's FileUtil equivalent (with overwrite) is internal only.
        // From: https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp
        public virtual void DirectoryCopy(string sourcePath, string destinationPath, bool makeWritable = false)
        {
            Directory.CreateDirectory(destinationPath);

            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));

            //Copy all the files & Replaces any files with the same name
            foreach (string source in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                var dest = source.Replace(sourcePath, destinationPath);
                File.Copy(source, dest, true);
                if (makeWritable)
                    new FileInfo(dest).IsReadOnly = false;
            }
        }

        public virtual ulong DirectorySizeInBytes(string path)
        {
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            ulong sizeInBytes = 0;
            foreach (var file in files)
            {
                var info = new FileInfo(file);
                sizeInBytes += (ulong)info.Length;
            }
            return sizeInBytes;
        }

        public virtual void RemovePathAndMeta(string path, bool removeEmptyParent = false)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            if (File.Exists(path + ".meta"))
                File.Delete(path + ".meta");
            if (removeEmptyParent)
            {
                var parent = Directory.GetParent(path);
                if (parent.GetDirectories().Length == 0 && parent.GetFiles().Length == 0)
                    RemovePathAndMeta(parent.ToString(), removeEmptyParent);
            }
        }

        public virtual bool DirectoryExists(string directoryPath)
        {
            return Directory.Exists(directoryPath);
        }

        public virtual string[] DirectoryGetFiles(string directoryPath)
        {
            return Directory.GetFiles(directoryPath);
        }

        public virtual string[] DirectoryGetFiles(string directoryPath, string searchPattern)
        {
            return Directory.GetFiles(directoryPath, searchPattern);
        }

        public virtual string[] DirectoryGetFiles(string directoryPath, string searchPattern, SearchOption searchOption)
        {
            return Directory.GetFiles(directoryPath, searchPattern, searchOption);
        }

        public virtual DirectoryInfo DirectoryGetParent(string directoryPath)
        {
            return Directory.GetParent(directoryPath);
        }

        public virtual void CreateDirectory(string directoryPath)
        {
            Directory.CreateDirectory(directoryPath);
        }

        public virtual void DeleteDirectory(string directoryPath)
        {
            Directory.Delete(directoryPath);
        }

        public virtual void DirectoryDelete(string directoryPath, bool recursive)
        {
            Directory.Delete(directoryPath, recursive);
        }

        public virtual bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public virtual byte[] FileReadAllBytes(string filePath)
        {
            return File.ReadAllBytes(filePath);
        }

        public virtual string FileReadAllText(string filePath)
        {
            return File.ReadAllText(filePath);
        }

        public virtual void FileCopy(string sourcePath, string destinationPath, bool overwrite)
        {
            File.Copy(sourcePath, destinationPath, overwrite);
        }

        public virtual void FileWriteAllBytes(string destinationPath, byte[] bytes)
        {
            File.WriteAllBytes(destinationPath, bytes);
        }

        public virtual void FileDelete(string filePath)
        {
            File.Delete(filePath);
        }
    }
}
