// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/EditorMonoConsole.h")]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    internal partial class LogEntry
    {
        public string message;
        public string file;
        public int line;
        public int column;
        public int mode;
        public int instanceID;
        public int identifier;
        internal static extern void RemoveLogEntriesByMode(int mode);
    }

    // used to pull log messages from Cpp side to mono window
    // All functions marked internal may not be called unless you call StartGettingEntries and EndGettingEntries
    [StaticAccessor("GetEditorMonoConsole()", StaticAccessorType.Dot)]
    [NativeHeader("Editor/Mono/LogEntries.bindings.h")]
    internal sealed class LogEntries
    {
        public static extern void RowGotDoubleClicked(int index);
        public static extern void OpenFileOnSpecificLineAndColumn(string filePath, int line, int column);

        [FreeFunction]
        public static extern string GetStatusText();

        public static extern int GetStatusMask();

        // returns total line count
        public static extern int StartGettingEntries();

        public static extern int consoleFlags { get; set; }
        public static extern void SetConsoleFlag(int bit, bool value);
        public static extern void SetFilteringText(string filteringText);
        public static extern string GetFilteringText();

        public static extern void EndGettingEntries();

        public static extern int GetCount();

        public static extern void GetCountsByType(ref int errorCount, ref int warningCount, ref int logCount);

        public static extern void GetLinesAndModeFromEntryInternal(int row, int numberOfLines, ref int mask, [In, Out] ref string outString);

        [FreeFunction]
        public static extern bool GetEntryInternal(int row, [Out] LogEntry outputEntry);

        [FreeFunction(ThrowsException = true)]
        public static extern int GetEntryCount(int row);

        public static extern void Clear();

        public static extern int GetStatusViewErrorIndex();

        public static extern void ClickStatusBar(int count);

        public static extern void AddMessageWithDoubleClickCallback(LogEntry outputEntry);
    }
}
