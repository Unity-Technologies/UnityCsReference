// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using UnityEngine.SubsystemsImplementation;

namespace UnityEngine
{
    public abstract class SubsystemDescriptor : ISubsystemDescriptor
    {
        public string id { get; set; }
        public Type subsystemImplementationType { get; set; }

        ISubsystem ISubsystemDescriptor.Create() => CreateImpl();
        internal abstract ISubsystem CreateImpl();
    }

#pragma warning disable CS0618
    public class SubsystemDescriptor<TSubsystem> : SubsystemDescriptor
        where TSubsystem : Subsystem
#pragma warning restore CS0618
    {
        internal override ISubsystem CreateImpl() => this.Create();

        public TSubsystem Create()
        {
            TSubsystem subsystem = SubsystemManager.FindDeprecatedSubsystemByDescriptor(this) as TSubsystem;
            if (subsystem != null)
                return subsystem;

            subsystem = Activator.CreateInstance(subsystemImplementationType) as TSubsystem;
            subsystem.m_SubsystemDescriptor = this;

            SubsystemManager.AddDeprecatedSubsystem(subsystem);
            return subsystem;
        }
    }

    // used in the subsystem-registration package
    internal static class Internal_SubsystemDescriptors
    {
#pragma warning disable CS0618
        [RequiredByNativeCode]
        internal static void Internal_AddDescriptor(SubsystemDescriptor descriptor) => SubsystemDescriptorStore.RegisterDeprecatedDescriptor(descriptor);
#pragma warning restore CS0618
    }
}
