// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    public static class ConsoleWindowUtility
    {
        public static event Action consoleLogsChanged;

        public static void GetConsoleLogCounts(out int error, out int warn, out int log)
        {
            int outError = 0;
            int outWarn = 0;
            int outLog = 0;

            LogEntries.GetCountsByType(ref outError, ref outWarn, ref outLog);

            error = outError;
            warn = outWarn;
            log = outLog;
        }

        internal static void Internal_CallLogsHaveChanged()
        {
            consoleLogsChanged?.Invoke();
        }
    }
}
