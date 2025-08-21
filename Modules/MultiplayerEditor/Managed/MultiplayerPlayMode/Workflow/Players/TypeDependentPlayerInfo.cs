// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class TypeDependentPlayerInfo
    {
        // Type = Main
        /* none for now*/

        // TYPE = Clone
        // NOTE: This will be null when a player has not activated yet
        [JsonProperty] public VirtualProjectIdentifier VirtualProjectIdentifier { get; internal set; }

        // TYPE = DGS
        /* none for now*/

        public override bool Equals(object obj)
        {
            return obj is TypeDependentPlayerInfo identifier
                   && Equals(VirtualProjectIdentifier, identifier.VirtualProjectIdentifier);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(VirtualProjectIdentifier);
        }

        public static TypeDependentPlayerInfo NewEmpty()
        {
            var info = new TypeDependentPlayerInfo { VirtualProjectIdentifier = null };
            return info;
        }

        public static TypeDependentPlayerInfo NewClone(VirtualProjectIdentifier identifier)
        {
            Debug.Assert(identifier != null);
            var info = new TypeDependentPlayerInfo { VirtualProjectIdentifier = identifier };
            return info;
        }

        public override string ToString()
        {
            return $"{nameof(VirtualProjectIdentifier)}: {VirtualProjectIdentifier}";
        }
    }
}
