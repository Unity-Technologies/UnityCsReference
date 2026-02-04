// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using UnityEngine.Networking.PlayerConnection;
using UnityEditor.Networking.PlayerConnection;
using System.Text;

namespace UnityEditor
{
    internal class PlayerConnectionLogReceiver : ScriptableSingleton<PlayerConnectionLogReceiver>
    {
        static readonly Guid logMessageId = new Guid("394ada03-8ba0-4f26-b001-1a6cdeb05a62");
        static readonly Guid cleanLogMessageId = new Guid("3ded2dda-cdf2-46d8-a3f6-01741741e7a9");
        static readonly Guid logMessageIdMultiple = new Guid("5970345b-a9ef-448e-9706-aa42fb3e68e1");
        static readonly Guid cleanLogMessageIdMultiple = new Guid("b86c7bd8-e37d-4b1c-9a63-72f4b5d8112d");
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
                        EditorConnection.instance.Unregister(cleanLogMessageIdMultiple, LogMessageMultiple);
                        break;

                    case ConnectionState.FullLog:
                        EditorConnection.instance.Unregister(logMessageId, LogMessage);
                        EditorConnection.instance.Unregister(logMessageIdMultiple, LogMessageMultiple);
                        break;
                }
                state = value;
                switch (state)
                {
                    case ConnectionState.CleanLog:
                        EditorConnection.instance.Register(cleanLogMessageId, LogMessage);
                        EditorConnection.instance.Register(cleanLogMessageIdMultiple, LogMessageMultiple);
                        break;

                    case ConnectionState.FullLog:
                        EditorConnection.instance.Register(logMessageId, LogMessage);
                        EditorConnection.instance.Register(logMessageIdMultiple, LogMessageMultiple);
                        break;
                }
                EditorPrefs.SetInt(prefsKey, (int)state);
            }
        }

        [ThreadStatic]
        static System.Text.StringBuilder s_LogBuilder;

        void LogMessage(MessageEventArgs messageEventArgs)
        {
            if (s_LogBuilder == null)
                s_LogBuilder = new System.Text.StringBuilder(1024);
            else
                s_LogBuilder.Clear();

            var logType = (LogType)messageEventArgs.data[0];
            if (!Enum.IsDefined(typeof(LogType), logType))
                logType = LogType.Log;
            var oldStackTraceType = Application.GetStackTraceLogType(logType);

            // We don't want stack traces from editor code in player log messages.
            Application.SetStackTraceLogType(logType, StackTraceLogType.None);
            var name = ConnectionUIHelper.GetPlayerNameFromId(messageEventArgs.playerId);
            var t = ConnectionUIHelper.GetPlayerType(ProfilerDriver.GetConnectionIdentifier(messageEventArgs.playerId));

            s_LogBuilder.Append("<i>");
            s_LogBuilder.Append(t);
            s_LogBuilder.Append(" \"");
            s_LogBuilder.Append(name);
            s_LogBuilder.Append("\"</i> ");
            ReadOnlySpan<byte> utf8String = messageEventArgs.data.AsSpan<byte>(4);
            s_LogBuilder.Append(System.Text.Encoding.UTF8.GetString(utf8String));

            Debug.unityLogger.Log(logType, s_LogBuilder.ToString());
            Application.SetStackTraceLogType(logType, oldStackTraceType);
        }

        unsafe void LogMessageMultiple(MessageEventArgs messageEventArgs)
        {
            var payloadLength = messageEventArgs.data.Length;

            if (payloadLength <= 0) return;

            var name = ConnectionUIHelper.GetPlayerNameFromId(messageEventArgs.playerId);
            var t = ConnectionUIHelper.GetPlayerType(ProfilerDriver.GetConnectionIdentifier(messageEventArgs.playerId));

            var messagePrefix = $"<i>{t} \"{name}\"</i> ";
            var prefixUtf8Bytes = Encoding.UTF8.GetBytes(messagePrefix);

            fixed (byte* payloadPtr = messageEventArgs.data.AsSpan<byte>())
            fixed (byte* prefixUtf8Ptr = prefixUtf8Bytes)
            {
                static UTF8StringView ReadString(byte* payload, ref int offset)
                {
                    var bytesLength = BitConverter.ToInt32(new ReadOnlySpan<byte>(payload + offset, 4));
                    offset += 4;

                    if (bytesLength == 0)
                        return default;
                    if (bytesLength < 0)
                        throw new Exception("Received corrupted message");

                    var utf8StringPtr = payload + offset;
                    offset += bytesLength;
                    return new UTF8StringView(utf8StringPtr, bytesLength);
                }

                static LogMessageFlags FlagsFromType(LogType type)
                {
                    switch (type)
                    {
                        case LogType.Warning: return LogMessageFlags.DebugWarning;
                        case LogType.Error: return LogMessageFlags.DebugError;
                        case LogType.Assert: return LogMessageFlags.DebugAssert;
                        case LogType.Exception: return LogMessageFlags.DebugException;
                    }
                    return LogMessageFlags.DebugLog;
                }

                for (var offset = 0; offset < payloadLength;)
                {
                    var logType = (LogType)BitConverter.ToInt32(new ReadOnlySpan<byte>(payloadPtr + offset, 4));
                    offset += 4;

                    var message = ReadString(payloadPtr, ref offset);
                    var timestamp = ReadString(payloadPtr, ref offset);
                    var stacktrace = ReadString(payloadPtr, ref offset);

                    var log = new LogEntryStruct
                    {
                        messagePrefix = new UTF8StringView(prefixUtf8Ptr, prefixUtf8Bytes.Length),
                        message = message,
                        timestamp = timestamp,
                        callstack = stacktrace,
                        mode = FlagsFromType(logType) | LogMessageFlags.kStacktraceIsPostprocessed
                    };
                    ConsoleWindow.AddMessage(ref log);
                    Console.WriteLine(messagePrefix + message.ToString());
                }
            }
        }
    }
}
