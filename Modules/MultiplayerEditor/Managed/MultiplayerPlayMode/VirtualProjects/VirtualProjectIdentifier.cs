// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Newtonsoft.Json;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class VirtualProjectIdentifier
    {
        const int k_ProjectIdentifierLength = 8;

        [JsonProperty] readonly string m_Id;
        [JsonProperty] readonly string m_Prefix;

        [JsonIgnore] public string Prefix => m_Prefix;

        [JsonConstructor]
        VirtualProjectIdentifier(string id, string prefix = "")
        {
            m_Id = id;
            m_Prefix = prefix;
        }

        public override string ToString()
        {
            return $"{m_Prefix}{m_Id:N}";
        }

        public override bool Equals(object obj)
        {
            return obj is VirtualProjectIdentifier identifier
                   && Equals(m_Id, identifier.m_Id);
        }

        public override int GetHashCode()
        {
            return m_Id.GetHashCode();
        }

        public static bool operator ==(VirtualProjectIdentifier lhs, VirtualProjectIdentifier rhs)
        {
            return ReferenceEquals(lhs, null)
                ? ReferenceEquals(rhs, null)
                : lhs.Equals(rhs);
        }

        public static bool operator !=(VirtualProjectIdentifier lhs, VirtualProjectIdentifier rhs)
        {
            return !(lhs == rhs);
        }

        public static VirtualProjectIdentifier NewVirtualProjectIdentifier(string prefix = "")
        {
            return new VirtualProjectIdentifier(GenerateShortIdentifier(), prefix);
        }

        public static bool TryParse(string input, out VirtualProjectIdentifier identifier)
        {
            identifier = null;

            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }
            if (input.Length < k_ProjectIdentifierLength)
            {
                return false;
            }

            var potentialGuid = input.Substring(input.Length - k_ProjectIdentifierLength);
            var prefix = input.Replace(potentialGuid, string.Empty);
            identifier = new VirtualProjectIdentifier(potentialGuid, prefix);
            return true;
        }

        public static string GenerateShortIdentifier()
        {
            return Guid.NewGuid().ToString("N").Substring(0, k_ProjectIdentifierLength);
        }
    }
}
