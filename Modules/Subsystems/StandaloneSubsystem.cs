// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.SubsystemsImplementation
{
    public abstract class SubsystemWithProvider : ISubsystem
    {
        internal bool m_Proxy;

        public void Start()
        {
            if (running)
                return;

            OnStart();
            running = true;
        }

        protected abstract void OnStart();

        public void Stop()
        {
            if (!running)
                return;

            OnStop();
            running = false;
        }

        protected abstract void OnStop();

        public void Destroy()
        {
            Stop();
            if (SubsystemManager.RemoveStandaloneSubsystem(this))
                OnDestroy();
        }

        protected abstract void OnDestroy();

        public bool running { get; private set; }

        internal abstract void SetProvider(SubsystemProvider subsystemProvider);

        internal abstract SubsystemDescriptorWithProvider GetDescriptor();
        internal abstract void SetDescriptor(SubsystemDescriptorWithProvider descriptor);
    }

    public abstract class SubsystemWithProvider<TSubsystem, TSubsystemDescriptor, TProvider, TProviderToSubsystem> : SubsystemWithProvider
        where TSubsystem : SubsystemWithProvider, new()
        where TSubsystemDescriptor : SubsystemDescriptorWithProvider
        where TProvider : SubsystemProvider<TSubsystem, TProviderToSubsystem>
        where TProviderToSubsystem : IProviderToSubsystem<TSubsystem>
    {
        public TSubsystemDescriptor subsystemDescriptor { get; private set; }

        protected internal TProvider provider { get; private set; }

        protected override void OnStart() => provider.Start();
        protected override void OnStop() => provider.Stop();
        protected override void OnDestroy() => provider.Destroy();

        internal override sealed void SetProvider(SubsystemProvider subsystemProvider)
        {
            if (subsystemProvider is TProvider castedProvider)
                provider = (TProvider)subsystemProvider;
            else
                throw new InvalidOperationException($"Expected provider of type {typeof(TProvider).Name}");
        }

        internal override sealed SubsystemDescriptorWithProvider GetDescriptor()
            => subsystemDescriptor;
        internal override sealed void SetDescriptor(SubsystemDescriptorWithProvider descriptor)
            => subsystemDescriptor = (TSubsystemDescriptor)descriptor;
    }

    namespace Extensions
    {
        public static class SubsystemExtensions
        {
            public static TProvider GetProvider<TSubsystem, TDescriptor, TProvider, TProviderToSubsystem>(
                this SubsystemWithProvider<TSubsystem, TDescriptor, TProvider, TProviderToSubsystem> subsystem)
                where TSubsystem : SubsystemWithProvider, new()
                where TDescriptor : SubsystemDescriptorWithProvider
                where TProvider : SubsystemProvider<TSubsystem, TProviderToSubsystem>
                where TProviderToSubsystem : IProviderToSubsystem<TSubsystem>
            {
                return subsystem.provider;
            }
        }
    }
}
