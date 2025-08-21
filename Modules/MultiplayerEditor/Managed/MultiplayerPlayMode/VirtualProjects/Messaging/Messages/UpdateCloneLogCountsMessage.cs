// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class UpdateCloneLogCountsMessage
    {
        public UpdateCloneLogCountsMessage(VirtualProjectIdentifier identifier, LogCounts logCounts)
        {
            Identifier = identifier;
            LogCounts = logCounts;
        }

        public VirtualProjectIdentifier Identifier { get; }
        public LogCounts LogCounts { get; }

        static void Serialize(BinaryWriter writer, object message)
        {
            var value = message as UpdateCloneLogCountsMessage;
            writer.Write(value.Identifier.ToString());
            LogCounts.Serialize(writer, value.LogCounts);
        }

        static object Deserialize(BinaryReader reader)
        {
            VirtualProjectIdentifier.TryParse(reader.ReadString(), out var identifier);
            var logs = LogCounts.Deserialize(reader);
            return new UpdateCloneLogCountsMessage(identifier, logs);
        }

        [SerializeMessageDelegates] // ReSharper disable once UnusedMember.Global
        public static SerializeMessageDelegates SerializeMethods() => new SerializeMessageDelegates { SerializeFunc = Serialize, DeserializeFunc = Deserialize };
    }
}
