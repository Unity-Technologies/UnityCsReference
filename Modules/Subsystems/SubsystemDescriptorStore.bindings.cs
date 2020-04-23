// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.SubsystemsImplementation
{
    [NativeHeader("Modules/Subsystems/SubsystemManager.h")]
    public static partial class SubsystemDescriptorStore
    {
        [RequiredByNativeCode]
        internal static void InitializeManagedDescriptor(IntPtr ptr, IntegratedSubsystemDescriptor desc)
        {
            desc.m_Ptr = ptr;
            s_IntegratedDescriptors.Add(desc);
        }

        [RequiredByNativeCode]
        internal static void ClearManagedDescriptors()
        {
            foreach (var descriptor in s_IntegratedDescriptors)
                descriptor.m_Ptr = IntPtr.Zero;

            s_IntegratedDescriptors.Clear();
        }

        static extern void ReportSingleSubsystemAnalytics(string id);
    }
}
