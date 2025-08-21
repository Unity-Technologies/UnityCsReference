// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class SyncStateMessage
    {
        public CloneState State { get; private set; }

        public SyncStateMessage(CloneState state)
        {
            State = state;
        }

        static void Serialize(BinaryWriter writer, object value)
        {
            var message = value as SyncStateMessage;
            writer.Write(message.State.StreamLogsToMainEditor);
        }

        static object Deserialize(BinaryReader reader)
        {
            var state = new CloneState
            {
                StreamLogsToMainEditor = reader.ReadBoolean()
            };

            return new SyncStateMessage(state);
        }

        [SerializeMessageDelegates] // ReSharper disable once UnusedMember.Global
        public static SerializeMessageDelegates SerializeMethods() => new SerializeMessageDelegates { SerializeFunc = Serialize, DeserializeFunc = Deserialize };
    }
}
