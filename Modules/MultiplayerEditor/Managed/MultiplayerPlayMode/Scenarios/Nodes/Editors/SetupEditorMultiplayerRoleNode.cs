// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Multiplayer.Internal;
using UnityEngine;
using UnityEngine.Multiplayer.Internal;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class SetupEditorMultiplayerRoleNode : ExecutionNode
    {
        [SerializeReference] public NodeInput<MultiplayerRoleFlags> Role;
        [SerializeReference] public NodeInput<int> PlayerInstanceIndex;

        public SetupEditorMultiplayerRoleNode()
        {
            Role = new(this);
            PlayerInstanceIndex = new(this);
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var role = GetInput(Role);

            if ((role & MultiplayerRoleFlags.ClientAndServer) == 0 ||
                (role & ~MultiplayerRoleFlags.ClientAndServer) != 0)
                return Task.CompletedTask;

            var playerIndex = GetInput(PlayerInstanceIndex);
            if (playerIndex == 0)
            {
                EditorMultiplayerManager.activeMultiplayerRoleMask = role;
            }
            else
            {
                var player = MultiplayerPlaymode.Players[playerIndex];
                player.Role = role;
            }
            return Task.CompletedTask;
        }
    }
}
