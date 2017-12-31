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

namespace UnityEngine.Experimental.XR
{
    public interface IXRSubsystemDescriptor
    {
    }

    internal interface IXRSubsystemDescriptorImpl
    {
        IntPtr ptr { get; set; }
    }

    [UsedByNativeCode("XRSubsystemDescriptorBase")]
    [StructLayout(LayoutKind.Sequential)]
    public class XRSubsystemDescriptorBase : IXRSubsystemDescriptor, IXRSubsystemDescriptorImpl
    {
        internal IntPtr m_Ptr;

        public string id
        {
            get { return Internal_XRSubsystemDescriptors.GetId(m_Ptr); }
        }

        IntPtr IXRSubsystemDescriptorImpl.ptr { get { return m_Ptr; } set { m_Ptr = value; } }
    }

    [NativeType(Header = "Modules/XR/XRSubsystemDescriptor.h")]
    [UsedByNativeCode("XRSubsystemDescriptor")]
    [StructLayout(LayoutKind.Sequential)]
    public class XRSubsystemDescriptor<TXRInstance> : XRSubsystemDescriptorBase
        where TXRInstance : XRInstance
    {
        public TXRInstance Create()
        {
            IntPtr ptr = Internal_XRSubsystemDescriptors.Create(m_Ptr);
            var instance = (TXRInstance)Internal_XRSubsystemInstances.Internal_GetInstanceByPtr(ptr);
            instance.m_subsystemDescriptor = this;
            return instance;
        }
    }

    // Handle instance lifetime (on managed side)
    internal static class Internal_XRSubsystemInstances
    {
        internal static List<XRInstance> s_SubsystemInstances = new List<XRInstance>();

        [RequiredByNativeCode]
        internal static void Internal_InitializeManagedInstance(IntPtr ptr, XRInstance inst)
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

        internal static XRInstance Internal_GetInstanceByPtr(IntPtr ptr)
        {
            foreach (XRInstance instance in s_SubsystemInstances)
            {
                if (instance.m_Ptr == ptr)
                    return instance;
            }
            return null;
        }
    }

    // Handle subsystem descriptor lifetime (on managed side)
    internal static class Internal_XRSubsystemDescriptors
    {
        internal static List<IXRSubsystemDescriptorImpl> s_SubsystemDescriptors = new List<IXRSubsystemDescriptorImpl>();

        [RequiredByNativeCode]
        internal static void Internal_InitializeManagedDescriptor(IntPtr ptr, IXRSubsystemDescriptorImpl desc)
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

        // These are here instead of on XRSubsystemDescriptor because generic types are not supported by .bindings.cs
        public static extern IntPtr Create(IntPtr descriptorPtr);
        public static extern string GetId(IntPtr descriptorPtr);
    }

    [NativeType(Header = "Modules/XR/XRSubsystemManager.h")]
    public static class XRSubsystemManager
    {
        static XRSubsystemManager()
        {
            StaticConstructScriptingClassMap();
        }

        public static void GetSubsystemDescriptors<T>(List<T> descriptors)
            where T : IXRSubsystemDescriptor
        {
            descriptors.Clear();
            foreach (var descriptor in Internal_XRSubsystemDescriptors.s_SubsystemDescriptors)
            {
                if (descriptor is T)
                    descriptors.Add((T)descriptor);
            }
        }

        public static void GetInstances<T>(List<T> instances)
            where T : XRInstance
        {
            instances.Clear();
            foreach (var instance in Internal_XRSubsystemInstances.s_SubsystemInstances)
            {
                if (instance is T)
                    instances.Add((T)instance);
            }
        }

        extern internal static void DestroyInstance_Internal(IntPtr instancePtr);
        extern internal static void StaticConstructScriptingClassMap();
    }

    [NativeType(Header = "Modules/XR/XRInstance.h")]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public class XRInstance
    {
        internal IntPtr m_Ptr;
        internal IXRSubsystemDescriptor m_subsystemDescriptor;

        extern internal void SetHandle(XRInstance inst);
        extern public void Start();
        extern public void Stop();
        public void Destroy()
        {
            IntPtr removedPtr = m_Ptr;
            Internal_XRSubsystemInstances.Internal_RemoveInstanceByPtr(m_Ptr);
            XRSubsystemManager.DestroyInstance_Internal(removedPtr);
        }
    }
    [UsedByNativeCode("XRInstance_TXRSubsystemDescriptor")]
    public class XRInstance<TXRSubsystemDescriptor> : XRInstance where TXRSubsystemDescriptor : IXRSubsystemDescriptor
    {
        public TXRSubsystemDescriptor SubsystemDescriptor
        {
            get { return (TXRSubsystemDescriptor)m_subsystemDescriptor; }
        }
    }
}
