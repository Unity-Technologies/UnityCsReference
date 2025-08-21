// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    // State we keep on the main editor that is capable of surviving a domain reload
    [Serializable]
    class VirtualProjectStatePerProcessLifetime
    {
        public string[] LaunchArgs;
        public int Retry;
        public bool IsCommunicative;

        public override string ToString()
        {
            return $"{nameof(Retry)}: {Retry}, {nameof(IsCommunicative)}: {IsCommunicative}";
        }
    }
}
