// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class SceneHierarchyChangedMessage
    {
        public SceneHierarchy SceneHierarchy { get; }

        public SceneHierarchyChangedMessage(SceneHierarchy sceneHierarchy)
        {
            SceneHierarchy = sceneHierarchy;
        }

        static void Serialize(BinaryWriter writer, object message)
        {
            var value = message as SceneHierarchyChangedMessage;
            SceneHierarchy.Serialize(writer, value.SceneHierarchy);
        }

        static object Deserialize(BinaryReader reader)
        {
            SceneHierarchy.TryParse(reader, out var sceneHierarchy);
            return new SceneHierarchyChangedMessage(sceneHierarchy);
        }

        [SerializeMessageDelegates] // ReSharper disable once UnusedMember.Global
        public static SerializeMessageDelegates SerializeMethods() => new SerializeMessageDelegates { SerializeFunc = Serialize, DeserializeFunc = Deserialize };
    }
}
