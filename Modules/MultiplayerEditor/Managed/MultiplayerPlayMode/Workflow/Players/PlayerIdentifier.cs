// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Newtonsoft.Json;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class PlayerIdentifier
    {
        [JsonProperty(Required = Required.Always)]
        public Guid Guid { get; internal set; }

        // ensure that there is always a guid since guid is a struct
        [JsonConstructor]
        public PlayerIdentifier(Guid guid)
        {
            Guid = guid;
        }

        public static PlayerIdentifier New()
        {
            return new PlayerIdentifier(Guid.NewGuid());
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerIdentifier identifier
                   && Equals(Guid, identifier.Guid);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Guid);
        }

        public static bool TryParse(string input, out PlayerIdentifier identifier)
        {
            identifier = null;

            if (string.IsNullOrWhiteSpace(input)) return false;

            identifier = TryDeserializeObject(input);
            return identifier != null;
        }

        public override string ToString()
        {
            return $"Guid: {Guid}";
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        static PlayerIdentifier TryDeserializeObject(string input)
        {
            try
            {
                return JsonConvert.DeserializeObject<PlayerIdentifier>(input);
            }
            catch (JsonException e) when (e is JsonSerializationException or JsonReaderException)
            {
                return null;
            }
        }

        public static bool operator ==(PlayerIdentifier lhs, PlayerIdentifier rhs)
        {
            return ReferenceEquals(lhs, null)
                ? ReferenceEquals(rhs, null)
                : lhs.Equals(rhs);
        }

        public static bool operator !=(PlayerIdentifier lhs, PlayerIdentifier rhs)
        {
            return !(lhs == rhs);
        }
    }
}
