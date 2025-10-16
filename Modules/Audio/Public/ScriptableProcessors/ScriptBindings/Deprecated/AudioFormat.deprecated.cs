// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Audio
{
    /// <undoc/>
    [Obsolete("DSPConfiguration has been deprecated. Use AudioFormat instead. (UnityUpgradable) -> AudioFormat", true)]
    public struct DSPConfiguration
    {
        /// <undoc/>
        [Obsolete("AudioFormat.bufferSize has been deprecated. Use AudioFormat.bufferFrameCount instead. (UnityUpgradable) -> AudioFormat.bufferFrameCount", true)]
        public readonly int bufferSize => throw new NotImplementedException();
    }
}
