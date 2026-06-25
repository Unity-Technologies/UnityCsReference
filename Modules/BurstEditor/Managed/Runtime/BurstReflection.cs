// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UnityEditor.Modules.Burst.EditModeTests")]
namespace Unity.Burst.Editor
{
    internal static class BurstReflection
    {
        private static readonly object _lockObject = new object();
        private static FindExecuteMethodsResult _result;

        public static FindExecuteMethodsResult FindExecuteMethods()
        {
            lock (_lockObject)
            {
                if (_result == null)
                {
                    _result = new FindExecuteMethodsResult(
                        BurstCompiler.GetInspectorEntryPoints(),
                        new List<LogMessage>());
                }

                return _result;
            }
        }

        public sealed class FindExecuteMethodsResult
        {
            public readonly BurstCompileTarget[] CompileTargets;
            public readonly List<LogMessage> LogMessages;

            public FindExecuteMethodsResult(BurstCompileTarget[] compileTargets, List<LogMessage> logMessages)
            {
                CompileTargets = compileTargets;
                LogMessages = logMessages;
            }
        }

        public sealed class LogMessage
        {
            public readonly LogType LogType;
            public readonly string Message;
            public readonly Exception Exception;

            public LogMessage(LogType logType, string message)
            {
                LogType = logType;
                Message = message;
            }

            public LogMessage(Exception exception)
            {
                LogType = LogType.Exception;
                Exception = exception;
            }
        }

        public enum LogType
        {
            Warning,
            Exception,
        }
    }
}
