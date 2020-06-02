// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    public abstract class Subsystem : ISubsystem
    {
        abstract public bool running { get; }

        abstract public void Start();
        abstract public void Stop();

        public void Destroy()
        {
            if (SubsystemManager.RemoveDeprecatedSubsystem(this))
                OnDestroy();
        }

        abstract protected void OnDestroy();

        internal ISubsystemDescriptor m_SubsystemDescriptor;
    }

    public abstract class Subsystem<TSubsystemDescriptor>
#pragma warning disable CS0618
        : Subsystem
#pragma warning restore CS0618
        where TSubsystemDescriptor : ISubsystemDescriptor
    {
        public TSubsystemDescriptor SubsystemDescriptor => (TSubsystemDescriptor)m_SubsystemDescriptor;
    }
}
