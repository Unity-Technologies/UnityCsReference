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
    }

    class FileIOProvider : IFileIO
    {
        public bool Exists(string fileName)
        {
            return File.Exists(fileName);
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
}
