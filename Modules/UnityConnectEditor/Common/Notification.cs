// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Connect
{
    /// <summary>
    /// A notification sent by the NotificationManager
    /// </summary>
    internal class Notification
    {
        public long id { get; }
        public DateTime publishedOn { get; }
        public Topic topic { get; }
        public Severity severity { get; }
        public string rawMessage { get; }
        public VisualElement message { get; }

        internal Notification(long id, DateTime publishedOn, Topic topic, Severity severity, string rawMessage, VisualElement message)
        {
            this.id = id;
            this.publishedOn = publishedOn;
            this.topic = topic;
            this.severity = severity;
            this.rawMessage = rawMessage;
            this.message = message;
        }

        /// <summary>
        /// This delegate must be supplied to edit the message content to display.
        /// It will be called every time a copy of this notification needs to be sent.
        /// Do not cache contents to add to the VisualElement, each item must be unique.
        /// </summary>
        /// <param name="visualElement"></param>
        internal delegate void PopulateNotificationMessage(VisualElement visualElement);

        public enum Severity
        {
            Info,
            Warning,
            Error,
        }

        public enum Topic
        {
            //Service Window and related ProjectSettings
            NoProject,
            ProjectBind,
            CoppaCompliance,
            AdsService,
            AnalyticsService,
            BuildService,
            CollabService,
            CrashService,
            PurchasingService,
            UDPService,
        }
    }
}
