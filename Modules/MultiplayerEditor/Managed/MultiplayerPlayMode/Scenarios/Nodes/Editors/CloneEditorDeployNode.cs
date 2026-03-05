// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Multiplayer.Internal;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class CloneEditorDeployNode : ExecutionNode
    {
        bool m_HasConnected;

        [SerializeReference] public NodeInput<int> PlayerInstanceIndex;
        [SerializeReference] public NodeOutput<PlayerIdentifier> PlayerIdentifier; // Nodes needs to be public fields since they are serialized
        [SerializeReference] public NodeOutput<TypeDependentPlayerInfo> TypeDependentPlayerInfo; // Nodes needs to be public fields since they are serialized

        public bool IsRunning()
        {
            var player = MultiplayerPlaymode.Players[GetInput(PlayerInstanceIndex)];
            return player.PlayerState == PlayerState.Launched;
        }

        public CloneEditorDeployNode(string name) : base(name)
        {
            PlayerInstanceIndex = new(this);

            PlayerIdentifier = new(this);
            TypeDependentPlayerInfo = new(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var playerInstanceIndex = GetInput(PlayerInstanceIndex);
            var player = MultiplayerPlaymode.Players[playerInstanceIndex];
            var args = new List<string> { CommandLineParameters.k_ScenarioClone };

            try
            {
                DebugUtils.Trace($"Deploy started for '{player.Name}'");
                var hasActivated = player.Activate(out _, args);
                DebugUtils.Trace(hasActivated
                    ? $"Successfully activated '{player.Name}'"
                    : $"Failed to activate '{player.Name}'");

                if (hasActivated)
                {
                    // Activating at first could take a while
                    // 1. Could be symbolic linking the MPPM folder
                    // 2. Could be launching the process
                    while (player.PlayerState != PlayerState.Launched)
                    {
                        await Task.Delay(3000, cancellationToken);

                        if (player.PlayerState != PlayerState.Launched)
                        {
                            DebugUtils.Trace($"'{player.Name}' is not in the ready state");
                        }
                        else
                        {
                            if (!m_HasConnected)
                            {
                                m_HasConnected = true;
                                DebugUtils.Trace($"'{player.Name}' is ready!");
                            }
                        }
                    }
                }

                DebugUtils.Trace($"Deploy finished for '{player.Name}'");

                SetOutput(PlayerIdentifier, player.PlayerIdentifier);
                SetOutput(TypeDependentPlayerInfo, player.TypeDependentPlayerInfo);

                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                DebugUtils.Trace($"Play Mode cancelled, deactivating '{player.Name}'.");
                player.Deactivate(out _);
                return;
            }
        }

        protected override Task ExecuteResumeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
