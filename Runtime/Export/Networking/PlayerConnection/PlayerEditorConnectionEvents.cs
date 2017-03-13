// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

namespace UnityEngine.Networking.PlayerConnection
{
    [Serializable]
    internal class PlayerEditorConnectionEvents
    {
        [SerializeField]
        public List<MessageTypeSubscribers> messageTypeSubscribers = new List<MessageTypeSubscribers>();

        [SerializeField]
        public ConnectionChangeEvent connectionEvent = new ConnectionChangeEvent();

        [SerializeField]
        public ConnectionChangeEvent disconnectionEvent = new ConnectionChangeEvent();

        [Serializable]
        public class MessageEvent : UnityEvent<MessageEventArgs> {}

        [Serializable]
        public class ConnectionChangeEvent : UnityEvent<int> {}

        [Serializable]
        public class MessageTypeSubscribers
        {
            [SerializeField]
            private string m_messageTypeId;

            public Guid MessageTypeId
            {
                get
                {
                    return new Guid(m_messageTypeId);
                }
                set
                {
                    m_messageTypeId = value.ToString();
                }
            }

            public int subscriberCount = 0;

            public MessageEvent messageCallback = new MessageEvent();
        }

        public void InvokeMessageIdSubscribers(Guid messageId, byte[] data, int playerId)
        {
            IEnumerable<MessageTypeSubscribers> messageSubscribers = messageTypeSubscribers.Where(x => x.MessageTypeId == messageId);
            if (!messageSubscribers.Any())
            {
                Debug.LogError("No actions found for messageId: " + messageId);
                return;
            }

            var messageEventArg = new MessageEventArgs
            {
                playerId = playerId,
                data = data,
            };

            foreach (var eventSubscriber in messageSubscribers)
            {
                eventSubscriber.messageCallback.Invoke(messageEventArg);
            }
        }

        public UnityEvent<MessageEventArgs> AddAndCreate(Guid messageId)
        {
            var MessageTypeSubscriber = messageTypeSubscribers.SingleOrDefault(x => x.MessageTypeId == messageId);
            if (MessageTypeSubscriber == null)
            {
                MessageTypeSubscriber = new MessageTypeSubscribers
                {
                    MessageTypeId = messageId,
                    messageCallback = new MessageEvent()
                };

                messageTypeSubscribers.Add(MessageTypeSubscriber);
            }
            MessageTypeSubscriber.subscriberCount++;
            return MessageTypeSubscriber.messageCallback;
        }

        public void UnregisterManagedCallback(Guid messageId, UnityAction<MessageEventArgs> callback)
        {
            var messageTypeSubscriber = messageTypeSubscribers.SingleOrDefault(x => x.MessageTypeId == messageId);

            if (messageTypeSubscriber == null)
            {
                return;
            }
            messageTypeSubscriber.subscriberCount--;
            messageTypeSubscriber.messageCallback.RemoveListener(callback);
            if (messageTypeSubscriber.subscriberCount <= 0)
            {
                messageTypeSubscribers.Remove(messageTypeSubscriber);
            }
        }
    }
}
