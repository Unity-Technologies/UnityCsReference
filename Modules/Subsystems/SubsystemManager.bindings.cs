// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("Modules/Subsystems/SubsystemManager.h")]
    public static partial class SubsystemManager
    {
        // ReSharper disable once UnusedMember.Local - called by native code
        [RequiredByNativeCode]
        static void ReloadSubsystemsStarted()
        {
            if (reloadSubsytemsStarted != null)
                reloadSubsytemsStarted();

            if (beforeReloadSubsystems != null)
                beforeReloadSubsystems();
        }

        // ReSharper disable once UnusedMember.Local - called by native code
        [RequiredByNativeCode]
        static void ReloadSubsystemsCompleted()
        {
            if (reloadSubsytemsCompleted != null)
                reloadSubsytemsCompleted();

            if (afterReloadSubsystems != null)
                afterReloadSubsystems();
        }

        // ReSharper disable once UnusedMember.Local - called by native code
        [RequiredByNativeCode]
        static void InitializeIntegratedSubsystem(IntPtr ptr, IntegratedSubsystem subsystem)
        {
            subsystem.m_Ptr = ptr;
            subsystem.SetHandle(subsystem);
            s_IntegratedSubsystems.Add(subsystem);
        }

        // ReSharper disable once UnusedMember.Local - called by native code
        [RequiredByNativeCode]
        static void ClearSubsystems()
        {
            foreach (var subsystem in s_IntegratedSubsystems)
                subsystem.m_Ptr = IntPtr.Zero;

            s_IntegratedSubsystems.Clear();
            s_StandaloneSubsystems.Clear();
            s_DeprecatedSubsystems.Clear();
        }

        static extern void StaticConstructScriptingClassMap();
        internal static extern void ReportSingleSubsystemAnalytics(string id);
    }
}
