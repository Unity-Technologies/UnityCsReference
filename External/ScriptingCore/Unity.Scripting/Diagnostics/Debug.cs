using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Unity.Scripting
{
    // *** TODO (code reload): this interface is in LifecycleManagement assembly temporarily until we find out how we want to organize the ScriptingCore functionality
    internal static class Debug
    {
        private static IScriptingCoreDebug _scriptingCoreDebug = new DefaultScriptingCoreDebug();
        public static IScriptingCoreDebug ScriptingCoreDebug
        {
            get => _scriptingCoreDebug;
            // can be null for unittest since they work in a non-nullable environment
            set => _scriptingCoreDebug = value ?? new DefaultScriptingCoreDebug();
        }

        public static bool IsDiagnosticSwitchEnabled(string name) => ScriptingCoreDebug.IsDiagnosticSwitchEnabled(name);
        public static void Log(string message) => ScriptingCoreDebug.Log(message);
        public static void LogError(string message) => ScriptingCoreDebug.LogError(message);
        public static void LogError(string message, Exception exception) => ScriptingCoreDebug.LogError($"{message}\nException:\n{exception}");
        public static void LogException(Exception exception) => ScriptingCoreDebug.LogException(exception);
        [Conditional("DEBUG")]
        public static void Assert(bool condition) => ScriptingCoreDebug.Assert(condition);
        [Conditional("DEBUG")]
        public static void AssertMsg(bool condition, string message) => ScriptingCoreDebug.AssertMsg(condition, message);
        public static bool RunAssemblyLoadContextLeakDetection(List<IntPtr> assemblyLoadContextWeakHandles) => ScriptingCoreDebug.RunAssemblyLoadContextLeakDetection(assemblyLoadContextWeakHandles);

        private class DefaultScriptingCoreDebug : IScriptingCoreDebug
        {
            public bool IsDiagnosticSwitchEnabled(string name) => false;

            public void Log(string message) => System.Diagnostics.Debug.WriteLine(message);
            public void LogError(string message) => System.Diagnostics.Debug.WriteLine(message);
            public void LogException(Exception exception) => System.Diagnostics.Debug.WriteLine("[Error] " + exception);
            public void LogExceptionFatal(Exception exception) => System.Diagnostics.Debug.WriteLine("[Fatal] " + exception);
            public void Assert(bool condition) => System.Diagnostics.Debug.Assert(condition);
            public void AssertMsg(bool condition, string message) => System.Diagnostics.Debug.Assert(condition, message);
            public bool RunAssemblyLoadContextLeakDetection(List<IntPtr> assemblyLoadContextWeakHandles)
            {
                return false;
            }
        }
    }
}
