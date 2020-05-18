// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public static partial class SubsystemManager
    {
        public static void GetInstances<T>(List<T> subsystems)
            where T : ISubsystem
        {
            GetSubsystems(subsystems);
        }

#pragma warning disable CS0618
        internal static void AddDeprecatedSubsystem(Subsystem subsystem) => s_DeprecatedSubsystems.Add(subsystem);
        internal static bool RemoveDeprecatedSubsystem(Subsystem subsystem) => s_DeprecatedSubsystems.Remove(subsystem);

        internal static Subsystem FindDeprecatedSubsystemByDescriptor(SubsystemDescriptor descriptor)
        {
            foreach (var subsystem in s_DeprecatedSubsystems)
            {
                if (subsystem.m_SubsystemDescriptor == descriptor)
                    return subsystem;
            }

            return null;
        }

#pragma warning restore CS0618

// event never invoked warning (invoked indirectly from native code)
#pragma warning disable CS0067
        public static event Action reloadSubsytemsStarted;
        public static event Action reloadSubsytemsCompleted;
#pragma warning restore CS0067
    }
}
