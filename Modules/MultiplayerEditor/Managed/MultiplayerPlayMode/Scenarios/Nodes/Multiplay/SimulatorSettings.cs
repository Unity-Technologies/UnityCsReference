// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    struct SimulatorSettings
    {
        public enum ProtocolType
        {
            SQP,
            A2S,
        }

        [Tooltip("Whether to automatically allocate the server on startup.")]
        public bool AutoAllocate;

        public static SimulatorSettings Default => new()
        {
            AutoAllocate = true,
        };
    }
}
