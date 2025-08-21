// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    internal struct BuildConfigurationSettings
    {
        public string CommandLineArguments;
        public int CoresCount;
        public int MemoryMiB;
        public int SpeedMhz;
    }
}
