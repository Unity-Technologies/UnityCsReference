// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor
{
    internal class FileOpenInfo : IFileOpenInfo
    {
        public string FilePath { get; set; }
        public int LineNumber { get; set; }

        public FileOpenInfo()
        {
            LineNumber = 1;
            FilePath = string.Empty;
        }
    }
}
