// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    static class ConsoleWindowHelper_Internal
    {
        static readonly int k_LogIdentifier = "GraphToolsFoundation".GetHashCode();

        static int LogTypeOptionsToMode(LogType logType, LogOption logOptions)
        {
            ConsoleWindow.Mode mode;

            if (logType == LogType.Log) // LogType::Log
                mode = ConsoleWindow.Mode.ScriptingLog;
            else if (logType == LogType.Warning) // LogType::Warning
                mode = ConsoleWindow.Mode.ScriptingWarning;
            else if (logType == LogType.Error) // LogType::Error
                mode = ConsoleWindow.Mode.ScriptingError;
            else if (logType == LogType.Exception) // LogType::Exception
                mode = ConsoleWindow.Mode.ScriptingException;
            else
                mode = ConsoleWindow.Mode.ScriptingAssertion;

            if (logOptions == LogOption.NoStacktrace)
                mode |= ConsoleWindow.Mode.DontExtractStacktrace;

            return (int)mode;
        }

        public static void SetEntryDoubleClickedDelegate(Action<string, int> doubleClickedCallback)
        {
            ConsoleWindow.entryWithManagedCallbackDoubleClicked += CallEntryDoubleClickedCallback;

            void CallEntryDoubleClickedCallback(LogEntry logEntry) => doubleClickedCallback(logEntry.file, logEntry.instanceID);
        }

        public static void LogSticky(string message, string file, LogType logType, LogOption logOptions, int instanceId)
        {
            int mode = LogTypeOptionsToMode(logType, logOptions) | (int)ConsoleWindow.Mode.StickyError;

            LogEntries.AddMessageWithDoubleClickCallback(new LogEntry
            {
                message = message,
                file = file,
                mode = mode,
                identifier = k_LogIdentifier,
                instanceID = instanceId,
            });
        }

        public static void RemoveLogEntries()
        {
            Debug.RemoveLogEntriesByIdentifier(k_LogIdentifier);
        }
    }
}
