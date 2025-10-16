// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Multiplayer.PlayMode.Editor
{
    class CloneContext
    {
        internal CloneContext()
        {
            CloneSystems = new CloneSystems();
            {
                MessagingService = MessagingService.GetClone(CommandLineParameters.ReadCurrentChannelName());
                ProcessSystemDelegates = ProcessSystem.Delegates;
                var internalRuntime = new CloneInternalRuntime();
                internalRuntime.HandleEvents(this);
            }
            CloneSystems.Listen(vpContext: this);
        }

        public MessagingService MessagingService { get; }
        public CloneSystems CloneSystems { get; }
        public ProcessSystemDelegates ProcessSystemDelegates { get; }
    }
}
