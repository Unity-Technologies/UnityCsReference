// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.MPE;
using UnityEngine;
using Newtonsoft.Json;
using Unity.Multiplayer.PlayMode.Editor;

namespace Unity.Multiplayer.PlayMode.Editor
{
    struct MessagingConnectorDelegates
    {   // This simply represents the static methods of ChannelService and ChannelClient
        internal delegate void Send(byte[] bytes);
        internal delegate void DispatchMessages();

        internal Send SendFunc;
        internal DispatchMessages DispatchMessagesFunc;
    }

    struct MessagingServiceDelegates
    {
        internal delegate void DispatchMessages();
        internal delegate void Send(object message, VirtualProjectIdentifier identifier, Action onAck = null, Action<string> onErrorOrTimeout = null);

        internal DispatchMessages DispatchMessagesFunc;
        internal Send SendFunc;
    }

    class MessagingService
    {
        public const string k_DefaultChannelName = "vp-channel";
        const int k_MessageTimeoutSeconds = 60;
        const string k_HasEditorTickedSessionStateKey = "MPPM_" + nameof(HasEditorTicked);

        readonly IDictionary<Type, List<Action<object>>> m_Handlers = new Dictionary<Type, List<Action<object>>>();
        readonly List<(MessageId MessageId, PendingAck Ack)> m_Acks = new List<(MessageId, PendingAck)>();
        readonly List<Action> m_PendingMessages = new List<Action>();   // NOTE: Lists like these get nuked on a domain reload
        readonly VirtualProjectIdentifier m_TargetIdentifier;
        readonly MessagingConnectorDelegates m_Delegates;

        static bool HasEditorTicked
        {
            get => SessionState.GetBool(k_HasEditorTickedSessionStateKey, false);
            set => SessionState.SetBool(k_HasEditorTickedSessionStateKey, value);
        }

        public static MessagingService GetMain(string channelName)
        {
            return new MessagingService(channelName, false, null);
        }
        public static MessagingService GetClone(string channelName)
        {
            return new MessagingService(channelName, true, VirtualProjectsEditor.CloneIdentifier);
        }

        public static MessagingServiceDelegates GetDelegates(MessagingService messagingService)
        {
            return new MessagingServiceDelegates
            {
                SendFunc = messagingService.Send,
                DispatchMessagesFunc = messagingService.DispatchMessages,
            };
        }

        static MessagingConnectorDelegates GetFromChannelClient(ChannelClient channelClient) => new MessagingConnectorDelegates
        {
            SendFunc = channelClient.Send,
        };

        static MessagingConnectorDelegates GetFromChannelService(string channelName) => new MessagingConnectorDelegates
        {
            SendFunc = bytes => ChannelService.BroadcastBinary(ChannelService.ChannelNameToId(channelName), bytes),
            DispatchMessagesFunc = ChannelService.DispatchMessages,
        };

        // For the clone Editor to connect to the correct
        // web socket server, it needs to be started
        // with the following command line parameters:
        // $"-ump -ump-channel-service-port {ChannelService.GetPort()}"
        internal MessagingService(string channelName, bool isClone, VirtualProjectIdentifier targetIdentifier)
        {
            if (isClone)
            {
                var channelClient = ChannelClient.GetOrCreateClient(channelName);
                channelClient.Start(true);
                channelClient.RegisterMessageHandler(HandleIncomingData);
                m_Delegates = GetFromChannelClient(channelClient);
            }
            else
            {
                ChannelService.GetOrCreateChannel(channelName, (_, bytes) => HandleIncomingData(bytes));
                m_Delegates = GetFromChannelService(channelName);
            }

            m_TargetIdentifier = targetIdentifier;
        }

        public void HandleUpdate()
        {
            HasEditorTicked = true;

            DispatchPendingMessages();

            for (var i = m_Acks.Count - 1; i >= 0; i--)
            {
                var (messageId, ack) = m_Acks[i];
                if ((DateTime.UtcNow - ack.SentTimestamp).TotalSeconds > k_MessageTimeoutSeconds)
                {
                    ack.ErrorCallback?.Invoke($"Timeout [{(DateTime.UtcNow - ack.SentTimestamp).TotalSeconds}>{k_MessageTimeoutSeconds}] reached for client message '{ack.EventType}:{messageId}' for {ack.Sender}");
                    m_Acks.RemoveAt(i);
                }
            }
        }

        public void Send(object message, VirtualProjectIdentifier targetIdentifier, Action onAck = null, Action<string> onErrorOrTimeout = null)
        {
            if (!HasEditorTicked)
            {
                m_PendingMessages.Add(() => Send(message, targetIdentifier, onAck, onErrorOrTimeout));
                return;
            }

            InternalSend(message, targetIdentifier, onAck, onErrorOrTimeout);
        }

        public void Broadcast(object message, Action onAck = null, Action<string> onErrorOrTimeout = null)
        {
            if (!HasEditorTicked)
            {
                m_PendingMessages.Add(() => Broadcast(message, onAck, onErrorOrTimeout));
                return;
            }

            InternalSend(message, null, onAck, onErrorOrTimeout);
        }

        void InternalSend(object message, VirtualProjectIdentifier targetIdentifier, Action onAck, Action<string> onErrorOrTimeout)
        {
            var messageId = MessageId.NewMessageId();
            var requiresAck = false;

            if ((onAck != null || onErrorOrTimeout != null) && (message.GetType() != typeof(AckMessage)))
            {
                requiresAck = true;
                var cloneTargetLog = targetIdentifier != null
                    ? $" from clone {targetIdentifier}"
                    : string.Empty;
                if (MppmLog.AreLogsEnabled() && onAck != null)
                {
                    var realAck = onAck;
                    onAck = () =>
                    {
                        MppmLog.Debug($"Ack Received for {message.GetType().Name}{cloneTargetLog}");

                        realAck.Invoke();
                    };
                }
                m_Acks.Add((messageId, new PendingAck
                {
                    SuccessCallback = onAck,
                    ErrorCallback = onErrorOrTimeout,
                    SentTimestamp = DateTime.UtcNow,
                    EventType = message.GetType().Name,
                    Sender = cloneTargetLog,
                }));
            }

            using var messageStream = new MemoryStream();
            using var messageWriter = new BinaryWriter(messageStream, Encoding.UTF8);
            var type = message.GetType();
            if (SerializeMessageMapping.SerializeMessageDelegatesMap.ContainsKey(type))
            {
                try
                {
                    SerializeMessageMapping.SerializeMessageDelegatesMap[type].SerializeFunc(messageWriter, message);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return;
                }

                var payload = new MessagePayload(
                    messageId,
                    message.GetType().AssemblyQualifiedName,
                    messageStream.ToArray(),
                    requiresAck,
                    targetIdentifier);

                using var payloadStream = new MemoryStream();
                using var payloadWriter = new BinaryWriter(payloadStream, Encoding.UTF8);
                MessagePayload.Serializer.Serialize(payloadWriter, payload);

                m_Delegates.SendFunc(payloadStream.ToArray());

                MppmLog.Debug($"Message Sent: {message.GetType().Name} (to '{targetIdentifier}')\nPayload: {JsonConvert.SerializeObject(message)}");
            }
        }

        public void Receive<TMessage>(Action<TMessage> onMessageReceived)
        {
            if (!m_Handlers.TryGetValue(typeof(TMessage), out var handlers))
            {
                handlers = new List<Action<object>>();
            }

            void Wrapper(object o) => onMessageReceived((TMessage)o);
            handlers.Add(Wrapper);

            m_Handlers[typeof(TMessage)] = handlers;
        }

        void DispatchMessages()
        {
            DispatchPendingMessages();
            Debug.Assert(m_Delegates.DispatchMessagesFunc != null, "This method was not set. Does it support force polling?");
            m_Delegates.DispatchMessagesFunc();
        }

        void DispatchPendingMessages()
        {
            if (m_PendingMessages.Count == 0)
            {
                return;
            }

            var messages = m_PendingMessages.ToArray();
            m_PendingMessages.Clear();
            foreach (var message in messages)
            {
                message.Invoke();
            }
        }

        void HandleIncomingData(byte[] bytes)
        {
            using var payloadStream = new MemoryStream(bytes);
            using var payloadReader = new BinaryReader(payloadStream);
            var messagePayload = MessagePayload.Serializer.Deserialize(payloadReader);

            var type = Type.GetType(messagePayload.Type);
            Debug.Assert(type != null, $"Cannot find type '{messagePayload.Type}'.");

            using var messageStream = new MemoryStream(messagePayload.Data);
            using var messageReader = new BinaryReader(messageStream);
            var hasType = SerializeMessageMapping.SerializeMessageDelegatesMap.ContainsKey(type);
            Debug.Assert(hasType, $"Could not find serializer for message of type {type}");

            object message = null;
            try
            {
                message = SerializeMessageMapping.SerializeMessageDelegatesMap[type].DeserializeFunc(messageReader);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return;
            }

            if (message is AckMessage ackMessage)
            {
                var successfulAcks = new List<(MessageId MessageId, PendingAck Ack)>();
                foreach (var x in m_Acks)
                {
                    if (x.MessageId == ackMessage.MessageId)
                    {
                        successfulAcks.Add(x);
                    }
                }

                // We foreach twice because we can't remove from a collection while iterating
                foreach (var ack in successfulAcks)
                {
                    try
                    {
                        ack.Ack.SuccessCallback?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }

                    m_Acks.Remove(ack);
                }
            }
            else
            {
                var isForTarget = messagePayload.TargetIdentifier == null                  // Is for all
                                  || messagePayload.TargetIdentifier == m_TargetIdentifier;   // Is for us
                if (!isForTarget)
                {
                    return;
                }

                MppmLog.Debug($"Message Received: {message.GetType().Name}\nPayload:{JsonConvert.SerializeObject(message)}");

                var handled = false;
                foreach (var (handlerType, handlerActions) in m_Handlers)
                {
                    if (handlerType != type) continue;  // Only look at handlers of the same type as the message

                    foreach (var handler in handlerActions)
                    {
                        try
                        {
                            handler.Invoke(message);

                            // Within the try/catch so that acknowledgement is not sent if an exception occurs
                            handled = true;
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }

                if (handled && messagePayload.RequiresAck)
                {
                    Broadcast(new AckMessage(messagePayload.MessageId));
                }
            }
        }

        internal class MessagePayload
        {
            public MessagePayload(MessageId messageId, string type, byte[] data, bool requiresAck, VirtualProjectIdentifier targetIdentifier)
            {
                MessageId = messageId;
                Type = type;
                Data = data;
                RequiresAck = requiresAck;
                TargetIdentifier = targetIdentifier;
            }

            public MessageId MessageId { get; }

            public string Type { get; }

            public byte[] Data { get; }

            public bool RequiresAck { get; }

            public VirtualProjectIdentifier TargetIdentifier { get; }

            internal static class Serializer
            {
                public static void Serialize(BinaryWriter writer, MessagePayload value)
                {
                    writer.Write(value.MessageId.ToString());
                    writer.Write(value.Type);
                    writer.Write(value.Data.Length);
                    writer.Write(value.Data);
                    writer.Write(value.RequiresAck);
                    writer.Write(value.TargetIdentifier != null ? value.TargetIdentifier.ToString() : "");  // Use empty string on send all to properly deserialize
                }

                public static MessagePayload Deserialize(BinaryReader reader)
                {
                    MessageId.TryParse(reader.ReadString(), out var messageId);
                    var type = reader.ReadString();
                    var length = reader.ReadInt32();
                    var data = reader.ReadBytes(length);
                    var requiresAck = reader.ReadBoolean();
                    VirtualProjectIdentifier.TryParse(reader.ReadString(), out var targetIdentifier);

                    return new MessagePayload(
                        messageId,
                        type,
                        data,
                        requiresAck,
                        targetIdentifier);
                }
            }
        }
    }
}
