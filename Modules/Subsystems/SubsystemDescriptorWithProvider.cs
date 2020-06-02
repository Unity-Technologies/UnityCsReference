// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.SubsystemsImplementation
{
    public abstract class SubsystemDescriptorWithProvider : ISubsystemDescriptor
    {
        public string id { get; set; }

        internal protected Type providerType { get; set; }
        internal protected Type subsystemTypeOverride { get; set; }

        internal abstract ISubsystem CreateImpl();
        ISubsystem ISubsystemDescriptor.Create() => CreateImpl();

        internal abstract void ThrowIfInvalid();
    }

    public class SubsystemDescriptorWithProvider<TSubsystem, TProvider> : SubsystemDescriptorWithProvider
        where TSubsystem : SubsystemWithProvider, new()
        where TProvider : SubsystemProvider<TSubsystem>
    {
        internal override ISubsystem CreateImpl() => this.Create();

        public TSubsystem Create()
        {
            var subsystem = SubsystemManager.FindStandaloneSubsystemByDescriptor(this) as TSubsystem;
            if (subsystem != null)
                return subsystem;

            var provider = CreateProvider();
            if (provider == null)
                return null;

            subsystem = subsystemTypeOverride != null
                ? (TSubsystem)Activator.CreateInstance(subsystemTypeOverride)
                : new TSubsystem();

            subsystem.Initialize(this, provider);
            SubsystemManager.AddStandaloneSubsystem(subsystem);
            return subsystem;
        }

        internal override sealed void ThrowIfInvalid()
        {
            if (providerType == null)
                throw new InvalidOperationException("Invalid descriptor - must supply a valid providerType field!");

            if (!providerType.IsSubclassOf(typeof(TProvider)))
                throw new InvalidOperationException(string.Format("Can't create provider - providerType '{0}' is not a subclass of '{1}'!", providerType.ToString(), typeof(TProvider).ToString()));

            if (subsystemTypeOverride != null && !subsystemTypeOverride.IsSubclassOf(typeof(TSubsystem)))
                throw new InvalidOperationException(string.Format("Can't create provider - subsystemTypeOverride '{0}' is not a subclass of '{1}'!", subsystemTypeOverride.ToString(), typeof(TSubsystem).ToString()));
        }

        internal TProvider CreateProvider()
        {
            var provider = (TProvider)Activator.CreateInstance(providerType);
            return provider.TryInitialize() ? provider : null;
        }
    }

    namespace Extensions
    {
        public static class SubsystemDescriptorExtensions
        {
            public static SubsystemProxy<TSubsystem, TProvider> CreateProxy<TSubsystem, TProvider>(this SubsystemDescriptorWithProvider<TSubsystem, TProvider> descriptor)
                where TSubsystem : SubsystemWithProvider, new()
                where TProvider : SubsystemProvider<TSubsystem>
            {
                var provider = descriptor.CreateProvider();
                return provider != null ? new SubsystemProxy<TSubsystem, TProvider>(provider) : null;
            }
        }
    }
}
