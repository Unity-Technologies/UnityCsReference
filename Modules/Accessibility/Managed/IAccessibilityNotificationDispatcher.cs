// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Accessibility
{
        /// <summary>
        /// Sends accessibility notifications to the screen reader.
        /// </summary>
        public interface IAccessibilityNotificationDispatcher
        {
            /// <summary>
            /// Sends a notification to the screen reader conveying an announcement. Use this notification to provide
            /// accessibility information about events that don't update the app's UI, or that update the UI only briefly.
            /// </summary>
            /// <param name="announcement">The string representing the announcement.</param>
            public void SendAnnouncement(string announcement);

            /// <summary>
            /// Sends a notification to the screen reader when the screen has changed considerably. An optional parameter
            /// can be used to request the screen reader focus on a specific node after processing the notification.
            /// </summary>
            /// <param name="nodeToFocus">Optional node to be focused by the screen reader.</param>
            public void SendScreenChanged(AccessibilityNode nodeToFocus = null);

            /// <summary>
            /// Sends a notification to the screen reader when the layout of a screen changes (for example, when an
            /// individual element appears or disappears). An optional parameter can be used to request the screen reader
            /// focus on a specific node after processing the notification.
            /// </summary>
            /// <param name="nodeToFocus">Optional node to be focused by the screen reader.</param>
            public void SendLayoutChanged(AccessibilityNode nodeToFocus = null);
        }
}
