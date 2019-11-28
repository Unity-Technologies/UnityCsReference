// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Threading;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Connect
{
    /// <summary>
    /// A common system to display notifications, targeted for UI Elements.
    /// The notifications can be handled however subscriber wishes, but:
    ///   Easy UIElements implementation -> use UIElementsNotificationSubscriber
    /// </summary>
    internal class NotificationManager
    {
        static readonly NotificationManager k_Instance;

        const long k_DuplicateNotificationMillisecondsThreshold = 1000;

        //TODO eventually: if we want notifications to survive domain reloads (they should), they must be stored in NativeCode, not here
        List<NotificationBuilder> m_NotificationBuilders = new List<NotificationBuilder>();
        long m_NotificationIdCounter;
        Dictionary<Notification.Topic, HashSet<INotificationSubscriber>> m_SubscribersByTopic = new Dictionary<Notification.Topic, HashSet<INotificationSubscriber>>();

        public static NotificationManager instance => k_Instance;

        static NotificationManager()
        {
            k_Instance = new NotificationManager();
        }

        NotificationManager()
        {
            foreach (Notification.Topic topic in Enum.GetValues(typeof(Notification.Topic)))
            {
                m_SubscribersByTopic.Add(topic, new HashSet<INotificationSubscriber>());
            }
        }

        /// <summary>
        /// Will return a deep copy of all notifications
        /// </summary>
        /// <returns></returns>
        public List<Notification> GetAllNotifications()
        {
            var notifications = new List<Notification>();
            foreach (var notificationBuilder in m_NotificationBuilders)
            {
                notifications.Add(notificationBuilder.BuildNotification());
            }

            return notifications;
        }

        /// <summary>
        /// Gets all the current notifications for specified topics
        /// </summary>
        /// <param name="topics"></param>
        /// <returns></returns>
        public IEnumerable<Notification> GetNotificationsForTopics(params Notification.Topic[] topics)
        {
            var notifications = new List<Notification>();
            foreach (var notificationBuilder in m_NotificationBuilders)
            {
                if (topics.Contains(notificationBuilder.topic))
                {
                    notifications.Add(notificationBuilder.BuildNotification());
                }
            }

            return notifications;
        }

        /// <summary>
        /// Publish a new text-only notification
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="severity"></param>
        /// <param name="message"></param>
        /// <param name="avoidDuplicates"></param>
        public void Publish(Notification.Topic topic, Notification.Severity severity, string message, bool avoidDuplicates = true)
        {
            Publish(topic, severity, message, element =>
            {
                var textElement = new TextElement();
                textElement.text = message;
                element.Add(textElement);
            });
        }

        /// <summary>
        /// Publish a new notification
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="severity"></param>
        /// <param name="rawMessage"></param>
        /// <param name="populateNotificationMessage"></param>
        /// <param name="avoidDuplicates"></param>
        public void Publish(Notification.Topic topic, Notification.Severity severity, string rawMessage, Notification.PopulateNotificationMessage populateNotificationMessage, bool avoidDuplicates = true)
        {
            var publishedOn = DateTime.Now.ToUniversalTime();
            foreach (var existingNotificationBuilder in m_NotificationBuilders)
            {
                if (existingNotificationBuilder.topic == topic
                    && existingNotificationBuilder.severity == severity
                    && existingNotificationBuilder.rawMessage == rawMessage
                    && (avoidDuplicates || (publishedOn - existingNotificationBuilder.publishedOn) <= TimeSpan.FromMilliseconds(k_DuplicateNotificationMillisecondsThreshold))
                )
                {
                    //Duplicate messages should at least be a minimum amount of time apart and specifically requested. We don't want spam.
                    return;
                }
            }

            var notificationBuilder = new NotificationBuilder(GetNextNotificationId(), publishedOn, topic, severity, rawMessage, populateNotificationMessage);
            //TODO eventually: add analytics to keep track of this notification
            //TODO eventually: if we want to add auto-dismissal, a notification should either be listening on events or another system needs to keep track of auto-dismiss notifications
            //Chances are the new notification is actually the most recent, so add it as the last element, then sort by desc date if required,
            //but most likely won't do anything and won't take too much time
            m_NotificationBuilders.Add(notificationBuilder);
            m_NotificationBuilders.Sort((notification1, notification2) => notification2.publishedOn.CompareTo(notification1.publishedOn));
            foreach (var subscriber in m_SubscribersByTopic[topic])
            {
                try
                {
                    //We can be vulnerable here since subscribers could crash and prevent other subscribers to receive the notification
                    //Hopefully each Subscriber will be robust enough that it won't happen. But worst case, we will output the error.
                    subscriber.ReceiveNotification(notificationBuilder.BuildNotification());
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        /// <summary>
        /// Dismiss a notification (warns subscribers that a notification was dismissed by a user)
        /// </summary>
        /// <param name="notificationId"></param>
        public void Dismiss(long notificationId)
        {
            NotificationBuilder notificationBuilderToRemove = null;
            for (var i = 0; i < m_NotificationBuilders.Count; i++)
            {
                if (m_NotificationBuilders[i].id == notificationId)
                {
                    notificationBuilderToRemove = m_NotificationBuilders[i];
                    m_NotificationBuilders.RemoveAt(i);
                    break;
                }
            }

            if (notificationBuilderToRemove != null)
            {
                //TODO eventually: add analytics to keep track of this notification dismissal
                foreach (var subscriber in m_SubscribersByTopic[notificationBuilderToRemove.topic])
                {
                    try
                    {
                        //We can be vulnerable here since subscribers could crash and prevent other subscribers to receive the dismissal
                        //Hopefully each Subscriber will be robust enough that it won't happen. But worst case, we will output the error.
                        subscriber.DismissNotification(notificationId);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
        }

        /// <summary>
        /// Subscribe to specified topics to listen to notifications
        /// </summary>
        /// <param name="subscriber"></param>
        /// <param name="topics"></param>
        /// <returns>The list of current notifications for the specified topics</returns>
        public IEnumerable<Notification> Subscribe(INotificationSubscriber subscriber, params Notification.Topic[] topics)
        {
            if (subscriber == null)
            {
                return new List<Notification>();
            }
            foreach (var topic in topics)
            {
                m_SubscribersByTopic[topic].Add(subscriber);
            }

            return GetNotificationsForTopics(topics);
        }

        /// <summary>
        /// Helper method to simplify unsubscribe from everything
        /// </summary>
        /// <param name="subscriber"></param>
        public void UnsubscribeFromAllTopics(INotificationSubscriber subscriber)
        {
            Unsubscribe(subscriber, (Notification.Topic[])Enum.GetValues(typeof(Notification.Topic)));
        }

        /// <summary>
        /// Stop listening to notifications for specified topics
        /// </summary>
        /// <param name="subscriber"></param>
        /// <param name="topics"></param>
        public void Unsubscribe(INotificationSubscriber subscriber, params Notification.Topic[] topics)
        {
            if (subscriber == null)
            {
                return;
            }
            foreach (var topic in topics)
            {
                m_SubscribersByTopic[topic].Remove(subscriber);
            }
        }

        long GetNextNotificationId()
        {
            return Interlocked.Increment(ref m_NotificationIdCounter);
        }

        /// <summary>
        /// This class is used to make sure we do not reuse VisualElements between notifications sent to various subscribers.
        /// VisualElements that are reused end up being stolen between windows that want to display the notification.
        /// So no caching is not a performance issue here: it is a requisite.
        /// </summary>
        private class NotificationBuilder
        {
            public long id { get; }
            public DateTime publishedOn { get; }
            public Notification.Topic topic { get; }
            public Notification.Severity severity { get; }
            public string rawMessage;
            public Notification.PopulateNotificationMessage populateNotificationMessage { get; }

            internal NotificationBuilder(long id, DateTime publishedOn, Notification.Topic topic, Notification.Severity severity,
                                         string rawMessage, Notification.PopulateNotificationMessage populateNotificationMessage)
            {
                this.id = id;
                this.publishedOn = publishedOn;
                this.topic = topic;
                this.severity = severity;
                this.rawMessage = rawMessage;
                this.populateNotificationMessage = populateNotificationMessage;
            }

            internal Notification  BuildNotification()
            {
                var visualElement = new VisualElement();
                populateNotificationMessage.Invoke(visualElement);
                return new Notification(id, publishedOn, topic, severity, rawMessage, visualElement);
            }
        }
    }
}
