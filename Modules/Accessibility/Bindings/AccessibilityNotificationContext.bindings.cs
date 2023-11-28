// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Accessibility
{
    /// <summary>
    /// Notifications that the app or the operating system can send.
    /// </summary>
    [NativeHeader("Modules/Accessibility/Native/AccessibilityNotificationContext.h")]
    internal enum AccessibilityNotification
    {
        /// Default notification value that should not be sent.
        None = 0,

        /// A notification that an app sends when it needs to convey an
        /// announcement to the screen reader.
        Announcement = 1,

        /// A notification that the operating system sends when the screen
        /// reader finishes reading an announcement.
        AnnouncementFinished = 2,

        /// A notification that the operating system sends when the screen
        /// reader is enabled or disabled.
        ScreenReaderStatusChanged = 3,

        /// A notification that an app sends when a new view appears that
        /// occupies a major portion of the screen.
        ScreenChanged = 4,

        /// A notification that an app sends when the layout of a screen
        /// changes.
        LayoutChanged = 5,

        /// A notification that an app sends when a scroll action completes.
        PageScrolled = 6,

        /// A notification that the operating system sends when an assistive
        /// technology focuses on an accessibility node.
        ElementFocused = 7,

        /// A notification that the operating system sends when an assistive
        /// technology removes focus from an accessibility node.
        ElementUnfocused = 8,

        /// A notification that the operating system sends when the user
        /// changes the font scale in the system settings.
        FontScaleChanged = 9,

        /// A notification that the operating system sends when the bold
        /// text setting changes.
        BoldTextStatusChanged = 10,

        /// A notification that the operating system sends when the closed
        /// captioning setting changes.
        ClosedCaptioningStatusChanged = 11,
    }

    /// <summary>
    /// The context of an accessibility notification.
    /// </summary>
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions.Custom, "MonoAccessibilityNotificationContext")]
    [NativeHeader("Modules/Accessibility/Native/AccessibilityNotificationContext.h")]
    [NativeHeader("Modules/Accessibility/Bindings/AccessibilityNotificationContext.bindings.h")]
    internal struct AccessibilityNotificationContext
    {
        /// <summary>
        /// The accessibility notification that the app or the operating system
        /// sends.
        /// </summary>
        public AccessibilityNotification notification { get; set; }

        /// <summary>
        /// Whether a screen reader is enabled.
        /// <br/><br/>
        /// Present for the following accessibility notifications:
        /// <list type="bullet">
        ///     <item>
        ///         <description><see cref="AccessibilityNotification.ScreenReaderStatusChanged"/></description>
        ///     </item>
        /// </list>
        /// </summary>
        public bool isScreenReaderEnabled { get; }

        /// <summary>
        /// The announcement or description of the new scroll position for the
        /// screen reader to output.
        /// <br/><br/>
        /// Used with the following accessibility notifications:
        /// <list type="bullet">
        ///     <item>
        ///         <description><see cref="AccessibilityNotification.Announcement"/></description>
        ///     </item>
        ///     <item>
        ///         <description><see cref="AccessibilityNotification.AnnouncementFinished"/></description>
        ///     </item>
        ///     <item>
        ///         <description><see cref="AccessibilityNotification.PageScrolled"/></description>
        ///     </item>
        /// </list>
        /// </summary>
        public string announcement { get; set; }

        /// <summary>
        /// Whether the announcement was successful.
        /// <br/><br/>
        /// Present for the following accessibility notifications:
        /// <list type="bullet">
        ///     <item>
        ///         <description><see cref="AccessibilityNotification.AnnouncementFinished"/></description>
        ///     </item>
        /// </list>
        /// </summary>
        public bool wasAnnouncementSuccessful { get; }

        /// <summary>
        /// The accessibility node that is currently focused.
        /// <br/><br/>
        /// Present for the following accessibility notifications:
        /// <list type="bullet">
        ///     <item>
        ///         <description><see cref="AccessibilityNotification.ElementFocused"/></description>
        ///     </item>
        /// </list>
        /// </summary>
        public int currentNodeId { get; }

        /// <summary>
        /// The accessibility node for the screen reader to focus after
        /// processing the notification.
        /// <br/><br/>
        /// Used with the following accessibility notifications:
        /// <list type="bullet">
        ///     <item>
        ///         <description><see cref="AccessibilityNotification.ScreenChanged"/></description>
        ///     </item>
        ///     <item>
        ///         <description><see cref="AccessibilityNotification.LayoutChanged"/></description>
        ///     </item>
        /// </list>
        /// </summary>
        public int nextNodeId { get; set; }
    }
}
