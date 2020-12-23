// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using UnityEngine;

namespace UnityEditor.Search.Providers
{
    static class LogProvider
    {
        struct LogEntry
        {
            public string id;
            public int lineNumber;
            public string msg;
            public string msgLowerCased;
            public LogType logType;
        }

        private const string type = "log";
        private const string displayName = "Logs";

        private static int s_LogIndex = 0;
        private static List<LogEntry> s_Logs = new List<LogEntry>();
        private static volatile bool s_Initialized = false;

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            var consoleLogPath = Application.consoleLogPath;
            if (string.IsNullOrEmpty(consoleLogPath) || !File.Exists(consoleLogPath))
                return null;

            var readConsoleLogThread = new Thread(() =>
            {
                using (var logStream = new FileStream(consoleLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(logStream))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        lock (s_Logs)
                            s_Logs.Add(CreateLogEntry(line));
                    }
                }
            });
            readConsoleLogThread.Start();

            return new SearchProvider(type, displayName)
            {
                active = false, // Still experimental
                priority = 210,
                filterId = type + ":",
                isExplicitProvider = true,

                fetchItems = (context, items, provider) => SearchLogs(context, provider),

                fetchDescription = (item, context) =>
                {
                    var logEntry = (LogEntry)item.data;
                    return $"{logEntry.lineNumber}: {logEntry.msg}";
                },

                fetchThumbnail = (item, context) =>
                {
                    var logEntry = (LogEntry)item.data;
                    switch (logEntry.logType)
                    {
                        case LogType.Log:
                            return (item.thumbnail = Icons.logInfo);
                        case LogType.Warning:
                            return (item.thumbnail = Icons.logWarning);
                        case LogType.Error:
                        case LogType.Assert:
                        case LogType.Exception:
                            return (item.thumbnail = Icons.logError);
                        default:
                            return null;
                    }
                }
            };
        }

        private static void HandleLog(string logString, string stackTrace, LogType type)
        {
            lock (s_Logs)
                s_Logs.Add(CreateLogEntry(logString, type));
        }

        private static LogEntry CreateLogEntry(string s, LogType logType = LogType.Log)
        {
            s = s.Trim();
            return new LogEntry
            {
                id = "__log_" + s_LogIndex,
                lineNumber = Interlocked.Increment(ref s_LogIndex),
                msg = s,
                msgLowerCased = s.ToLowerInvariant(),
                logType = logType
            };
        }

        #region SearchLogs
        private static IEnumerable<SearchItem> SearchLogs(SearchContext context, SearchProvider provider)
        {
            lock (s_Logs)
            {
                if (!s_Initialized)
                {
                    s_Initialized = true;
                    Application.logMessageReceived -= HandleLog;
                    Application.logMessageReceived += HandleLog;
                }
                for (int logIndex = 0; logIndex < s_Logs.Count; ++logIndex)
                    yield return SearchLogEntry(context, provider, s_Logs[logIndex]);
            }
        }

        #endregion

        #region SearchItemOptions
        private static SearchItem SearchLogEntry(SearchContext context, SearchProvider provider, LogEntry logEntry)
        {
            if (!SearchUtils.MatchSearchGroups(context, logEntry.msgLowerCased, true))
                return null;

            var logItem = provider.CreateItem(context, logEntry.id, ~logEntry.lineNumber, logEntry.msg, null, null, logEntry);
            logItem.options = SearchItemOptions.Ellipsis | SearchItemOptions.RightToLeft | SearchItemOptions.Highlight;
            return logItem;
        }

        #endregion

        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> ActionHandlers()
        {
            return new[]
            {
                new SearchAction(type, "copy", null, "Copy to the clipboard")
                {
                    handler = (item) => EditorGUIUtility.systemCopyBuffer = item.label.ToString(CultureInfo.InvariantCulture)
                },
                new SearchAction(type, "open", null, "Open console log file")
                {
                    handler = (item) => EditorUtility.RevealInFinder(Application.consoleLogPath)
                }
            };
        }
    }
}
