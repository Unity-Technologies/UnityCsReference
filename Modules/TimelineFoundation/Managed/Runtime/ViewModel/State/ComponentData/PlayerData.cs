// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.Model;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct PlayerData : IReadOnlyData
    {
        public readonly bool isPlaying;
        public readonly bool isPreviewing;
        public bool isPaused => !isPlaying;
        public readonly bool isConnected;

        public PlayerData(IPlayer player)
        {
            if (player == null)
            {
                isConnected = false;
                isPlaying = false;
                isPreviewing = false;
            }
            else
            {
                isConnected = player.enabled;
                isPlaying = player.isPlaying;
                isPreviewing = player.isPreviewing;
            }
        }
    }
}
