// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling;

namespace UnityEngine.SubsystemsImplementation
{
    ///<summary>A subsystem is initialized from a SubsystemDescriptorWithProvider for a given subsystem (Session, Plane, Face, etc.) and provides an interface to interact with that given subsystem until it is Destroyed. After a subsystem is created, it can be Started or Stopped to turn on and off functionality and preserve performance. The base type for the subsystem only exposes this functionality; this class is designed to be a base class for derived classes that expose more functionality specific to a given subsystem.
    ///
    ///*Note:* Initializing a second subsystem from the same subsystem descriptor will return a reference to the existing subsystem, because only one subsystem is currently allowed for a single subsystem provider.</summary>
    public abstract class SubsystemWithProvider : ISubsystem
    {
        static readonly ProfilerMarker k_StartMarker = new ProfilerMarker("SubsystemWithProvider.Start");
        static readonly ProfilerMarker k_StopMarker = new ProfilerMarker("SubsystemWithProvider.Stop");
        static readonly ProfilerMarker k_DestroyMarker = new ProfilerMarker("SubsystemWithProvider.Destroy");

        ///<summary>Starts an instance of a subsystem.
        ///
        ///Once the instance is started, the subsystem representing this instance is active and can be interacted with.</summary>
        public void Start()
        {
            using var _ = k_StartMarker.Auto();
            if (m_Destroyed)
                throw new ArgumentNullException(null, $"Cannot start {GetType().Name} because it has been destroyed. Create a new instance from its descriptor instead.");

            if (running)
                return;

            OnStart();
            providerBase.m_Running = true;
            running = true;
        }

        ///<exclude />
        protected abstract void OnStart();

        ///<summary>Stops an instance of a subsystem.
        ///
        ///Once the instance is stopped, the subsystem representing this instance is no longer active and should not consume CPU resources.</summary>
        public void Stop()
        {
            using var _ = k_StopMarker.Auto();
            if (!running)
                return;

            OnStop();
            providerBase.m_Running = false;
            running = false;
        }

        ///<exclude />
        protected abstract void OnStop();

        ///<summary>Destroys this instance of a subsystem.
        ///
        ///Also unloads all resources acquired during the initialization step. Call this when you no longer need this instance of a subsystem.
        ///
        ///Note: Once a subsystem is Destroyed, script can still hold a reference but calling a method on it will result in a NullArgumentException.</summary>
        public void Destroy()
        {
            using var _ = k_DestroyMarker.Auto();
            Stop();
            if (SubsystemManager.RemoveStandaloneSubsystem(this))
            {
                m_Destroyed = true;
                OnDestroy();
            }
        }

        ///<exclude />
        protected abstract void OnDestroy();

        ///<summary>Whether or not the subsystem is running.
        ///
        ///This returns true after Start has been called on the subsystem, and false after Stop is called.</summary>
        public bool running { get; private set; }
        internal SubsystemProvider providerBase { get; set; }
        bool m_Destroyed;

        internal abstract void Initialize(SubsystemDescriptorWithProvider descriptor, SubsystemProvider subsystemProvider);
        internal abstract SubsystemDescriptorWithProvider descriptor { get; }
    }

    ///<exclude />
    public abstract class SubsystemWithProvider<TSubsystem, TSubsystemDescriptor, TProvider> : SubsystemWithProvider
        where TSubsystem : SubsystemWithProvider, new()
        where TSubsystemDescriptor : SubsystemDescriptorWithProvider
        where TProvider : SubsystemProvider<TSubsystem>
    {
        static readonly ProfilerMarker k_InitializeMarker = new ProfilerMarker("SubsystemWithProvider.Initialize");
        static readonly ProfilerMarker k_CreateMarker = new ProfilerMarker("SubsystemWithProvider.OnCreate");

        ///<exclude />
        public TSubsystemDescriptor subsystemDescriptor { get; private set; }

        protected internal TProvider provider { get; private set; }

        ///<exclude />
        protected virtual void OnCreate() {}
        ///<exclude />
        protected override void OnStart() => provider.Start();
        ///<exclude />
        protected override void OnStop() => provider.Stop();
        ///<exclude />
        protected override void OnDestroy() => provider.Destroy();

        internal override sealed void Initialize(SubsystemDescriptorWithProvider descriptor, SubsystemProvider provider)
        {
            using var _ = k_InitializeMarker.Auto();
            providerBase = provider;
            this.provider = (TProvider)provider;
            subsystemDescriptor = (TSubsystemDescriptor)descriptor;
            using (k_CreateMarker.Auto())
                OnCreate();
        }

        internal override sealed SubsystemDescriptorWithProvider descriptor => subsystemDescriptor;
    }

    namespace Extensions
    {
        ///<exclude />
        public static class SubsystemExtensions
        {
            ///<exclude />
            public static TProvider GetProvider<TSubsystem, TDescriptor, TProvider>(
                this SubsystemWithProvider<TSubsystem, TDescriptor, TProvider> subsystem)
                where TSubsystem : SubsystemWithProvider, new()
                where TDescriptor : SubsystemDescriptorWithProvider<TSubsystem, TProvider>
                where TProvider : SubsystemProvider<TSubsystem>
            {
                return subsystem.provider;
            }
        }
    }
}
