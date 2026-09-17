// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor
{
    public static partial class ConsoleWindowUtility
    {
        [AutoStaticsCleanupOnCodeReload]
        // Subscribers attach through their own lifecycle and re-subscribe after a code reload, so the
        // cleared invocation list refills itself.
        [IgnoreForUAL0015("Event whose subscribers re-register through their own lifecycle after a code reload")]
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
