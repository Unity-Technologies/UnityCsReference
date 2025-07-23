// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Accessibility
{
        /// <summary>
        /// Sends accessibility notifications to the screen reader.
        /// </summary>
        /// <remarks>
        /// Sending the same notification type multiple times in quick succession might result in the screen reader
        /// skipping some of the notifications.
        /// </remarks>
        public interface IAccessibilityNotificationDispatcher
        {
            /// <summary>
            /// Requests the screen reader to convey an announcement.
            /// </summary>
            /// <param name="announcement">The announcement text that the screen reader should read.</param>
            /// <remarks>
            /// <para>
            /// Call this method to provide the user with information about events that do not update the application's
            /// user interface, that update it only briefly, or that are not directly related to the user's current
            /// focus. For example, to announce the completion of a background process, such as a file download.
            /// </para>
            /// <para>
            /// The announcement is interrupted if the user focuses on another accessibility node before the screen
            /// reader finishes reading it.
            /// </para>
            /// <para>
            /// **Note**: On iOS, this method has no effect if called from a button's callback.
            /// </para>
            /// </remarks>
            public void SendAnnouncement(string announcement);

            /// <summary>
            /// Notifies the screen reader that a scrolling action completed and optionally requests it to move its
            /// focus to a given accessibility node.
            /// </summary>
            /// <param name="announcement">Text that describes the new scroll position. For example, @@"Tab 3 of 5"@@
            /// or @@"Page 19 of 27"@@. This text is read by the screen reader after processing the notification.
            /// </param>
            /// <param name="nodeToFocus">An optional node to be focused by the screen reader after processing the
            /// notification.</param>
            /// <remarks>
            /// <para>
            /// Call this method after responding to the <see cref="AccessibilityNode.scrolled"/> event to provide the
            /// user with information about the contents of the screen after they performed a scroll gesture, and to
            /// update the screen reader focus accordingly. For example, to move it off an accessibility node that may
            /// have been scrolled out of the screen.
            /// </para>
            /// <para>
            /// On iOS, if this method is repeatedly called with the same <paramref name="announcement"/> text, the
            /// screen reader indicates to the user that scrolling cannot continue due to a border or boundary.
            /// </para>
            /// <para>
            /// **Platform support**: This method has no effect on macOS.
            /// </para>
            /// </remarks>
            // TODO: A11Y-652
            public void SendPageScrolledAnnouncement(string announcement, AccessibilityNode nodeToFocus = null);

            /// <summary>
            /// Notifies the screen reader that the screen changed considerably and optionally requests it to move its
            /// focus to a given accessibility node.
            /// </summary>
            /// <param name="nodeToFocus">An optional node to be focused by the screen reader after processing the
            /// notification. If a node is not provided, the screen reader focuses on the first active node in the
            /// accessibility hierarchy.</param>
            /// <remarks>
            /// <para>
            /// Call this method to notify the screen reader of major changes to the user interface. For example, when
            /// a view appears that occupies a major portion of the screen, or when an automatic refresh updates the
            /// entire content of the screen.
            /// </para>
            /// <para>
            /// Calling this method prompts the screen reader to invalidate its cache of the accessibility hierarchy.
            /// </para>
            /// <para>
            /// **Note**: On Windows and macOS, duplicate calls with the same argument are ignored by the screen reader.
            /// </para>
            /// </remarks>
            public void SendScreenChanged(AccessibilityNode nodeToFocus = null);

            /// <summary>
            /// Notifies the screen reader that the layout of the screen changed and optionally requests it to move its
            /// focus to a given accessibility node.
            /// </summary>
            /// <param name="nodeToFocus">An optional node to be focused by the screen reader after processing the
            /// notification. If a node is not provided and the currently focused node still exists, the screen reader
            /// does not change its focus.</param>
            /// <remarks>
            /// <para>
            /// Call this method to notify the screen reader of minor adjustments to the user interface (for example,
            /// when a dropdown is opened or closed), or to update the screen reader focus as a result of such changes
            /// (for example, to move it off an accessibility node that may have been removed or deactivated so that the
            /// screen reader's focus indicator isn’t stuck on a stale node).
            /// </para>
            /// <para>
            /// Calling this method prompts the screen reader to invalidate its cache of the accessibility hierarchy.
            /// </para>
            /// <para>
            /// **Note**: On Windows and macOS, duplicate calls with the same argument are ignored by the screen reader.
            /// </para>
            /// </remarks>
            public void SendLayoutChanged(AccessibilityNode nodeToFocus = null);
        }
}
