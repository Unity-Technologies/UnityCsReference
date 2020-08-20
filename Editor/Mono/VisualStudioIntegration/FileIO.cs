// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using System.Text;

namespace UnityEditor.VisualStudioIntegration
{
    interface IFileIO
    {
        bool Exists(string fileName);

        string ReadAllText(string fileName);
        void WriteAllText(string fileName, string content);
        void Copy(string source, string destination, bool overwrite = false);
    }

    class FileIOProvider : IFileIO
    {
        public bool Exists(string fileName)
        {
            return File.Exists(fileName);
        }

        public void Copy(string source, string destination, bool overwrite = false)
        {
            File.Copy(source, destination, overwrite);
        }

        public string ReadAllText(string fileName)
        {
            return File.ReadAllText(fileName);
        }

        public void WriteAllText(string fileName, string content)
        {
            File.WriteAllText(fileName, content, Encoding.UTF8);
        }
    }

    internal interface IDirectoryIO
    {
        void CreateDirectory(string path);
        bool Exists(string path);
        void Delete(string path, bool recursive = false);
        string[] GetFiles(string outputPath, string s, SearchOption topDirectoryOnly);
    }

    class DirectoryIOProvider : IDirectoryIO
    {
        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public bool Exists(string path)
        {
            return Directory.Exists(path);
        }

        public void Delete(string path, bool recursive)
        {
            Directory.Delete(path, recursive);
        }

        public string[] GetFiles(string outputPath, string searchPattern, SearchOption searchOption)
        {
            return Directory.GetFiles(outputPath, searchPattern, searchOption);
        }
    }
}
