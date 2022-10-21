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
        public IReadOnlyList<MessageTypeSubscribers> messageTypeSubscribers => m_MessageTypeSubscribers;
        [SerializeField]
        private List<MessageTypeSubscribers> m_MessageTypeSubscribers = new List<MessageTypeSubscribers>();

        private Dictionary<Guid, MessageTypeSubscribers> m_SubscriberLookup;

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

        private void BuildLookup()
        {
            if (m_SubscriberLookup == null)
            {
                m_SubscriberLookup = new Dictionary<Guid, MessageTypeSubscribers>();
                foreach (var subscriber in messageTypeSubscribers)
                    m_SubscriberLookup.Add(subscriber.MessageTypeId, subscriber);
            }
        }

        public void InvokeMessageIdSubscribers(Guid messageId, byte[] data, int playerId)
        {
            BuildLookup();
            if (!m_SubscriberLookup.TryGetValue(messageId, out var eventSubscriber))
            {
                Debug.LogError("No actions found for messageId: " + messageId);
                return;
            }

            var messageEventArg = new MessageEventArgs
            {
                playerId = playerId,
                data = data,
            };
            eventSubscriber.messageCallback.Invoke(messageEventArg);
        }

        public UnityEvent<MessageEventArgs> AddAndCreate(Guid messageId)
        {
            BuildLookup();
            if (!m_SubscriberLookup.TryGetValue(messageId, out var eventSubscriber))
            {
                eventSubscriber = new MessageTypeSubscribers
                {
                    MessageTypeId = messageId,
                    messageCallback = new MessageEvent()
                };
                m_MessageTypeSubscribers.Add(eventSubscriber);
                m_SubscriberLookup.Add(messageId, eventSubscriber);
            }
            eventSubscriber.subscriberCount += 1;
            return eventSubscriber.messageCallback;
        }

        public void UnregisterManagedCallback(Guid messageId, UnityAction<MessageEventArgs> callback)
        {
            BuildLookup();
            if (!m_SubscriberLookup.TryGetValue(messageId, out var eventSubscriber))
                return;
            eventSubscriber.subscriberCount -= 1;
            eventSubscriber.messageCallback.RemoveListener(callback);
            if (eventSubscriber.subscriberCount <= 0)
            {
                m_MessageTypeSubscribers.Remove(eventSubscriber);
                m_SubscriberLookup.Remove(messageId);
            }
        }

        public void Clear()
        {
            if (m_SubscriberLookup != null)
            {
                m_SubscriberLookup.Clear();
            }
            m_MessageTypeSubscribers.Clear();
        }
    }
}
