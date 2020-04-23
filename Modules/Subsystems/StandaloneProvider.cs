// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.SubsystemsImplementation
{
    public interface IProviderToSubsystem<TSubsystem> : IRunning
        where TSubsystem : SubsystemWithProvider, new()
    {}

    public abstract class SubsystemProvider
    {}

    public abstract class SubsystemProvider<TSubsystem, TProviderToSubsystem> : SubsystemProvider, IRunning
        where TSubsystem : SubsystemWithProvider, new()
        where TProviderToSubsystem : IProviderToSubsystem<TSubsystem>
    {
        public bool running => subsystem.running;

        protected internal virtual bool TryInitialize() => true;
        public abstract void Start();
        public abstract void Stop();
        public abstract void Destroy();

        protected internal TProviderToSubsystem subsystem { get; internal set; }
    }
}
