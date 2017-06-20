// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Compilation
{
    public enum CompilerMessageType
    {
        Error = 0,
        Warning = 1
    }

    public struct CompilerMessage
    {
        public string message;
        public string file;
        public int line;
        public int column;
        public CompilerMessageType type;
    }
}
