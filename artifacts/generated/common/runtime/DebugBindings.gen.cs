// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnityEngine
{


internal sealed partial class DebugLogHandler
{
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void Internal_Log (LogType level, string msg, [Writable] Object obj) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void Internal_LogException (Exception exception, [Writable] Object obj) ;

}

public sealed partial class Debug
{
    internal static ILogger s_Logger = new Logger(new DebugLogHandler());
    public static ILogger unityLogger
        {
            get { return s_Logger; }
        }
    
    
    
    public static void DrawLine (Vector3 start, Vector3 end, [uei.DefaultValue("Color.white")]  Color color , [uei.DefaultValue("0.0f")]  float duration , [uei.DefaultValue("true")]  bool depthTest ) {
        INTERNAL_CALL_DrawLine ( ref start, ref end, ref color, duration, depthTest );
    }

    [uei.ExcludeFromDocs]
    public static void DrawLine (Vector3 start, Vector3 end, Color color , float duration ) {
        bool depthTest = true;
        INTERNAL_CALL_DrawLine ( ref start, ref end, ref color, duration, depthTest );
    }

    [uei.ExcludeFromDocs]
    public static void DrawLine (Vector3 start, Vector3 end, Color color ) {
        bool depthTest = true;
        float duration = 0.0f;
        INTERNAL_CALL_DrawLine ( ref start, ref end, ref color, duration, depthTest );
    }

    [uei.ExcludeFromDocs]
    public static void DrawLine (Vector3 start, Vector3 end) {
        bool depthTest = true;
        float duration = 0.0f;
        Color color = Color.white;
        INTERNAL_CALL_DrawLine ( ref start, ref end, ref color, duration, depthTest );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DrawLine (ref Vector3 start, ref Vector3 end, ref Color color, float duration, bool depthTest);
    [uei.ExcludeFromDocs]
public static void DrawRay (Vector3 start, Vector3 dir, Color color , float duration ) {
    bool depthTest = true;
    DrawRay ( start, dir, color, duration, depthTest );
}

[uei.ExcludeFromDocs]
public static void DrawRay (Vector3 start, Vector3 dir, Color color ) {
    bool depthTest = true;
    float duration = 0.0f;
    DrawRay ( start, dir, color, duration, depthTest );
}

[uei.ExcludeFromDocs]
public static void DrawRay (Vector3 start, Vector3 dir) {
    bool depthTest = true;
    float duration = 0.0f;
    Color color = Color.white;
    DrawRay ( start, dir, color, duration, depthTest );
}

public static void DrawRay(Vector3 start, Vector3 dir, [uei.DefaultValue("Color.white")]  Color color , [uei.DefaultValue("0.0f")]  float duration , [uei.DefaultValue("true")]  bool depthTest ) { DrawLine(start, start + dir, color, duration, depthTest); }

    
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Break () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void DebugBreak () ;

    public static void Log(object message) { unityLogger.Log(LogType.Log, message); }
    
    
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
    
    
    public static void LogError(object message) { unityLogger.Log(LogType.Error, message); }
    
    
    public static void LogError(object message, Object context) { unityLogger.Log(LogType.Error, message, context); }
    
    
    public static void LogErrorFormat(string format, params object[] args)
        {
            unityLogger.LogFormat(LogType.Error, format, args);
        }
    
    
    public static void LogErrorFormat(UnityEngine.Object context, string format, params object[] args)
        {
            unityLogger.LogFormat(LogType.Error, context, format, args);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ClearDeveloperConsole () ;

    public extern static bool developerConsoleVisible
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public static void LogException(Exception exception) { unityLogger.LogException(exception, null); }
    
    
    public static void LogException(Exception exception, Object context) { unityLogger.LogException(exception, context); }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void LogPlayerBuildError (string message, string file, int line, int column) ;

    public static void LogWarning(object message) { unityLogger.Log(LogType.Warning, message); }
    
    
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
    
    
    [Conditional(Assertions.Assert.UNITY_ASSERTIONS)]
    public static void Assert(bool condition, string message) { if (!condition) unityLogger.Log(LogType.Assert, (object)message); }
    
    
    [Conditional(Assertions.Assert.UNITY_ASSERTIONS)]
    public static void Assert(bool condition, object message, Object context) { if (!condition) unityLogger.Log(LogType.Assert, message, context); }
    
    
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
    
    
    public extern static bool isDebugBuild
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void OpenConsoleFile () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void GetDiagnosticSwitches (List<DiagnosticSwitch> results) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  object GetDiagnosticSwitch (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetDiagnosticSwitch (string name, object value, bool setPersistent) ;

}

}
