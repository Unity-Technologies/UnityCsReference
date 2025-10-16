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
    }

    /// <undoc/>
    [Obsolete("Generator has been deprecated. Use GeneratorInstance instead. (UnityUpgradable) -> GeneratorInstance", true)]
    public struct Generator
    {
    }
}
