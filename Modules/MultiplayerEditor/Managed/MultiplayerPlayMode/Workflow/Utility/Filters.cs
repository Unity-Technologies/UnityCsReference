// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class Filters
    {
        internal static bool FindFirstPlayerWithVirtualProjectsIdentifier(Dictionary<int, PlayerStateJson> playerStateJSONs, VirtualProjectIdentifier virtualProjectIdentifier, out PlayerStateJson result)
        {
            result = null;

            foreach (var (_, playerStateJson) in playerStateJSONs)
            {
                if (Equals(playerStateJson.TypeDependentPlayerInfo.VirtualProjectIdentifier, virtualProjectIdentifier))
                {
                    result = playerStateJson;
                    return true;
                }
            }

            return false;
        }

        internal static bool FindFirstPlayerWithPlayerType(Dictionary<int, PlayerStateJson> playerStateJSONs, PlayerType playerType, out PlayerStateJson result)
        {
            result = null;

            foreach (var (_, playerStateJson) in playerStateJSONs)
            {
                if (Equals(playerStateJson.Type, playerType))
                {
                    result = playerStateJson;
                    return true;
                }
            }

            return false;
        }
    }
}
