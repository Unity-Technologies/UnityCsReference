// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class TestResultMessage
    {
        public TestResultMessage(VirtualProjectIdentifier identifier, string callingFilePath, int lineNumber, bool resultCondition, string resultMessage)
        {
            Identifier = identifier;
            CallingFilePath = callingFilePath;
            LineNumber = lineNumber;
            ResultCondition = resultCondition;
            ResultMessage = resultMessage;
        }

        public VirtualProjectIdentifier Identifier { get; }
        public string CallingFilePath { get; }
        public int LineNumber { get; }
        public bool ResultCondition { get; }
        public string ResultMessage { get; }

        static void Serialize(BinaryWriter writer, object message)
        {
            var value = message as TestResultMessage;
            writer.Write(value.Identifier.ToString());
            writer.Write(value.CallingFilePath);
            writer.Write(value.LineNumber);
            writer.Write(value.ResultCondition);
            writer.Write(value.ResultMessage);
        }

        static object Deserialize(BinaryReader reader)
        {
            VirtualProjectIdentifier.TryParse(reader.ReadString(), out var identifier);
            var callingFilePath = reader.ReadString();
            var lineNumber = reader.ReadInt32();
            var condition = reader.ReadBoolean();
            var message = reader.ReadString();
            return new TestResultMessage(identifier, callingFilePath, lineNumber, condition, message);
        }

        [SerializeMessageDelegates] // ReSharper disable once UnusedMember.Global
        public static SerializeMessageDelegates SerializeMethods() => new SerializeMessageDelegates { SerializeFunc = Serialize, DeserializeFunc = Deserialize };
    }
}
