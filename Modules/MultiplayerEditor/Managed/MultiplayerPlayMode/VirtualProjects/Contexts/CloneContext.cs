// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: HeadlessRuntime not yet converted
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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
