// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Unity.Multiplayer.PlayMode.Editor
{
    enum PlayerType
    {
        Main,
        Clone,
    }

    [Serializable]
    class PlayerStateJson
    {
        [JsonProperty] public string Name { get; internal set; } // can be changed in Editor
        [JsonProperty] public List<string> Tags { get; internal set; }
        [JsonProperty] public bool Active { get; internal set; } // can be changed at Runtime
        [JsonProperty] public int Index { get; internal set; } // can NOT be changed
        [JsonProperty] public PlayerType Type { get; internal set; } // can NOT be changed
        [JsonProperty] public PlayerIdentifier PlayerIdentifier { get; internal set; } // can NOT be changed (assigned at player creation)
        [JsonProperty] public TypeDependentPlayerInfo TypeDependentPlayerInfo { get; internal set; } // changed depending on a players needs
        [JsonProperty] public int MultiplayerRole { get; internal set; }    // this is used only when UNITY_USE_MULTIPLAYER_ROLES is active

        public static PlayerStateJson NewMain()
        {
            return new PlayerStateJson
            {
                Name = "Main Editor",
                Active = true,
                Index = 1,
                Type = PlayerType.Main,
                PlayerIdentifier = PlayerIdentifier.New(),
                TypeDependentPlayerInfo = TypeDependentPlayerInfo.NewEmpty(),
                Tags = new List<string>(),
                MultiplayerRole = 1 << 0 | 1 << 1
            };
        }

        public static PlayerStateJson NewClone(int index)
        {
            return new PlayerStateJson
            {
                Name = $"Player {index}",
                Type = PlayerType.Clone,
                Index = index,
                PlayerIdentifier = PlayerIdentifier.New(),
                TypeDependentPlayerInfo = TypeDependentPlayerInfo.NewEmpty(), // The external info becomes known later once its actually activated for the first time
                Tags = new List<string>(),
                MultiplayerRole = 1 << 0 | 1 << 1,
            };
        }

        // We need equal comparison so that we compare JSON and its members like a value type, instead of by reference
        protected bool Equals(PlayerStateJson other)
        {
            if (Tags.Count != other.Tags.Count) return false;

            for (var index = 0; index < Tags.Count; index++)
            {
                if (Tags[index] != other.Tags[index]) return false;
            }

            return Name == other.Name
                   && Active == other.Active
                   && Index == other.Index
                   && Type == other.Type
                   && Equals(PlayerIdentifier, other.PlayerIdentifier)
                   && Equals(TypeDependentPlayerInfo, other.TypeDependentPlayerInfo)
                   && MultiplayerRole == other.MultiplayerRole;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PlayerStateJson)obj);
        }

        // ReSharper disable NonReadonlyMemberInGetHashCode
        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Tags, Active, Index, (int)Type, PlayerIdentifier, TypeDependentPlayerInfo, MultiplayerRole);
        }
        // ReSharper restore NonReadonlyMemberInGetHashCode
    }
}
