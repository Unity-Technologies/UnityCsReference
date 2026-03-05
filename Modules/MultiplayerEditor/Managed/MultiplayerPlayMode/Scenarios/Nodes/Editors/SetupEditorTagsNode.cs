// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class SetupEditorTagsNode : ExecutionNode
    {
        [SerializeReference] public NodeInput<string[]> Tags;
        [SerializeReference] public NodeInput<int> PlayerInstanceIndex;

        public SetupEditorTagsNode(string name) : base(name)
        {
            Tags = new(this);
            PlayerInstanceIndex = new(this);
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var player = MultiplayerPlaymode.Players[GetInput(PlayerInstanceIndex)];
            player.ClearTags(out var error);
            if (error is not TagError.None)
            {
                Debug.LogError($"Failed to clear player tags during Main Editor setup: {error}");
                return Task.CompletedTask;
            }

            var tags = GetInput(Tags) ?? Array.Empty<string>();
            foreach (var tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;

                player.AddTag(tag, out var tagError);

                if (tagError is not TagError.None and not TagError.Duplicate)
                    Debug.LogError($"Could not add tag '{tag}'. Reason: [{tagError}]");
            }

            return Task.CompletedTask;
        }
    }
}
