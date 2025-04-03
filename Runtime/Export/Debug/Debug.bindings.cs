// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Internal;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // Must be kept in sync with enum in IntegrityCheck.h
    [NativeHeader("Runtime/Diagnostics/IntegrityCheck.h")]
    public enum IntegrityCheckLevel
    {
        Low = 1,
        Medium = 2,
        High = 3
    }

    // Must be kept in sync with enum in Validation.h
    [NativeHeader("Runtime/Diagnostics/Validation.h")]
    public enum ValidationLevel
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    [NativeHeader("Runtime/Export/Debug/Debug.bindings.h")]
    internal sealed partial class DebugLogHandler
    {
        [ThreadAndSerializationSafe]
        internal static extern void Internal_Log(LogType level, LogOption options, string msg, Object obj);
        [ThreadAndSerializationSafe]
        internal static extern void Internal_LogException(Exception ex, Object obj);
    }

    [NativeHeader("Runtime/Export/Debug/Debug.bindings.h")]
    [NativeHeader("Runtime/Diagnostics/IntegrityCheck.h")]
    [NativeHeader("Runtime/Diagnostics/Validation.h")]
    // Class containing methods to ease debugging while developing a game.
    public partial class Debug
    {
        // This logger is used by CallOverridenDebugHandler in case of an exception occurring
        // in the default s_Logger. This logger doesn't override ILogger.logHandler
        internal static readonly ILogger s_DefaultLogger = new Logger(new DebugLogHandler());

        internal static ILogger s_Logger = new Logger(new DebugLogHandler());
        public static ILogger unityLogger => s_Logger;

        [ExcludeFromDocs]
        public static void DrawLine(Vector3 start, Vector3 end, Color color , float duration)
        {
            bool depthTest = true;
            DrawLine(start, end, color, duration, depthTest);
        }

        [ExcludeFromDocs]
        public static void DrawLine(Vector3 start, Vector3 end, Color color)
        {
            bool depthTest = true;
            float duration = 0.0f;
            DrawLine(start, end, color, duration, depthTest);
        }

        [ExcludeFromDocs]
        public static void DrawLine(Vector3 start, Vector3 end)
        {
            bool depthTest = true;
            float duration = 0.0f;
            Color color = Color.white;
            DrawLine(start, end, color, duration, depthTest);
        }

        // Draws a line from the /point/ start to /end/ with color for a duration of time and with or without depth testing. If duration is 0 then the line is rendered 1 frame.
        [FreeFunction("DebugDrawLine", IsThreadSafe = true)]
        public static extern void DrawLine(Vector3 start, Vector3 end, [DefaultValue("Color.white")] Color color, [DefaultValue("0.0f")] float duration, [DefaultValue("true")] bool depthTest);

        [ExcludeFromDocs]
        public static void DrawRay(Vector3 start, Vector3 dir, Color color , float duration)
        {
            bool depthTest = true;
            DrawRay(start, dir, color, duration, depthTest);
        }

        [ExcludeFromDocs]
        public static void DrawRay(Vector3 start, Vector3 dir, Color color)
        {
            bool depthTest = true;
            float duration = 0.0f;
            DrawRay(start, dir, color, duration, depthTest);
        }

        [ExcludeFromDocs]
        public static void DrawRay(Vector3 start, Vector3 dir)
        {
            bool depthTest = true;
            float duration = 0.0f;
            Color color = Color.white;
            DrawRay(start, dir, color, duration, depthTest);
        }

        // Draws a line from /start/ to /start/ + /dir/ with color for a duration of time and with or without depth testing. If duration is 0 then the line is rendered 1 frame.
        public static void DrawRay(Vector3 start, Vector3 dir, [DefaultValue("Color.white")]  Color color , [DefaultValue("0.0f")]  float duration , [DefaultValue("true")]  bool depthTest)
        {
            DrawLine(start, start + dir, color, duration, depthTest);
        }

        // Pauses the editor.
        [FreeFunction("PauseEditor")]
        public static extern void Break();

        // Breaks into the attached debugger, if present
        public static extern void DebugBreak();

        [ThreadSafe]
        public static extern unsafe int ExtractStackTraceNoAlloc(byte* buffer, int bufferMax, string projectFolder);

        // Logs /message/ to the Unity Console.
        public static void Log(object message) { unityLogger.Log(LogType.Log, message); }

        // Logs /message/ to the Unity Console.
        public static void Log(object message, Object context)
        {
            unityLogger.Log(LogType.Log, message, context);
        }

        public static void LogFormat(string format, params object[] args)
        {
            unityLogger.LogFormat(LogType.Log, format, args);
        }

        public static void LogFormat(Object context, string format, params object[] args)
        {
            unityLogger.LogFormat(LogType.Log, context, format, args);
        }

        public static void LogFormat(LogType logType, LogOption logOptions, Object context, string format, params object[] args)
        {
            var l = unityLogger.logHandler as DebugLogHandler;
            if (l == null)
                unityLogger.LogFormat(logType, context, format, args);
            else if (unityLogger.IsLogTypeAllowed(logType))
                l.LogFormat(logType, logOptions, context, format, args);
        }

        // A variant of Debug.Log that logs an error message to the console.
        public static void LogError(object message) { unityLogger.Log(LogType.Error, message); }

        // A variant of Debug.Log that logs an error message to the console.
        public static void LogError(object message, Object context) { unityLogger.Log(LogType.Error, message, context); }

        public static void LogErrorFormat(string format, params object[] args)
        {
            unityLogger.LogFormat(LogType.Error, format, args);
        }

        public static void LogErrorFormat(Object context, string format, params object[] args)
        {
            unityLogger.LogFormat(LogType.Error, context, format, args);
        }

        internal static void LogError(string message, string fileName, int lineNumber, int columnNumber)
        {
            LogCompilerError(message, fileName, lineNumber, columnNumber);
        }

        internal static void LogWarning(string message, string fileName, int lineNumber, int columnNumber)
        {
            LogCompilerWarning(message, fileName, lineNumber, columnNumber);
        }

        internal static void LogInfo(string message, string fileName, int lineNumber, int columnNumber)
        {
            LogInformation(message, fileName, lineNumber, columnNumber);
        }

        [ThreadAndSerializationSafe]
        internal static extern void LogCompilerMessage(string message, string fileName, int lineNumber, int columnNumber, bool forEditor, bool isError, int identifier, int instanceId);
        [ThreadAndSerializationSafe]
        private static extern void LogCompilerWarning(string message, string fileName, int lineNumber, int columnNumber);
        [ThreadAndSerializationSafe]
        private static extern void LogCompilerError(string message, string fileName, int lineNumber, int columnNumber);
        [ThreadAndSerializationSafe]
        private static extern void LogInformation(string message, string fileName, int lineNumber, int columnNumber);

        // Clears errors from the developer console.
        public static extern void ClearDeveloperConsole();

        // Prevent the developer console from opening in developer builds
        public static extern bool developerConsoleEnabled { get; set; }

        // Opens or closes developer console.
        public static extern bool developerConsoleVisible { get; set; }

        // A variant of Debug.Log that logs an error message from an exception to the console.
        public static void LogException(Exception exception) { unityLogger.LogException(exception, null); }

        // A variant of Debug.Log that logs an error message to the console.
        public static void LogException(Exception exception, Object context) { unityLogger.LogException(exception, context); }

        [ThreadAndSerializationSafe]
        internal static extern void LogPlayerBuildError(string message, string file, int line, int column);

        // A variant of Debug.Log that logs a warning message to the console.
        public static void LogWarning(object message) { unityLogger.Log(LogType.Warning, message); }

        // A variant of Debug.Log that logs a warning message to the console.
        public static void LogWarning(object message, Object context) { unityLogger.Log(LogType.Warning, message, context); }

        public static void LogWarningFormat(string format, params object[] args)
        {
            unityLogger.LogFormat(LogType.Warning, format, args);
        }

        public static void LogWarningFormat(UnityEngine.Object context, string format, params object[] args)
        {
            unityLogger.LogFormat(LogType.Warning, context, format, args);
        }

        [Conditional(Assertions.Assert.UNITY_ASSERTIONS)]
        public static void Assert(bool condition) { if (!condition) unityLogger.Log(LogType.Assert, (object)"Assertion failed"); }

        [Conditional(Assertions.Assert.UNITY_ASSERTIONS)]
        public static void Assert(bool condition, Object context) { if (!condition) unityLogger.Log(LogType.Assert, (object)"Assertion failed", context); }

        [Conditional(Assertions.Assert.UNITY_ASSERTIONS)]
        public static void Assert(bool condition, object message) { if (!condition) unityLogger.Log(LogType.Assert, message); }

        //Same as Assert (bool, object) but can't deprecate because the script updater won't work
        [Conditional(Assertions.Assert.UNITY_ASSERTIONS)]
        public static void Assert(bool condition, string message) { if (!condition) unityLogger.Log(LogType.Assert, (object)message); }

        [Conditional(Assertions.Assert.UNITY_ASSERTIONS)]
        public static void Assert(bool condition, object message, Object context) { if (!condition) unityLogger.Log(LogType.Assert, message, context); }

        //Same as Assert (bool, object, Object) but can't deprecate because the script updater won't work
        [Conditional(Assertions.Assert.UNITY_ASSERTIONS)]
        public static void Assert(bool condition, string message, Object context) { if (!condition) unityLogger.Log(LogType.Assert, (object)message, context); }

        [Conditional(Assertions.Assert.UNITY_ASSERTIONS)]
        public static void AssertFormat(bool condition, string format, params object[] args) { if (!condition) unityLogger.LogFormat(LogType.Assert, format, args); }

        [Conditional(Assertions.Assert.UNITY_ASSERTIONS)]
        public static void AssertFormat(bool condition, Object context, string format, params object[] args) { if (!condition) unityLogger.LogFormat(LogType.Assert, context, format, args); }

        [Conditional(Assertions.Assert.UNITY_ASSERTIONS)]
        public static void LogAssertion(object message) { unityLogger.Log(LogType.Assert, message); }

        [Conditional(Assertions.Assert.UNITY_ASSERTIONS)]
        public static void LogAssertion(object message, Object context) { unityLogger.Log(LogType.Assert, message, context); }

        [Conditional(Assertions.Assert.UNITY_ASSERTIONS)]
        public static void LogAssertionFormat(string format, params object[] args) { unityLogger.LogFormat(LogType.Assert, format, args); }

        [Conditional(Assertions.Assert.UNITY_ASSERTIONS)]
        public static void LogAssertionFormat(Object context, string format, params object[] args) { unityLogger.LogFormat(LogType.Assert, context, format, args); }

        // In the Build Settings dialog there is a check box called "Development Build".
        public static extern bool isDebugBuild { get; }

        [NativeThrows]
        internal static extern DiagnosticSwitch[] diagnosticSwitches { get; }


        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.TextCoreTextEngineModule","UnityEngine.IMGUIModule")]
        internal static DiagnosticSwitch GetDiagnosticSwitch(string name)
        {
            foreach (var diagnosticSwitch in diagnosticSwitches)
            {
                if (diagnosticSwitch.name == name)
                    return diagnosticSwitch;
            }

            throw new ArgumentException($"Could not find DiagnosticSwitch named {name}");
        }

        [RequiredByNativeCode]
        internal static bool CallOverridenDebugHandler(Exception exception, Object obj)
        {
            if (unityLogger.logHandler is DebugLogHandler)
            {
                return false;
            }

            try
            {
                unityLogger.LogException(exception, obj);
            }
            catch (Exception ex)
            {
                // If s_Logger.logHandler.LogException throws an error it would make this method fail and would
                // generate an infinite loop of exceptions, so we cannot let this method fail.
                // So we fallback to the default logger.
                s_DefaultLogger.LogError($"Invalid exception thrown from custom {unityLogger.logHandler.GetType()}.LogException(). Message: {ex}", obj);
                return false;
            }

            return true;
        }

        [RequiredByNativeCode]
        internal static bool IsLoggingEnabled()
        {
            if (unityLogger.logHandler is DebugLogHandler)
            {
                return unityLogger.logEnabled;
            }

            return s_DefaultLogger.logEnabled;
        }

        internal static extern void LogSticky(int identifier, LogType logType, LogOption logOptions, string message, Object context = null);
        internal static extern void RemoveLogEntriesByIdentifier(int identifier);

        [NativeHeader("Runtime/Export/Debug/LogCapture.bindings.h")]
        [StructLayout(LayoutKind.Sequential)]
        public struct StartupLog
        {
            public long timestamp;
            public LogType logType;
            public string message;
        }

        [FreeFunction("RetrieveStartupLogs")]
        extern public static StartupLog[] RetrieveStartupLogs();

        [FreeFunction("CheckApplicationIntegrity")]
        public static extern string CheckIntegrity(IntegrityCheckLevel level);

        [FreeFunction("IsValidationLevelEnabled")]
        public static extern bool IsValidationLevelEnabled(ValidationLevel level);
    }
}
