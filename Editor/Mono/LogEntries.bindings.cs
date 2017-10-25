// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/EditorMonoConsole.h")]
    [NativeAsStruct]
    internal partial class LogEntry
    {
        internal static extern void LogToConsoleEx(LogEntry outputEntry);
        internal static extern void RemoveLogEntriesByMode(int mode);
    }
}
