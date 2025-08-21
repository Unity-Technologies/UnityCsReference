// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class SceneSavedMessage
    {
        public string SceneSaved { get; }

        public SceneSavedMessage(string sceneSaved)
        {
            SceneSaved = sceneSaved;
        }

        static void Serialize(BinaryWriter writer, object message)
        {
            var value = message as SceneSavedMessage;
            writer.Write(value.SceneSaved);
        }

        static object Deserialize(BinaryReader reader)
        {
            return new SceneSavedMessage(reader.ReadString());
        }

        [SerializeMessageDelegates] // ReSharper disable once UnusedMember.Global
        public static SerializeMessageDelegates SerializeMethods() => new SerializeMessageDelegates { SerializeFunc = Serialize, DeserializeFunc = Deserialize };
    }
}
