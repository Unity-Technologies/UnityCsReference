// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Audio;

namespace UnityEngine
{
    /// <undoc/>
    public sealed partial class AudioSource : AudioBehaviour
    {
        /// <undoc/>
        [Obsolete("AudioSource.generatorDefinition has been deprecated. Use AudioSource.generator instead. (UnityUpgradable) -> generator", true)]
        public IAudioGenerator generatorDefinition
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <undoc/>
        [Obsolete("AudioSource.generatorHandle has been deprecated. Use AudioSource.generatorInstance instead. (UnityUpgradable) -> generatorInstance", true)]
        public unsafe ProcessorInstance generatorHandle
        {
            get => throw new NotImplementedException();
        }
    }
}
