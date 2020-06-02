// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.SubsystemsImplementation
{
    public class SubsystemProxy<TSubsystem, TProvider>
        where TSubsystem : SubsystemWithProvider, new()
        where TProvider : SubsystemProvider<TSubsystem>
    {
        public TProvider provider { get; private set; }

        public bool running
        {
            get => provider.running;
            set => provider.m_Running = value;
        }

        internal SubsystemProxy(TProvider provider) => this.provider = provider;
    }
}
