// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/Subsystems/Subsystem.h")]
    public class IntegratedSubsystem : ISubsystem
    {
        internal IntPtr m_Ptr;
        internal ISubsystemDescriptor m_SubsystemDescriptor;

        extern internal void SetHandle(IntegratedSubsystem subsystem);
        extern public void Start();
        extern public void Stop();
        public void Destroy()
        {
            IntPtr removedPtr = m_Ptr;
            SubsystemManager.RemoveIntegratedSubsystemByPtr(m_Ptr);
            SubsystemBindings.DestroySubsystem(removedPtr);
            m_Ptr = IntPtr.Zero;
        }

        public bool running => valid && IsRunning();

        internal bool valid => m_Ptr != IntPtr.Zero;

        extern internal bool IsRunning();
    }

    [UsedByNativeCode("Subsystem_TSubsystemDescriptor")]
    public partial class IntegratedSubsystem<TSubsystemDescriptor> : IntegratedSubsystem
        where TSubsystemDescriptor : ISubsystemDescriptor
    {
        public TSubsystemDescriptor subsystemDescriptor => (TSubsystemDescriptor)m_SubsystemDescriptor;
    }

    internal static class SubsystemBindings
    {
        internal static extern void DestroySubsystem(IntPtr nativePtr);
    }
}
