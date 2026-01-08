// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Scripting;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Assemblies;
using UnityEngine.Bindings;
using UnityEngine.Scripting;


namespace UnityEngine
{
    /// <summary>
    /// DomainReloadLifecycleController represents the LifecycleManagement integration for Mono Unity
    /// It is the alternative implementation of the LifecycleManagement responsibilities of AssemblyLoader:
    ///     - ensure a LifecycleController is instantiated
    ///     - set up AttributeUsageLocator dependency
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

        [RequiredByNativeCode]
        static void Internal_EnterAssembliesLoadedLifecycleScopes_PreDeserialization()
        {
            // IAttributeUsageLocator implementations:
            // AttributeUsageLocatorWithTypeCache: TypeCache-based - best performance, unfortunately currently only available in Editor.     "UnityEditor.AttributeUsageLocatorWithTypeCache, UnityEditor.CoreModule";
            // AttributeUsageLocatorWithTypeDB: until we get TypeCache in player, temporary TypeDB-based implementation for lifecycle attributes, similar to RuntimeInitializeOnLoad
            //                                                                                                                               "UnityEngine.AttributeUsageLocatorWithTypeDB, UnityEngine.CoreModule";
            string kAttributeUsageLocatorTypeName = "UnityEditor.AttributeUsageLocatorWithTypeCache, UnityEditor";
            try
            {
                LifecycleController.InitializeForIl2Cpp(
                    kAttributeUsageLocatorTypeName,
                    new ScriptingCoreDebugForIl2AndMonoCpp());

                _currentAssemblyLoadedScope = new AssemblyLoadedScopeIl2Cpp(CurrentAssemblies.GetLoadedAssemblies());
                LifecycleController.Instance.EnterScope(_currentAssemblyLoadedScope);

                LifecycleController.Instance.EnterScope<CodeLoadedScope>();
            }
            catch (Exception e)
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : Failed to setup LifecycleManagement and enter code reload scopes (pre deserialization) due to exception {e.ToString()}", true);
                Debug.LogException(e);
            }
        }

        [RequiredByNativeCode]
        static void Internal_EnterAssembliesLoadedLifecycleScopes_AfterManagedObjectsRestored()
        {
            try
            {
                LifecycleController.Instance.EnterScope<ManagedObjectsRestoredScope>();
            }
            catch (Exception e)
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : Failed to setup LifecycleManagement and enter code reload scope ManagedObjectsRestoredScope due to exception {e.ToString()}", true);
                Debug.LogException(e);
            }
        }

        [RequiredByNativeCode]
        static void Internal_EnterAssembliesLoadedLifecycleScopes_AfterManagedObjectsAwoken()
        {
            try
            {
                LifecycleController.Instance.EnterScope<ManagedObjectsAwokenScope>();
            }
            catch (Exception e)
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : Failed to setup LifecycleManagement and enter code reload scope ManagedObjectsAwokenScope due to exception {e.ToString()}", true);
                Debug.LogException(e);
            }
        }

        [RequiredByNativeCode]
        static void Internal_ExitAssembliesLoadedLifecycleScopes_BeforeManagedObjectsDisabled()
        {
            try
            {
                LifecycleController.Instance.ExitScope<ManagedObjectsAwokenScope>();
            }
            catch (Exception e)
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : Failed to exit code reload scope ManagedObjectsAwokenScope due to exception {e.ToString()}", true);
                Debug.LogException(e);
            }
        }

        [RequiredByNativeCode]
        static void Internal_ExitAssembliesLoadedLifecycleScopes_BeforeManagedObjectsBackup()
        {
            try
            {
                LifecycleController.Instance.ExitScope<ManagedObjectsRestoredScope>();
            }
            catch (Exception e)
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : Failed to exit code reload scope ManagedObjectsRestoredScope due to exception {e.ToString()}", true);
                Debug.LogException(e);
            }
        }

        [RequiredByNativeCode]
        static void Internal_OnceCodeReloadSerializationDone()
        {
            try
            {
                LifecycleController.Instance.ExitScope<CodeLoadedScope>();

                LifecycleController.Instance.ExitScope(_currentAssemblyLoadedScope);
                _currentAssemblyLoadedScope = null;
            }
            catch (Exception e)
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : Failed to exit code reload scopes (post serialization) and shut down the Lifecycle Management core due to exception {e.ToString()}", true);
                Debug.LogException(e);
            }
        }
    }
}

