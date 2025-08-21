// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class LogMessage
    {
        public VirtualProjectIdentifier Identifier { get; private set; }
        public string Message { get; private set; }
        public string StackTrace { get; private set; }
        public LogType Type { get; private set; }

        public LogMessage(VirtualProjectIdentifier identifier, string message, string stackTrace, LogType type)
        {
            Identifier = identifier;
            Message = message;
            StackTrace = stackTrace;
            Type = type;
        }

        static void Serialize(BinaryWriter writer, object obj)
        {
            var message = obj as LogMessage;

            writer.Write(message.Identifier.ToString());
            writer.Write(message.Message);
            writer.Write(message.StackTrace);
            writer.Write((int)message.Type);
        }

        static object Deserialize(BinaryReader reader)
        {
            VirtualProjectIdentifier.TryParse(reader.ReadString(), out var identifier);
            var message = reader.ReadString();
            var stackTrace = reader.ReadString();
            var type = (LogType)reader.ReadInt32();

            return new LogMessage(identifier, message, stackTrace, type);
        }

        [SerializeMessageDelegates] // ReSharper disable once UnusedMember.Global
        public static SerializeMessageDelegates SerializeMethods() => new() { SerializeFunc = Serialize, DeserializeFunc = Deserialize };
    }
}
