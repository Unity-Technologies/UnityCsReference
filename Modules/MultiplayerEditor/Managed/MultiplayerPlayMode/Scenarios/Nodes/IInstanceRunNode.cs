// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal interface IInstanceRunNode : IConnectableNode
    {
        public bool IsRunning();

        public static void PrintReceivedLog(string identifier, Color color, string message, LogType logType = LogType.Log)
        {
            Debug.LogFormat(logType, LogOption.NoStacktrace, null, "{0}", CalculateLogString(identifier, color, message));
        }

        public static string CalculateLogString(string identifier, Color color, string message)
        {
            var colorHex = $"#{ColorUtility.ToHtmlStringRGB(color)}";
            return $"<color={colorHex}>[{identifier}]</color> {message}";
        }
    }
}
