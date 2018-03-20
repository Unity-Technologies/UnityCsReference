// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using UnityEngine.Bindings;
using UnityEngine.Scripting;

using intptr_t = System.Int32;

namespace UnityEngine.Experimental
{
    public interface ISubsystemDescriptor
    {
    }

    internal interface ISubsystemDescriptorImpl
    {
        IntPtr ptr { get; set; }
    }

    [UsedByNativeCode("XRSubsystemDescriptorBase")]
    [StructLayout(LayoutKind.Sequential)]
    public class SubsystemDescriptorBase : ISubsystemDescriptor, ISubsystemDescriptorImpl
    {
        internal IntPtr m_Ptr;

        public string id
        {
            get { return Internal_SubsystemDescriptors.GetId(m_Ptr); }
        }

        IntPtr ISubsystemDescriptorImpl.ptr { get { return m_Ptr; } set { m_Ptr = value; } }
    }

    [NativeType(Header = "Modules/XR/XRSubsystemDescriptor.h")]
    [UsedByNativeCode("XRSubsystemDescriptor")]
    [StructLayout(LayoutKind.Sequential)]
    public class SubsystemDescriptor<TSubsystem> : SubsystemDescriptorBase
        where TSubsystem : Subsystem
    {
        public TSubsystem Create()
        {
            IntPtr ptr = Internal_SubsystemDescriptors.Create(m_Ptr);
            var instance = (TSubsystem)Internal_SubsystemInstances.Internal_GetInstanceByPtr(ptr);
            instance.m_subsystemDescriptor = this;
            return instance;
        }
    }

    // Handle instance lifetime (on managed side)
    internal static class Internal_SubsystemInstances
    {
        internal static List<Subsystem> s_SubsystemInstances = new List<Subsystem>();

        [RequiredByNativeCode]
        internal static void Internal_InitializeManagedInstance(IntPtr ptr, Subsystem inst)
        {
            inst.m_Ptr = ptr;
            inst.SetHandle(inst);
            s_SubsystemInstances.Add(inst);
        }

        [RequiredByNativeCode]
        internal static void Internal_ClearManagedInstances()
        {
            foreach (var instance in s_SubsystemInstances)
            {
                instance.m_Ptr = IntPtr.Zero;
            }
            s_SubsystemInstances.Clear();
        }

        [RequiredByNativeCode]
        internal static void Internal_RemoveInstanceByPtr(IntPtr ptr)
        {
            for (int i = s_SubsystemInstances.Count - 1; i >= 0; i--)
            {
                if (s_SubsystemInstances[i].m_Ptr == ptr)
                {
                    s_SubsystemInstances[i].m_Ptr = IntPtr.Zero;
                    s_SubsystemInstances.RemoveAt(i);
                }
            }
        }

        internal static Subsystem Internal_GetInstanceByPtr(IntPtr ptr)
        {
            foreach (Subsystem instance in s_SubsystemInstances)
            {
                if (instance.m_Ptr == ptr)
                    return instance;
            }
            return null;
        }
    }

    // Handle subsystem descriptor lifetime (on managed side)
    internal static class Internal_SubsystemDescriptors
    {
        internal static List<ISubsystemDescriptorImpl> s_SubsystemDescriptors = new List<ISubsystemDescriptorImpl>();

        [RequiredByNativeCode]
        internal static void Internal_InitializeManagedDescriptor(IntPtr ptr, ISubsystemDescriptorImpl desc)
        {
            desc.ptr = ptr;
            s_SubsystemDescriptors.Add(desc);
        }

        [RequiredByNativeCode]
        internal static void Internal_ClearManagedDescriptors()
        {
            foreach (var descriptor in s_SubsystemDescriptors)
            {
                descriptor.ptr = IntPtr.Zero;
            }
            s_SubsystemDescriptors.Clear();
        }

        // These are here instead of on SubsystemDescriptor because generic types are not supported by .bindings.cs
        public static extern IntPtr Create(IntPtr descriptorPtr);
        public static extern string GetId(IntPtr descriptorPtr);
    }

    [NativeType(Header = "Modules/XR/XRSubsystemManager.h")]
    public static class SubsystemManager
    {
        static SubsystemManager()
        {
            StaticConstructScriptingClassMap();
        }

        public static void GetSubsystemDescriptors<T>(List<T> descriptors)
            where T : ISubsystemDescriptor
        {
            descriptors.Clear();
            foreach (var descriptor in Internal_SubsystemDescriptors.s_SubsystemDescriptors)
            {
                if (descriptor is T)
                    descriptors.Add((T)descriptor);
            }
        }

        public static void GetInstances<T>(List<T> instances)
            where T : Subsystem
        {
            instances.Clear();
            foreach (var instance in Internal_SubsystemInstances.s_SubsystemInstances)
            {
                if (instance is T)
                    instances.Add((T)instance);
            }
        }

        extern internal static void DestroyInstance_Internal(IntPtr instancePtr);
        extern internal static void StaticConstructScriptingClassMap();
    }

    [NativeType(Header = "Modules/XR/XRSubsystem.h")]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public class Subsystem
    {
        internal IntPtr m_Ptr;
        internal ISubsystemDescriptor m_subsystemDescriptor;

        extern internal void SetHandle(Subsystem inst);
        extern public void Start();
        extern public void Stop();
        public void Destroy()
        {
            IntPtr removedPtr = m_Ptr;
            Internal_SubsystemInstances.Internal_RemoveInstanceByPtr(m_Ptr);
            SubsystemManager.DestroyInstance_Internal(removedPtr);
        }
    }
    [UsedByNativeCode("XRSubsystem_TXRSubsystemDescriptor")]
    public class Subsystem<TSubsystemDescriptor> : Subsystem where TSubsystemDescriptor : ISubsystemDescriptor
    {
        public TSubsystemDescriptor SubsystemDescriptor
        {
            get { return (TSubsystemDescriptor)m_subsystemDescriptor; }
        }
    }
}
