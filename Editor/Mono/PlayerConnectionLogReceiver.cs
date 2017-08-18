// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using UnityEngine.Networking.PlayerConnection;
using UnityEditor.Networking.PlayerConnection;

namespace UnityEditor
{
    internal class PlayerConnectionLogReceiver : ScriptableSingleton<PlayerConnectionLogReceiver>
    {
        static Guid logMessageId { get { return new Guid("394ada03-8ba0-4f26-b001-1a6cdeb05a62"); } }
        static Guid cleanLogMessageId { get { return new Guid("3ded2dda-cdf2-46d8-a3f6-01741741e7a9"); } }
        const string prefsKey = "PlayerConnectionLoggingState";

        internal enum ConnectionState
        {
            Disconnected,
            CleanLog,
            FullLog
        }

        [SerializeField]
        ConnectionState state = ConnectionState.Disconnected;

        void OnEnable()
        {
            State = (ConnectionState)EditorPrefs.GetInt(prefsKey, (int)ConnectionState.CleanLog);
        }

        internal ConnectionState State
        {
            get
            {
                return state;
            }
            set
            {
                if (state == value)
                    return;

                switch (state)
                {
                    case ConnectionState.CleanLog:
                        EditorConnection.instance.Unregister(cleanLogMessageId, LogMessage);
                        break;

                    case ConnectionState.FullLog:
                        EditorConnection.instance.Unregister(logMessageId, LogMessage);
                        break;
                }
                state = value;
                switch (state)
                {
                    case ConnectionState.CleanLog:
                        EditorConnection.instance.Register(cleanLogMessageId, LogMessage);
                        break;

                    case ConnectionState.FullLog:
                        EditorConnection.instance.Register(logMessageId, LogMessage);
                        break;
                }
                EditorPrefs.SetInt(prefsKey, (int)state);
            }
        }

        void LogMessage(MessageEventArgs messageEventArgs)
        {
            var body = messageEventArgs.data.Skip(4).ToArray();
            string text = System.Text.Encoding.UTF8.GetString(body);

            var logType = (LogType)messageEventArgs.data[0];
            if (!Enum.IsDefined(typeof(LogType), logType))
                logType = LogType.Log;
            var oldStackTraceType = Application.GetStackTraceLogType(logType);

            // We don't want stack traces from editor code in player log messages.
            Application.SetStackTraceLogType(logType, StackTraceLogType.None);

            string name = ProfilerDriver.GetConnectionIdentifier(messageEventArgs.playerId);

            text = "<i>" + name + "</i> " + text;
            Debug.unityLogger.Log(logType, text);
            Application.SetStackTraceLogType(logType, oldStackTraceType);
        }
    }
}
