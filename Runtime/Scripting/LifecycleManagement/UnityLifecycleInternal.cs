// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Scripting;

namespace UnityEngine
{
    internal partial class UnityLifecycleInternal
    {
        [RequiredByNativeCode]
        internal static void Internal_EnterAssembliesLoadedLifecycleScopes_OnCodeInitializing()
        {
            try
            {
                LifecycleController.Instance.EnterScope<CodeInitializedScope>();
            }
            catch (Exception e)
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : Failed to setup LifecycleManagement and enter code reload scope CodeInitializedScope due to exception {e.ToString()}", true);
                Debug.LogException(e);
            }
        }

        [RequiredByNativeCode]
        internal static void Internal_EnterAssembliesLoadedLifecycleScopes_AfterManagedObjectsAwoken()
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
        internal static void Internal_ExitAssembliesLoadedLifecycleScopes_BeforeManagedObjectsDisabled()
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
        internal static void Internal_ExitAssembliesLoadedLifecycleScopes_OnCodeDeinitializing()
        {
            try
            {
                LifecycleController.Instance.ExitScope<CodeInitializedScope>();
            }
            catch (Exception e)
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : Failed to exit code reload scope CodeInitializedScope due to exception {e.ToString()}", true);
                Debug.LogException(e);
            }
        }
    }
}
