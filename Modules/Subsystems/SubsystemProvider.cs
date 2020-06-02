// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.SubsystemsImplementation
{
    public abstract class SubsystemProvider
    {
        public bool running => m_Running;
        internal bool m_Running;
    }

    public abstract class SubsystemProvider<TSubsystem> : SubsystemProvider
        where TSubsystem : SubsystemWithProvider, new()
    {
        protected internal virtual bool TryInitialize() => true;
        public abstract void Start();
        public abstract void Stop();
        public abstract void Destroy();
    }
}
