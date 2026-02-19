using System;
using System.Collections.Generic;

namespace Unity.Scripting
{
    // *** TODO (code reload): this interface is in LifecycleManagement assembly temporarily until we find out how we want to organize the ScriptingCore functionality
    internal static class Debug
    {
        public static IScriptingCoreDebug? scriptingCoreDebug { get; set; }

        public static bool IsDiagnosticSwitchEnabled(string name) => scriptingCoreDebug?.IsDiagnosticSwitchEnabled(name) ?? false;
        public static void Log(string message) => scriptingCoreDebug?.Log(message);
        public static void LogError(string message) => scriptingCoreDebug?.LogError(message);
        public static void LogError(string message, Exception exception) => scriptingCoreDebug?.LogError($"{message}\nException:\n{exception}");
        public static void Assert(bool condition) => scriptingCoreDebug?.Assert(condition);
        public static void AssertMsg(bool condition, string message) => scriptingCoreDebug?.AssertMsg(condition, message);
        public static bool RunAssemblyLoadContextLeakDetection(List<IntPtr> assemblyLoadContextWeakHandles) => scriptingCoreDebug != null && scriptingCoreDebug.RunAssemblyLoadContextLeakDetection(assemblyLoadContextWeakHandles);
    }
}
