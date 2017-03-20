// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    public class Logger : ILogger
    {
        private const string kNoTagFormat = "{0}";
        private const string kTagFormat = "{0}: {1}";

        private Logger()
        {}

        public Logger(ILogHandler logHandler)
        {
            this.logHandler = logHandler;
            this.logEnabled = true;
            this.filterLogType = LogType.Log;
        }

        public ILogHandler logHandler { get; set; }

        public bool logEnabled { get; set; }

        public LogType filterLogType { get; set; }

        public bool IsLogTypeAllowed(LogType logType)
        {
            if (logEnabled)
            {
                if (logType == LogType.Exception)
                    return true;

                if (filterLogType != LogType.Exception)
                    return (logType <= filterLogType);
            }
            return false;
        }

        private static string GetString(object message)
        {
            return message != null ? message.ToString() : "Null";
        }

        public void Log(LogType logType, object message)
        {
            if (IsLogTypeAllowed(logType))
                logHandler.LogFormat(logType, null, kNoTagFormat, new object[] {GetString(message)});
        }

        public void Log(LogType logType, object message, Object context)
        {
            if (IsLogTypeAllowed(logType))
                logHandler.LogFormat(logType, context, kNoTagFormat, new object[] {GetString(message)});
        }

        public void Log(LogType logType, string tag, object message)
        {
            if (IsLogTypeAllowed(logType))
                logHandler.LogFormat(logType, null, kTagFormat, new object[] {tag, GetString(message)});
        }

        public void Log(LogType logType, string tag, object message, Object context)
        {
            if (IsLogTypeAllowed(logType))
                logHandler.LogFormat(logType, context, kTagFormat, new object[] {tag, GetString(message)});
        }

        public void Log(object message)
        {
            if (IsLogTypeAllowed(LogType.Log))
                logHandler.LogFormat(LogType.Log, null, kNoTagFormat, new object[] {GetString(message)});
        }

        public void Log(string tag, object message)
        {
            if (IsLogTypeAllowed(LogType.Log))
                logHandler.LogFormat(LogType.Log, null, kTagFormat, new object[] {tag, GetString(message)});
        }

        public void Log(string tag, object message, Object context)
        {
            if (IsLogTypeAllowed(LogType.Log))
                logHandler.LogFormat(LogType.Log, context, kTagFormat, new object[] {tag, GetString(message)});
        }

        public void LogWarning(string tag, object message)
        {
            if (IsLogTypeAllowed(LogType.Warning))
                logHandler.LogFormat(LogType.Warning, null, kTagFormat, new object[] {tag, GetString(message)});
        }

        public void LogWarning(string tag, object message, Object context)
        {
            if (IsLogTypeAllowed(LogType.Warning))
                logHandler.LogFormat(LogType.Warning, context, kTagFormat, new object[] {tag, GetString(message)});
        }

        public void LogError(string tag, object message)
        {
            if (IsLogTypeAllowed(LogType.Error))
                logHandler.LogFormat(LogType.Error, null, kTagFormat, new object[] {tag, GetString(message)});
        }

        public void LogError(string tag, object message, Object context)
        {
            if (IsLogTypeAllowed(LogType.Error))
                logHandler.LogFormat(LogType.Error, context, kTagFormat, new object[] {tag, GetString(message)});
        }

        public void LogFormat(LogType logType, string format, params object[] args)
        {
            if (IsLogTypeAllowed(logType))
                logHandler.LogFormat(logType, null, format, args);
        }

        public void LogException(Exception exception)
        {
            if (logEnabled)
                logHandler.LogException(exception, null);
        }

        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            if (IsLogTypeAllowed(logType))
                logHandler.LogFormat(logType, context, format, args);
        }

        public void LogException(Exception exception, Object context)
        {
            if (logEnabled)
                logHandler.LogException(exception, context);
        }
    }
}
