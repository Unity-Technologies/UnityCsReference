// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Audio
{
    partial struct ProcessorInstance
    {
        partial struct CreationParameters
        {
            /// <undoc/>
            [Obsolete("processorUpdateSetting has been deprecated. Use realtimeUpdateSetting instead.", true)]
            public UpdateSetting processorUpdateSetting { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        }

        /// <undoc/>
        [Obsolete("IProcessor has been deprecated. Use IRealtime instead. (UnityUpgradable) -> ProcessorInstance/IRealtime", true)]
        public interface IProcessor
        {
        }

        /// <undoc/>
        [Obsolete("MessageStatus has been deprecated. Use Response instead. (UnityUpgradable) -> ProcessorInstance/Response", true)]
        public enum MessageStatus
        {
        }
    }

    /// <undoc/>
    [Obsolete("Processor has been deprecated. Use ProcessorInstance instead. (UnityUpgradable) -> ProcessorInstance", true)]
    public struct Processor
    {
    }

    /// <undoc/>
    [Obsolete("ProcessingContext has been deprecated. Use RealtimeContext instead. (UnityUpgradable) -> RealtimeContext", true)]
    public struct ProcessingContext
    {
    }

    /// <undoc/>
    [Obsolete("IAudioScriptingContext has been deprecated. Use ProcessorInstance.IContext instead. (UnityUpgradable) -> ProcessorInstance/IContext", true)]
    public interface IAudioScriptingContext
    {
    }
}
