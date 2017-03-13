// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    internal partial class DebugLogHandler : ILogHandler
    {
        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            Internal_Log(logType, string.Format(format, args), context);
        }

        public void LogException(Exception exception, Object context)
        {
            Internal_LogException(exception, context);
        }
    }
}
