// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class PlayerInitializedMessage
    {
        public PlayerInitializedMessage(VirtualProjectIdentifier identifier)
        {
            Identifier = identifier;
        }

        public VirtualProjectIdentifier Identifier { get; }

        static void Serialize(BinaryWriter writer, object message)
        {
            var value = message as PlayerInitializedMessage;
            writer.Write(value.Identifier.ToString());
        }

        static object Deserialize(BinaryReader reader)
        {
            VirtualProjectIdentifier.TryParse(reader.ReadString(), out var identifier);
            return new PlayerInitializedMessage(identifier);
        }

        [SerializeMessageDelegates] // ReSharper disable once UnusedMember.Global
        public static SerializeMessageDelegates SerializeMethods() => new SerializeMessageDelegates { SerializeFunc = Serialize, DeserializeFunc = Deserialize };
    }
}
