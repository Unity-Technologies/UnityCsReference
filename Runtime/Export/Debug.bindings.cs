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

namespace UnityEngine
{
    [NativeHeader("Runtime/Export/Debug.bindings.h")]
    internal sealed partial class DebugLogHandler
    {
        [ThreadAndSerializationSafe]
        internal static extern void Internal_Log(LogType level, string msg, Object obj);
        [ThreadAndSerializationSafe]
        internal static extern void Internal_LogException(Exception exception, Object obj);
    }

    [NativeHeader("Runtime/Export/Debug.bindings.h")]
    // Class containing methods to ease debugging while developing a game.
    public partial class Debug
    {
        internal static ILogger s_Logger = new Logger(new DebugLogHandler());
        public static ILogger unityLogger
        {
            get { return s_Logger; }
        }


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
        [FreeFunction("DebugDrawLine")]
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

        public static void LogFormat(UnityEngine.Object context, string format, params object[] args)
        {
            unityLogger.LogFormat(LogType.Log, context, format, args);
        }

        // A variant of Debug.Log that logs an error message to the console.
        public static void LogError(object message) { unityLogger.Log(LogType.Error, message); }

        // A variant of Debug.Log that logs an error message to the console.
        public static void LogError(object message, Object context) { unityLogger.Log(LogType.Error, message, context); }

        public static void LogErrorFormat(string format, params object[] args)
        {
            unityLogger.LogFormat(LogType.Error, format, args);
        }

        public static void LogErrorFormat(UnityEngine.Object context, string format, params object[] args)
        {
            unityLogger.LogFormat(LogType.Error, context, format, args);
        }

        // Clears errors from the developer console.
        public static extern void ClearDeveloperConsole();

        // Opens or closes developer console.
        public static extern bool developerConsoleVisible { get; set; }

        // A variant of Debug.Log that logs an error message from an exception to the console.
        public static void LogException(Exception exception) { unityLogger.LogException(exception, null); }

        // A variant of Debug.Log that logs an error message to the console.
        public static void LogException(Exception exception, Object context) { unityLogger.LogException(exception, context); }

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
        [StaticAccessor("GetBuildSettings()", StaticAccessorType.Dot)]
        [NativeProperty(TargetType = TargetType.Field)]
        public static extern bool isDebugBuild { get; }

        [FreeFunction("DeveloperConsole_OpenConsoleFile")]
        internal static extern void OpenConsoleFile();

        internal static extern void GetDiagnosticSwitches(List<DiagnosticSwitch> results);

        [NativeThrows]
        internal static extern object GetDiagnosticSwitch(string name);

        [NativeThrows]
        internal static extern void SetDiagnosticSwitch(string name, object value, bool setPersistent);
    }
}
