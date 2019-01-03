// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    public interface ILogger : ILogHandler
    {
        ILogHandler logHandler { get; set; }

        bool logEnabled { get; set; }

        LogType filterLogType { get; set; }

        bool IsLogTypeAllowed(LogType logType);

        void Log(LogType logType, object message);

        void Log(LogType logType, object message, Object context);

        void Log(LogType logType, string tag, object message);

        void Log(LogType logType, string tag, object message, Object context);

        void Log(object message);

        void Log(string tag, object message);

        void Log(string tag, object message, Object context);

        void LogWarning(string tag, object message);

        void LogWarning(string tag, object message, Object context);

        void LogError(string tag, object message);

        void LogError(string tag, object message, Object context);

        void LogFormat(LogType logType, string format, params object[] args);

        void LogException(Exception exception);
    }
}
