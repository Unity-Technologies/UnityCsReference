// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.SubsystemsImplementation;

namespace UnityEngine
{
    public static partial class SubsystemManager
    {
        static SubsystemManager() => StaticConstructScriptingClassMap();

        public static void GetAllSubsystemDescriptors(List<ISubsystemDescriptor> descriptors)
        {
            SubsystemDescriptorStore.GetAllSubsystemDescriptors(descriptors);
        }

        public static void GetSubsystemDescriptors<T>(List<T> descriptors)
            where T : ISubsystemDescriptor
        {
            SubsystemDescriptorStore.GetSubsystemDescriptors(descriptors);
        }

        public static void GetSubsystems<T>(List<T> subsystems)
            where T : ISubsystem
        {
            subsystems.Clear();
            AddSubsystemSubset(s_IntegratedSubsystems, subsystems);
            AddSubsystemSubset(s_StandaloneSubsystems, subsystems);
            AddSubsystemSubset(s_DeprecatedSubsystems, subsystems);
        }

        static void AddSubsystemSubset<TBaseTypeInList, TQueryType>(List<TBaseTypeInList> copyFrom, List<TQueryType> copyTo)
            where TQueryType : ISubsystem
            where TBaseTypeInList : ISubsystem
        {
            foreach (var subsystem in copyFrom)
            {
                if (subsystem is TQueryType concreteSubsystem)
                    copyTo.Add(concreteSubsystem);
            }
        }

// event never invoked warning (invoked indirectly from native code)
#pragma warning disable CS0067
        public static event Action beforeReloadSubsystems;
        public static event Action afterReloadSubsystems;
#pragma warning restore CS0067

        internal static IntegratedSubsystem GetIntegratedSubsystemByPtr(IntPtr ptr)
        {
            foreach (var subsystem in s_IntegratedSubsystems)
            {
                if (subsystem.m_Ptr == ptr)
                    return subsystem;
            }

            return null;
        }

        internal static void RemoveIntegratedSubsystemByPtr(IntPtr ptr)
        {
            for (int finderIndex = 0; finderIndex < s_IntegratedSubsystems.Count; ++finderIndex)
            {
                if (s_IntegratedSubsystems[finderIndex].m_Ptr != ptr)
                    continue;

                s_IntegratedSubsystems[finderIndex].m_Ptr = IntPtr.Zero;
                s_IntegratedSubsystems.RemoveAt(finderIndex);
                break;
            }
        }

        internal static void AddStandaloneSubsystem(SubsystemWithProvider subsystem)
        {
            s_StandaloneSubsystems.Add(subsystem);
        }

        internal static bool RemoveStandaloneSubsystem(SubsystemWithProvider subsystem)
        {
            return s_StandaloneSubsystems.Remove(subsystem);
        }

        internal static SubsystemWithProvider FindStandaloneSubsystemByDescriptor(SubsystemDescriptorWithProvider descriptor)
        {
            foreach (var subsystem in s_StandaloneSubsystems)
            {
                if (subsystem.descriptor == descriptor)
                    return subsystem;
            }

            return null;
        }

        static List<IntegratedSubsystem> s_IntegratedSubsystems = new List<IntegratedSubsystem>();
        static List<SubsystemWithProvider> s_StandaloneSubsystems = new List<SubsystemWithProvider>();
#pragma warning disable CS0618
        static List<Subsystem> s_DeprecatedSubsystems = new List<Subsystem>();
#pragma warning restore CS0618
    }
}
