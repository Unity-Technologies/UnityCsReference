// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class MppmLog
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            s_Enabled = MigrationUtility.ShouldEnableMultiplayerPlayMode() && (bool)UnityEngine.Debug.GetDiagnosticSwitch("MultiplayerPlayModeLogs").value;
        }

        const string k_ToolsPrefix = "[MultiplayerPlaymode]";

        private static bool s_Enabled;

        public static bool AreLogsEnabled() => s_Enabled;

        public static void Debug(object message)
        {
            if (!AreLogsEnabled())
                return;

            UnityEngine.Debug.Log($"{k_ToolsPrefix}: {message}");
        }

        public static void Warning(object message) => UnityEngine.Debug.LogWarning($"{k_ToolsPrefix}: {message}");

        public static void Error(object message) => UnityEngine.Debug.LogError($"{k_ToolsPrefix}: {message}");
    }
}
