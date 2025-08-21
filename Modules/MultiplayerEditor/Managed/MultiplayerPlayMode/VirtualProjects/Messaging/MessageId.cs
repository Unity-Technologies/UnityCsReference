// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Newtonsoft.Json;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class MessageId
    {
        [JsonProperty]
        readonly Guid m_Id;

        [JsonConstructor]
        MessageId(Guid id)
        {
            m_Id = id;
        }

        public static bool operator ==(MessageId lhs, MessageId rhs)
        {
            return ReferenceEquals(lhs, null)
                ? ReferenceEquals(rhs, null)
                : lhs.Equals(rhs);
        }

        public static bool operator !=(MessageId lhs, MessageId rhs)
        {
            return !(lhs == rhs);
        }

        public static MessageId NewMessageId()
        {
            return new MessageId(Guid.NewGuid());
        }

        public static bool TryParse(string input, out MessageId identifier)
        {
            identifier = null;

            if (!Guid.TryParse(input, out var guid))
            {
                return false;
            }

            identifier = new MessageId(guid);
            return true;
        }

        public override string ToString()
        {
            return $"{m_Id:N}";
        }

        public override bool Equals(object obj)
        {
            return obj is MessageId identifier
                   && Equals(m_Id, identifier.m_Id);
        }

        public override int GetHashCode()
        {
            return m_Id.GetHashCode();
        }
    }
}
