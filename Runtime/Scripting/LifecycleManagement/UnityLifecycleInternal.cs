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
        internal static void Internal_EnterCodeInitializedScope()
        {
            try
            {
                LifecycleController.Instance.EnterScope<CodeInitializedScope>();
            }
            catch (Exception e)
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : Failed to enter CodeInitializedScope due to exception {e.ToString()}", true);
                Debug.LogException(e);
            }
        }

        [RequiredByNativeCode]
        internal static void Internal_EnterManagedObjectsAwokenScope()
        {
            try
            {
                LifecycleController.Instance.EnterScope<ManagedObjectsAwokenScope>();
            }
            catch (Exception e)
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : Failed to enter ManagedObjectsAwokenScope due to exception {e.ToString()}", true);
                Debug.LogException(e);
            }
        }

        [RequiredByNativeCode]
        internal static void Internal_ExitManagedObjectsAwokenScope()
        {
            try
            {
                LifecycleController.Instance.ExitScope<ManagedObjectsAwokenScope>();
            }
            catch (Exception e)
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : Failed to exit ManagedObjectsAwokenScope due to exception {e.ToString()}", true);
                Debug.LogException(e);
            }
        }

        [RequiredByNativeCode]
        internal static void Internal_ExitCodeInitializedScope()
        {
            try
            {
                LifecycleController.Instance.ExitScope<CodeInitializedScope>();
            }
            catch (Exception e)
            {
                DebugLifecycle.ReportError($"Lifecycle ERROR : Failed to exit CodeInitializedScope due to exception {e.ToString()}", true);
                Debug.LogException(e);
            }
        }
    }
}

