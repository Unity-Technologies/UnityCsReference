// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class EditorPrefsSyncedMessage
    {
        static void Serialize(BinaryWriter writer, object message) { }

        static object Deserialize(BinaryReader reader) =>
            new EditorPrefsSyncedMessage();

        [SerializeMessageDelegates] // ReSharper disable once UnusedMember.Global
        public static SerializeMessageDelegates SerializeMethods() =>
            new SerializeMessageDelegates { SerializeFunc = Serialize, DeserializeFunc = Deserialize };
    }
}
