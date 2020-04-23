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
    }

    public class SubsystemDescriptorWithProvider<TSubsystem, TProviderToSubsystem> : SubsystemDescriptorWithProvider
        where TSubsystem : SubsystemWithProvider, TProviderToSubsystem, new()
        where TProviderToSubsystem : IProviderToSubsystem<TSubsystem>
    {
        internal override ISubsystem CreateImpl() => this.Create();

        public TSubsystemProxy CreateProxy<TSubsystemProxy>()
            where TSubsystemProxy : SubsystemWithProvider, TProviderToSubsystem, new()
        {
            if (SubsystemManager.FindStandaloneSubsystemByDescriptor(this) != null)
            {
                Debug.LogError(string.Format("Can't create pass-through subsystem '{id}' - descriptor has already been used to create a subsystem is still in use."));
                return null;
            }

            var subsystemProxy = new TSubsystemProxy();
            CreateCommon(ref subsystemProxy, true);
            return subsystemProxy;
        }

        public TSubsystem Create()
        {
            var subsystem = SubsystemManager.FindStandaloneSubsystemByDescriptor(this) as TSubsystem;
            if (subsystem != null)
            {
                if (subsystem.m_Proxy)
                {
                    Debug.LogError(string.Format("Can't create subsystem '{id}' - descriptor already used to create a pass-through subsystem that is in use."));
                    return null;
                }

                return subsystem;
            }

            subsystem = subsystemTypeOverride != null
                ? (TSubsystem)Activator.CreateInstance(subsystemTypeOverride)
                : new TSubsystem();

            CreateCommon(ref subsystem, false);
            return subsystem;
        }

        void CreateCommon<TSubsystemCommon>(ref TSubsystemCommon subsystemCommon, bool proxy)
            where TSubsystemCommon : SubsystemWithProvider, TProviderToSubsystem, new()
        {
            var provider = (SubsystemProvider<TSubsystem, TProviderToSubsystem>)Activator.CreateInstance(providerType);
            if (!provider.TryInitialize())
            {
                subsystemCommon = null;
                return;
            }

            subsystemCommon.SetDescriptor(this);
            subsystemCommon.SetProvider(provider);
            subsystemCommon.m_Proxy = proxy;
            provider.subsystem = subsystemCommon;

            SubsystemManager.AddStandaloneSubsystem(subsystemCommon);
        }
    }
}
