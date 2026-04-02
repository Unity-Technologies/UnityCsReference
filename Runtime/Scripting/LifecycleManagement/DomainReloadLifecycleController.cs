// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Scripting;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Scripting;


namespace UnityEngine
{
    /// <summary>
    /// DomainReloadLifecycleController represents the LifecycleManagement integration for Mono and IL2CPP
    /// It is the alternative implementation of the LifecycleManagement responsibilities of AssemblyLoader:
    ///     - ensure a LifecycleController is instantiated
    ///     - provide complete set of registered assemblies
    ///     - IScriptingCoreDebug dependency implementation and setup
    ///     - Enter/Exit assembly load related lifecycle scopes
    /// </summary>
    partial class DomainReloadLifecycleController
    {
        private class ScriptingCoreDebugForIl2AndMonoCpp : IScriptingCoreDebug
        {
            public bool IsDiagnosticSwitchEnabled(string name)
            {
                //if (!Debug.enableDiagnosticSwitches)
                    return false;

                //try
                //{
                //    return (bool)Debug.GetDiagnosticSwitch(name).value;
                //}
                //catch (ArgumentException)
                //{
                //    return false;
                //}
            }

            public void Log(string message) => Debug.Log(message);
            public void LogError(string message) => Debug.LogError(message);
            public void LogException(Exception exception) => Debug.LogException(exception);
            public void LogFormat(string format, params object[] args) => Debug.LogFormat(format, args);
            public void Assert(bool condition) => Debug.Assert(condition);
            public void AssertMsg(bool condition, string message) => Debug.Assert(condition, message);
            public bool RunAssemblyLoadContextLeakDetection(List<IntPtr> assemblyLoadContextWeakHandles) { /* nop on Mono */ return false; }
        }

        class AssemblyNameEqualityComparer : EqualityComparer<System.Reflection.Assembly>
        {
            public override bool Equals(Assembly x, Assembly y)
            {
                return x?.FullName == y?.FullName;
            }

            public override int GetHashCode(Assembly obj)
            {
                return obj?.FullName.GetHashCode() ?? 0;
            }
        }

        [NoAutoStaticsCleanup]
        private static AssemblyLoadedScopeIl2Cpp _currentAssemblyLoadedScope = null;

        [NoAutoStaticsCleanup]
        private static DependencyOrderedNativeCallbackProvider _nativeCallbackProvider = null;

        [RequiredByNativeCode]
        static void Internal_InitializeLifecycleController()
        {
            try
            {
                LifecycleController.InitializeForIl2Cpp(new ScriptingCoreDebugForIl2AndMonoCpp());

                _nativeCallbackProvider = new DependencyOrderedNativeCallbackProvider();
                LifecycleController.Instance.SetDependency_NativeCallbackProvider(_nativeCallbackProvider);
            }
            catch (Exception e)
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : Failed to initialize LifecycleManagement due to exception {e.ToString()}", true);
                Debug.LogException(e);
            }
        }

        [RequiredByNativeCode]
        static void Internal_EnterAssemblyLoadedScope(Assembly[] loadedAssemblies)
        {
            try
            {
                _currentAssemblyLoadedScope = new AssemblyLoadedScopeIl2Cpp(loadedAssemblies);
                LifecycleController.Instance.EnterScope(_currentAssemblyLoadedScope);
            }
            catch (Exception e)
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : Failed to Enter AssemblyLoaded scopes, due to exception {e.ToString()}", true);
                Debug.LogException(e);
            }
        }

        [RequiredByNativeCode]
        static void Internal_EnterCodeLoadedScope()
        {
            try
            {
                LifecycleController.Instance.EnterScope<CodeLoadedScope>();
            }
            catch (Exception e)
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : Failed to enter code loaded scope due to exception {e.ToString()}", true);
                Debug.LogException(e);
            }
        }

        [RequiredByNativeCode]
        static void Internal_ExitCodeLoadedScope()
        {
            try
            {
                LifecycleController.Instance.ExitScope<CodeLoadedScope>();
            }
            catch (Exception e)
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : Failed to exit code loaded scope, due to exception {e.ToString()}", true);
                Debug.LogException(e);
            }
        }

        [RequiredByNativeCode]
        static void Internal_ExitAssemblyLoadedScope()
        {
            try
            {
                LifecycleController.Instance.ExitScope(_currentAssemblyLoadedScope);
                _currentAssemblyLoadedScope = null;
                _nativeCallbackProvider = null;
            }
            catch (Exception e)
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : Failed to exit assembly loaded scope, due to exception {e.ToString()}", true);
                Debug.LogException(e);
            }
        }
    }
}

