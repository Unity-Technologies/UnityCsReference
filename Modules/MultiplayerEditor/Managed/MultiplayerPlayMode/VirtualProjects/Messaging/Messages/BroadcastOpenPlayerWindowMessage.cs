// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class BroadcastOpenPlayerWindowMessage
    {
        public BroadcastOpenPlayerWindowMessage(int playerIndex)
        {
            PlayerIndex = playerIndex;
        }

        public int PlayerIndex { get; }

        static void Serialize(BinaryWriter writer, object message)
        {
            var value = message as BroadcastOpenPlayerWindowMessage;
            writer.Write(value.PlayerIndex);
        }

        static object Deserialize(BinaryReader reader)
        {
            return new BroadcastOpenPlayerWindowMessage(reader.ReadInt32());
        }

        [SerializeMessageDelegates] // ReSharper disable once UnusedMember.Global
        public static SerializeMessageDelegates SerializeMethods() => new SerializeMessageDelegates { SerializeFunc = Serialize, DeserializeFunc = Deserialize };
    }
}
