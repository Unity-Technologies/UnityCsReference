// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Build.Analysis
{
    internal interface IBuildAnalysisFileSystem
    {
        string ReadAllText(string path);
        void WriteAllText(string path, string contents);
    }

    internal sealed class BuildAnalysisFileSystem : IBuildAnalysisFileSystem
    {
        public string ReadAllText(string path)
        {
            return System.IO.File.ReadAllText(path);
        }

        public void WriteAllText(string path, string contents)
        {
            System.IO.File.WriteAllText(path, contents);
        }
    }
}
