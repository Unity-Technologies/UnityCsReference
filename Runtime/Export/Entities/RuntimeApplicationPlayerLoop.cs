// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.LowLevel;

namespace Unity.Entities
{
    // Hooks RuntimeApplication's frame events into the player loop, which this module owns
    internal static class RuntimeApplicationPlayerLoop
    {
        sealed class UpdatePreFrame { }
        sealed class UpdatePostFrame { }

        internal static void RegisterFrameUpdateToCurrentPlayerLoop()
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            var playerLoopSystems = new List<PlayerLoopSystem>(playerLoop.subSystemList);
            if (!playerLoopSystems.Exists(s => s.type == typeof(UpdatePreFrame)))
            {
                playerLoopSystems.Insert(0, new PlayerLoopSystem
                {
                    type = typeof(UpdatePreFrame),
                    updateDelegate = RuntimeApplication.InvokePreFrameUpdate
                });
            }
            if (!playerLoopSystems.Exists(s => s.type == typeof(UpdatePostFrame)))
            {
                playerLoopSystems.Add(new PlayerLoopSystem
                {
                    type = typeof(UpdatePostFrame),
                    updateDelegate = RuntimeApplication.InvokePostFrameUpdate
                });
            }
            playerLoop.subSystemList = playerLoopSystems.ToArray();
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        internal static void UnregisterFrameUpdateToCurrentPlayerLoop()
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            var playerLoopSystems = new List<PlayerLoopSystem>(playerLoop.subSystemList);
            playerLoopSystems.RemoveAll(s => s.type == typeof(UpdatePreFrame) || s.type == typeof(UpdatePostFrame));
            playerLoop.subSystemList = playerLoopSystems.ToArray();
            PlayerLoop.SetPlayerLoop(playerLoop);
        }
    }
}
