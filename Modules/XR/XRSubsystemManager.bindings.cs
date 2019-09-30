// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;

using intptr_t = System.Int32;

namespace UnityEngine
{
    public interface ISubsystemDescriptor
    {
        string id { get; }
        ISubsystem Create();
    }

    internal interface ISubsystemDescriptorImpl : ISubsystemDescriptor
    {
        IntPtr ptr { get; set; }
    }

    [UsedByNativeCode("XRSubsystemDescriptorBase")]
    [StructLayout(LayoutKind.Sequential)]
    public abstract class IntegratedSubsystemDescriptor : ISubsystemDescriptorImpl
    {
        internal IntPtr m_Ptr;

        public string id
        {
            get { return Internal_SubsystemDescriptors.GetId(m_Ptr); }
        }

        IntPtr ISubsystemDescriptorImpl.ptr { get { return m_Ptr; } set { m_Ptr = value; } }

        ISubsystem ISubsystemDescriptor.Create()
        {
            return CreateImpl();
        }

        internal abstract ISubsystem CreateImpl();
    }

    public abstract class SubsystemDescriptor : ISubsystemDescriptor
    {
        public string id { get; set; }

        public System.Type subsystemImplementationType { get; set; }

        ISubsystem ISubsystemDescriptor.Create()
        {
            return CreateImpl();
        }

        internal abstract ISubsystem CreateImpl();
    }

    [NativeType(Header = "Modules/XR/XRSubsystemDescriptor.h")]
    [UsedByNativeCode("XRSubsystemDescriptor")]
    [StructLayout(LayoutKind.Sequential)]
    public class IntegratedSubsystemDescriptor<TSubsystem> : IntegratedSubsystemDescriptor
        where TSubsystem : IntegratedSubsystem
    {
        internal override ISubsystem CreateImpl()
        {
            return this.Create();
        }

        public TSubsystem Create()
        {
            IntPtr ptr = Internal_SubsystemDescriptors.Create(m_Ptr);
            var instance = (TSubsystem)Internal_SubsystemInstances.Internal_GetInstanceByPtr(ptr);
            if (instance != null)
            {
                instance.m_subsystemDescriptor = this;
            }
            return instance;
        }
    }

    public class SubsystemDescriptor<TSubsystem> : SubsystemDescriptor
        where TSubsystem : Subsystem
    {
        internal override ISubsystem CreateImpl()
        {
            return this.Create();
        }

        public TSubsystem Create()
        {
            TSubsystem existingImpl = Internal_SubsystemInstances.Internal_FindStandaloneSubsystemInstanceGivenDescriptor(this) as TSubsystem;

            if (existingImpl != null)
            {
                return existingImpl;
            }
            else
            {
                TSubsystem susbsystemImpl = Activator.CreateInstance(subsystemImplementationType) as TSubsystem;

                susbsystemImpl.m_subsystemDescriptor = this;
                Internal_SubsystemInstances.Internal_AddStandaloneSubsystem(susbsystemImpl);
                return susbsystemImpl;
            }
        }
    }

    // Handle instance lifetime (on managed side)
    internal static class Internal_SubsystemInstances
    {
        internal static List<ISubsystem> s_IntegratedSubsystemInstances = new List<ISubsystem>();
        internal static List<ISubsystem> s_StandaloneSubsystemInstances = new List<ISubsystem>();

        [RequiredByNativeCode]
        internal static void Internal_InitializeManagedInstance(IntPtr ptr, IntegratedSubsystem inst)
        {
            inst.m_Ptr = ptr;
            inst.SetHandle(inst);
            s_IntegratedSubsystemInstances.Add(inst);
        }

        [RequiredByNativeCode]
        internal static void Internal_ClearManagedInstances()
        {
            foreach (var instance in s_IntegratedSubsystemInstances)
            {
                ((IntegratedSubsystem)instance).m_Ptr = IntPtr.Zero;
            }
            s_IntegratedSubsystemInstances.Clear();
            s_StandaloneSubsystemInstances.Clear();
        }

        [RequiredByNativeCode]
        internal static void Internal_RemoveInstanceByPtr(IntPtr ptr)
        {
            for (int i = s_IntegratedSubsystemInstances.Count - 1; i >= 0; i--)
            {
                if (((IntegratedSubsystem)s_IntegratedSubsystemInstances[i]).m_Ptr == ptr)
                {
                    ((IntegratedSubsystem)s_IntegratedSubsystemInstances[i]).m_Ptr = IntPtr.Zero;
                    s_IntegratedSubsystemInstances.RemoveAt(i);
                }
            }
        }

        internal static IntegratedSubsystem Internal_GetInstanceByPtr(IntPtr ptr)
        {
            foreach (IntegratedSubsystem instance in s_IntegratedSubsystemInstances)
            {
                if (instance.m_Ptr == ptr)
                    return instance;
            }
            return null;
        }

        internal static void Internal_AddStandaloneSubsystem(Subsystem inst)
        {
            s_StandaloneSubsystemInstances.Add(inst);
        }

        internal static Subsystem Internal_FindStandaloneSubsystemInstanceGivenDescriptor(SubsystemDescriptor descriptor)
        {
            foreach (Subsystem subsystemInstance in s_StandaloneSubsystemInstances)
            {
                if (subsystemInstance.m_subsystemDescriptor == descriptor)
                    return subsystemInstance;
            }

            return null;
        }
    }

    // Handle subsystem descriptor lifetime (on managed side)
    [NativeHeader("Modules/XR/XRPrefix.h")]
    internal static class Internal_SubsystemDescriptors
    {
        internal static List<ISubsystemDescriptorImpl> s_IntegratedSubsystemDescriptors = new List<ISubsystemDescriptorImpl>();
        internal static List<ISubsystemDescriptor> s_StandaloneSubsystemDescriptors = new List<ISubsystemDescriptor>();

        [RequiredByNativeCode]
        internal static bool Internal_AddDescriptor(SubsystemDescriptor descriptor)
        {
            foreach (var standaloneDescriptor in s_StandaloneSubsystemDescriptors)
            {
                if (standaloneDescriptor == descriptor)
                    return false;
            }
            s_StandaloneSubsystemDescriptors.Add(descriptor);
            SubsystemManager.ReportSingleSubsystemAnalytics(descriptor.id);
            return true;
        }

        [RequiredByNativeCode]
        internal static void Internal_InitializeManagedDescriptor(IntPtr ptr, ISubsystemDescriptorImpl desc)
        {
            desc.ptr = ptr;
            s_IntegratedSubsystemDescriptors.Add(desc);
        }

        [RequiredByNativeCode]
        internal static void Internal_ClearManagedDescriptors()
        {
            foreach (var descriptor in s_IntegratedSubsystemDescriptors)
            {
                descriptor.ptr = IntPtr.Zero;
            }
            s_IntegratedSubsystemDescriptors.Clear();
        }

        // These are here instead of on SubsystemDescriptor because generic types are not supported by .bindings.cs
        [NativeConditional("ENABLE_XR")]
        public static extern IntPtr Create(IntPtr descriptorPtr);

        [NativeConditional("ENABLE_XR")]
        public static extern string GetId(IntPtr descriptorPtr);
    }

    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeType(Header = "Modules/XR/XRSubsystemManager.h")]
    public static class SubsystemManager
    {
        static SubsystemManager()
        {
            StaticConstructScriptingClassMap();
        }

        public static void GetAllSubsystemDescriptors(List<ISubsystemDescriptor> descriptors)
        {
            descriptors.Clear();
            foreach (var descriptor in Internal_SubsystemDescriptors.s_IntegratedSubsystemDescriptors)
            {
                descriptors.Add(descriptor);
            }

            foreach (var descriptor in Internal_SubsystemDescriptors.s_StandaloneSubsystemDescriptors)
            {
                descriptors.Add(descriptor);
            }
        }

        [NativeConditional("ENABLE_XR")]
        internal static extern void ReportSingleSubsystemAnalytics(string id);

        public static void GetSubsystemDescriptors<T>(List<T> descriptors)
            where T : ISubsystemDescriptor
        {
            descriptors.Clear();
            foreach (var descriptor in Internal_SubsystemDescriptors.s_IntegratedSubsystemDescriptors)
            {
                if (descriptor is T)
                    descriptors.Add((T)descriptor);
            }

            foreach (var descriptor in Internal_SubsystemDescriptors.s_StandaloneSubsystemDescriptors)
            {
                if (descriptor is T)
                    descriptors.Add((T)descriptor);
            }
        }

        public static void GetInstances<T>(List<T> instances)
            where T : ISubsystem
        {
            instances.Clear();
            foreach (var instance in Internal_SubsystemInstances.s_IntegratedSubsystemInstances)
            {
                if (instance is T)
                    instances.Add((T)instance);
            }

            foreach (var instance in Internal_SubsystemInstances.s_StandaloneSubsystemInstances)
            {
                if (instance is T)
                    instances.Add((T)instance);
            }
        }

        [NativeConditional("ENABLE_XR")]
        extern internal static void DestroyInstance_Internal(IntPtr instancePtr);

        [NativeConditional("ENABLE_XR")]
        extern internal static void StaticConstructScriptingClassMap();

        public static event Action reloadSubsytemsStarted;

        [RequiredByNativeCode]
        static void Internal_ReloadSubsystemsStarted()
        {
            if (reloadSubsytemsStarted != null)
            {
                reloadSubsytemsStarted();
            }
        }

        public static event Action reloadSubsytemsCompleted;

        [RequiredByNativeCode]
        static void Internal_ReloadSubsystemsCompleted()
        {
            if (reloadSubsytemsCompleted != null)
            {
                reloadSubsytemsCompleted();
            }
        }
    }

    public interface ISubsystem
    {
        void Start();

        void Stop();

        void Destroy();

        bool running { get; }
    }

    [NativeType(Header = "Modules/XR/XRSubsystem.h")]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public class IntegratedSubsystem : ISubsystem
    {
        internal IntPtr m_Ptr;
        internal ISubsystemDescriptor m_subsystemDescriptor;

        extern internal void SetHandle(IntegratedSubsystem inst);
        extern public void Start();
        extern public void Stop();
        public void Destroy()
        {
            IntPtr removedPtr = m_Ptr;
            Internal_SubsystemInstances.Internal_RemoveInstanceByPtr(m_Ptr);
            SubsystemManager.DestroyInstance_Internal(removedPtr);
        }

        public bool running { get { return Internal_IsRunning(); } }

        [NativeConditional("ENABLE_XR")]
        extern internal bool Internal_IsRunning();
    }

    [UsedByNativeCode("XRSubsystem_TXRSubsystemDescriptor")]
    public class IntegratedSubsystem<TSubsystemDescriptor> : IntegratedSubsystem where TSubsystemDescriptor : ISubsystemDescriptor
    {
        public TSubsystemDescriptor SubsystemDescriptor
        {
            get { return (TSubsystemDescriptor)m_subsystemDescriptor; }
        }
    }

    public abstract class Subsystem : ISubsystem
    {
        internal ISubsystemDescriptor m_subsystemDescriptor;

        abstract public void Start();

        abstract public void Stop();

        public void Destroy()
        {
            Internal_SubsystemInstances.s_StandaloneSubsystemInstances.Remove(this);
            OnDestroy();
        }

        abstract public bool running { get; }

        abstract protected void OnDestroy();
    }

    public abstract class Subsystem<TSubsystemDescriptor> : Subsystem where TSubsystemDescriptor : ISubsystemDescriptor
    {
        public TSubsystemDescriptor SubsystemDescriptor
        {
            get { return (TSubsystemDescriptor)m_subsystemDescriptor; }
        }
    }
}
