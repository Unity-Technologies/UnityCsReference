// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class DebugUtils
    {
        public static class DebugFlags
        {
            public const string MppmAnalyticsDebug = "MultiplayerPlayModeAnalyticsDebug";
            public const string MppmForceMainWindow = "MultiplayerPlayModeForceMainWindow";
            public const string MppmAnalysisWindow = "MultiplayerPlayModeAnalysisWindow";
        }

        // TODO: Still used by the Multiplayer Play Mode package, remove once migration is complete.
        public static void Trace(string message,
            int skipFrames = 0,
            [CallerFilePath] string filepath = "",
            [CallerMemberName] string methodName = "",
            [CallerLineNumber] int line = 0)
        {
            MppmLog.Debug(message);
        }

        public static bool IsDebugFlagEnabled(string flagName)
        {
            try
            {
                return (bool)UnityEngine.Debug.GetDiagnosticSwitch(flagName).value;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
