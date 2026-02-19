using System;
using System.Collections.Generic;

namespace Unity.Scripting
{
    // *** TODO (code reload): this interface is in LifecycleManagement assembly temporarily until we find out how we want to organize the ScriptingCore functionality
    internal interface IScriptingCoreDebug
    {
        public bool IsDiagnosticSwitchEnabled(string name);

        public void Log(string message);
        public void LogError(string message);

        public void Assert(bool condition);
        public void AssertMsg(bool condition, string message);

        public bool RunAssemblyLoadContextLeakDetection(List<IntPtr> assemblyLoadContextWeakHandles);
    }
}
