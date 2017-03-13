// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    public interface ILogHandler
    {
        void LogFormat(LogType logType, Object context, string format, params object[] args);

        void LogException(Exception exception, Object context);
    }
}
