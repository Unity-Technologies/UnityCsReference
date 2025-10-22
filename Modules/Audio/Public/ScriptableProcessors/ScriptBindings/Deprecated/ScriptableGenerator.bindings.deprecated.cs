// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Audio
{
    /// <undoc/>
    [Obsolete("IGeneratorDefinition has been deprecated. Use IAudioGenerator instead. (UnityUpgradable) -> IAudioGenerator", true)]
    public interface IGeneratorDefinition
    {
    }

    public partial struct GeneratorInstance
    {
        /// <undoc/>
        [Obsolete("IProcessor has been deprecated. Use IRealtime instead. (UnityUpgradable) -> GeneratorInstance/IRealtime", true)]
        public interface IProcessor
        {
        }

        /// <undoc/>
        [Obsolete("GeneratorInstance.Configure has been deprecated. Use ControlContext.Configure instead.", true)]
        public void Configure(ControlContext context, in AudioFormat format)
        {
            throw new NotImplementedException();
        }

        /// <undoc/>
        [Obsolete("GeneratorInstance.Update has been deprecated. Use ControlContext.Update instead.", true)]
        public void Update(ControlContext context)
        {
            throw new NotImplementedException();
        }

        /// <undoc/>
        [Obsolete("GeneratorInstance.Process has been deprecated. Use RealtimeContext.Process instead.", true)]
        public Result Process(RealtimeContext context, ChannelBuffer buffer, Arguments args)
        {
            throw new NotImplementedException();
        }
    }

    /// <undoc/>
    [Obsolete("Generator has been deprecated. Use GeneratorInstance instead. (UnityUpgradable) -> GeneratorInstance", true)]
    public struct Generator
    {
    }
}
