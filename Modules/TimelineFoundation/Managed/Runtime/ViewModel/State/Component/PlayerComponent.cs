// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.Model;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal class PlayerComponent : Component<PlayerData>
    {
        public IPlayer playerModel { get; }

        public PlayerComponent(IPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            playerModel = player;

            IPlayerEvents events = playerModel.playerEvents;
            events.OnPlay += OnPlayerChanged;
            events.OnPause += OnPlayerChanged;
            events.OnStop += OnPlayerChanged;
            events.OnEnable += OnPlayerChanged;
            events.OnDisable += OnPlayerChanged;
            events.OnPlayerDataChanged += OnPlayerChanged;
        }

        public override void Dispose()
        {
            IPlayerEvents events = playerModel.playerEvents;
            events.OnPlay -= OnPlayerChanged;
            events.OnPause -= OnPlayerChanged;
            events.OnStop -= OnPlayerChanged;
            events.OnEnable -= OnPlayerChanged;
            events.OnDisable -= OnPlayerChanged;
            events.OnPlayerDataChanged -= OnPlayerChanged;
        }

        protected override PlayerData GenerateReadOnlyData()
        {
            return new PlayerData(playerModel);
        }

        void OnPlayerChanged()
        {
            MarkAsDirty();
        }

        public override string ToString()
        {
            return playerModel.ToString();
        }
    }
}
