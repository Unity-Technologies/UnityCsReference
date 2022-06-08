// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine.Bindings;
using System.Reflection;

namespace UnityEditor
{
    // To keep in sync with the enum in LogAssert.h
    [Flags]
    internal enum LogMessageFlags : int
    {
        kNoLogMessageFlags = 0,
        kError = 1 << 0, // Message describes an error.
        kAssert = 1 << 1, // Message describes an assertion failure.
        kLog = 1 << 2, // Message is a general log message.
        kFatal = 1 << 4, // Message describes a fatal error, and that the program should now exit.
        kAssetImportError = 1 << 6, // Message describes an error generated during asset importing.
        kAssetImportWarning = 1 << 7, // Message describes a warning generated during asset importing.
        kScriptingError = 1 << 8, // Message describes an error produced by script code.
        kScriptingWarning = 1 << 9, // Message describes a warning produced by script code.
        kScriptingLog = 1 << 10, // Message describes a general log message produced by script code.
        kScriptCompileError = 1 << 11, // Message describes an error produced by the script compiler.
        kScriptCompileWarning = 1 << 12, // Message describes a warning produced by the script compiler.
        kStickyLog = 1 << 13, // Message is 'sticky' and should not be removed when the user manually clears the console window.
        kMayIgnoreLineNumber = 1 << 14, // The scripting runtime should skip annotating the log callstack with file and line information.
        kReportBug = 1 << 15, // When used with kFatal, indicates that the log system should launch the bug reporter.
        kDisplayPreviousErrorInStatusBar = 1 << 16, // The message before this one should be displayed at the bottom of Unity's main window, unless there are no messages before this one.
        kScriptingException = 1 << 17, // Message describes an exception produced by script code.
        kDontExtractStacktrace = 1 << 18, // Stacktrace extraction should be skipped for this message.
        kScriptingAssertion = 1 << 21, // The message describes an assertion failure in script code.
        kStacktraceIsPostprocessed = 1 << 22, // The stacktrace has already been postprocessed and does not need further processing.
        kIsCalledFromManaged = 1 << 23, // The message is being called from managed code.

        FromEditor = kDontExtractStacktrace | kMayIgnoreLineNumber | kIsCalledFromManaged,

        DebugLog = kScriptingLog | FromEditor,
        DebugWarning = kScriptingWarning | FromEditor,
        DebugError = kScriptingError | FromEditor,
        DebugException = kScriptingException | FromEditor,
    }

    internal static class LogMessageFlagsExtensions
    {
        public static bool IsInfo(this LogMessageFlags flags)
        {
            return (flags & (LogMessageFlags.kLog | LogMessageFlags.kScriptingLog)) != 0;
        }
        public static bool IsWarning(this LogMessageFlags flags)
        {
            return (flags & (LogMessageFlags.kScriptCompileWarning | LogMessageFlags.kScriptingWarning | LogMessageFlags.kAssetImportWarning)) != 0;
        }
        public static bool IsError(this LogMessageFlags flags)
        {
            return (flags & (LogMessageFlags.kFatal | LogMessageFlags.kAssert | LogMessageFlags.kError | LogMessageFlags.kScriptCompileError |
                            LogMessageFlags.kScriptingError | LogMessageFlags.kAssetImportError | LogMessageFlags.kScriptingAssertion | LogMessageFlags.kScriptingException)) != 0;
        }
    }

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
        public int globalLineIndex;
        public int callstackTextStartUTF8;
        public int callstackTextStartUTF16;
        internal static extern void RemoveLogEntriesByMode(int mode);
    }

    [NativeHeader("Editor/Src/EditorMonoConsole.h")]
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct LogEntryUTF8StringView
    {
        public readonly IntPtr utf8Ptr;
        public readonly int utf8Length;

        public LogEntryUTF8StringView(IntPtr ptr, int lengthUtf8)
        {
            utf8Ptr = ptr;
            utf8Length = lengthUtf8;
        }

        public unsafe LogEntryUTF8StringView(byte* ptr, int lengthUtf8)
        {
            utf8Ptr = new IntPtr(ptr);
            utf8Length = lengthUtf8;
        }
    }

    [NativeHeader("Editor/Src/EditorMonoConsole.h")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct LogEntryStruct
    {
        public LogEntryUTF8StringView message;
        public LogEntryUTF8StringView callstack;
        public LogEntryUTF8StringView timestamp;

        public LogEntryUTF8StringView file;
        public int line;
        public int column;

        public LogMessageFlags mode;
        public int instanceID;
        public int identifier;
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

        [FreeFunction]
        internal static extern string GetCallstackFormattedSignatureInternal(MethodBase methodInfo);

        [FreeFunction(ThrowsException = true)]
        public static extern int GetEntryCount(int row);

        public static extern void Clear();

        public static extern int GetStatusViewErrorIndex();

        public static extern void ClickStatusBar(int count);

        public static extern void AddMessageWithDoubleClickCallback(LogEntry outputEntry);

        [ThreadSafe]
        public extern static unsafe void AddMessagesImpl(void* messagesBuffer, int messagesBufferLength);

        internal static extern int GetEntryRowIndex(int globalIndex);
    }
}
