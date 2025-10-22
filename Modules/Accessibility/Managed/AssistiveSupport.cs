// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.Bindings;

namespace UnityEngine.Accessibility
{
    /// <summary>
    /// Access point to APIs that enable applications made with Unity to support assistive technologies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By default, applications made with Unity are incompatible with screen readers because they use Unityʼs own UI
    /// systems, which are not accessible to assistive technologies. This means that while the screen reader is on, it
    /// is impossible to interact with a Unity application.
    /// </para>
    /// <para>
    /// You can use this class, along with <see cref="AccessibilityHierarchy"/> and <see cref="AccessibilityNode"/>, to
    /// make your Unity application natively communicate with and send necessary information to screen readers.
    /// </para>
    /// <para>
    /// These APIs are currently supported on the following platforms:
    ///
    ///- <see cref="RuntimePlatform.Android"/> - starting with Android 8.0 (API level 26)
    ///- <see cref="RuntimePlatform.IPhonePlayer"/>
    ///- <see cref="RuntimePlatform.OSXPlayer"/>
    ///- <see cref="RuntimePlatform.WindowsPlayer"/>
    ///
    /// **Note**: These APIs might result in slight behavior differences across platforms. However, they are consistent
    /// with the behavior of native user interfaces on each platform and conform to user expectations. Their behavior
    /// might not be identical to native user interfaces, but it is a close replica. APIs that behave differently across
    /// platforms have those differences described in their documentation.
    /// </para>
    /// <para>
    /// SA:
    ///
    ///- [[wiki:accessibility|Accessibility for mobile applications]]
    ///- &lt;a href="https://github.com/Unity-Technologies/a11y-public-sample" &gt;Sample project using the accessibility APIs&lt;/a&gt;
    ///- &lt;a href="https://support.google.com/accessibility/android/topic/3529932?ref_topic=9078845" &gt;TalkBack user guide for Android&lt;/a&gt;
    ///- &lt;a href="https://support.apple.com/en-us/guide/iphone/iph3e2e415f/ios" &gt;VoiceOver user guide for iOS&lt;/a&gt;
    ///- &lt;a href="https://support.microsoft.com/en-us/windows/complete-guide-to-narrator-e4397a0d-ef4f-b386-d8ae-c172f109bdb1" &gt;Narrator user guide for Windows&lt;/a&gt;
    ///- &lt;a href="https://support.apple.com/en-us/guide/voiceover/welcome/mac" &gt;VoiceOver user guide for macOS&lt;/a&gt;
    /// </para>
    /// </remarks>
    public static class AssistiveSupport
    {
        internal class NotificationDispatcher : IAccessibilityNotificationDispatcher
        {
            public void SendAnnouncement(string announcement)
            {
                AccessibilityManager.SendAnnouncementNotification(announcement);
            }

            public void SendPageScrolledAnnouncement(string announcement, AccessibilityNode nodeToFocus = null)
            {
                AccessibilityManager.SendPageScrolledNotification(announcement, nodeToFocus?.id ?? AccessibilityNodeManager.k_InvalidNodeId);
            }

            public void SendScreenChanged(AccessibilityNode nodeToFocus = null)
            {
                AccessibilityManager.SendScreenChangedNotification(nodeToFocus?.id ?? AccessibilityNodeManager.k_InvalidNodeId);
            }

            public void SendLayoutChanged(AccessibilityNode nodeToFocus = null)
            {
                AccessibilityManager.SendLayoutChangedNotification(nodeToFocus?.id ?? AccessibilityNodeManager.k_InvalidNodeId);
            }
        }

        /// <summary>
        /// Options to determine the status of the screen reader.
        /// </summary>
        /// <remarks>
        /// You can use the values in this enumeration to set <see cref="AssistiveSupport.screenReaderStatusOverride"/>
        /// and force <see cref="AssistiveSupport.isScreenReaderEnabled"/> to return a specific value.
        /// </remarks>
        public enum ScreenReaderStatusOverride : byte
        {
            /// <summary>
            /// The screen reader status is determined by the operating system.
            /// </summary>
            OSDriven,

            /// <summary>
            /// The screen reader is considered enabled, regardless of the status given by the operating system.
            /// </summary>
            ForceEnabled,

            /// <summary>
            /// The screen reader is considered disabled, regardless of the status given by the operating system.
            /// </summary>
            ForceDisabled,
        }

        /// <summary>
        /// Service used to send accessibility notifications to the screen reader.
        /// </summary>
        public static IAccessibilityNotificationDispatcher notificationDispatcher { get; } = new NotificationDispatcher();

        /// <summary>
        /// Event invoked on the main thread when the user turns the screen reader on or off.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Subscribe to this event to activate your <see cref="AccessibilityHierarchy"/> (see
        /// <see cref="AssistiveSupport.activeHierarchy"/>) and if you need to customize your application’s interface
        /// for screen reader users.
        /// </para>
        /// <para>
        /// You can also use <see cref="AssistiveSupport.isScreenReaderEnabled"/> to determine whether the screen reader
        /// is turned on or off.
        /// </para>
        /// <para>
        /// **Platform support**: This event is not supported by Narrator, the Windows built-in screen reader.
        /// </para>
        /// </remarks>
        /// <example>
        /// The following example demonstrates a potential workaround for polling the status of Narrator and sending a
        /// custom event.
        /// <code source="../Tests/AccessibilityExamples/Assets/Examples/NarratorStatusManager.cs"/>
        /// </example>
        public static event Action<bool> screenReaderStatusChanged;

        static event Action<AccessibilityHierarchy> s_ActiveHierarchyChanged;

        /// <summary>
        /// Event invoked when the active accessibility hierarchy is changed.
        /// </summary>
        internal static event Action<AccessibilityHierarchy> activeHierarchyChanged
        {
            [VisibleToOtherModules("UnityEditor.AccessibilityModule")]
            add => s_ActiveHierarchyChanged += value;
            [VisibleToOtherModules("UnityEditor.AccessibilityModule")]
            remove => s_ActiveHierarchyChanged -= value;
        }

        /// <summary>
        /// Event invoked on the main thread when the user changes the screen reader focus by navigating to a different
        /// accessibility node.
        /// </summary>
        /// <remarks>
        /// Subscribe to this event if you need to know when the screen reader focus changes. For example, to scroll the
        /// visual element represented by the focused node into view if it is not currently visible.
        /// </remarks>
        public static event Action<AccessibilityNode> nodeFocusChanged;

        /// <summary>
        /// The accessibility hierarchy that is currently accessible to screen readers.
        /// </summary>
        /// <remarks>
        /// <para>
        /// An active <see cref="AccessibilityHierarchy"/> is required to make the content of an application accessible
        /// to screen reader users.
        /// </para>
        /// <para>
        /// To manage system resources efficiently, setting this property only takes effect if the screen reader is on,
        /// where the value of <see cref="AssistiveSupport.isScreenReaderEnabled"/> is @@true@@. Similarly, when the
        /// screen reader is turned off, so when the <see cref="AssistiveSupport.screenReaderStatusChanged"/> event is
        /// sent with a @@false@@ parameter or when <see cref="AssistiveSupport.screenReaderStatusOverride"/> is set to
        /// <see cref="AssistiveSupport.ScreenReaderStatusOverride.ForceDisabled"/>, this property is automatically set
        /// to @@null@@. You must set it each time the screen reader is turned on.
        /// </para>
        /// <para>
        /// When this property is set, Unity notifies the screen reader of the new hierarchy by calling
        /// <see cref="IAccessibilityNotificationDispatcher.SendScreenChanged"/> (with a @@null@@ parameter).
        /// </para>
        /// </remarks>
        public static AccessibilityHierarchy activeHierarchy
        {
            get => AccessibilityHierarchyService.activeHierarchy;
            set
            {
                // In the Editor context, we always accept an active hierarchy in order to allow users to debug it
                // (using the Accessibility Hierarchy Viewer, for instance) in play mode even if the Editor platform is
                // not supported or the screen reader is off.

                if (!Application.isEditor && !AccessibilityManager.isSupportedPlatform)
                {
                    Debug.LogError($"{nameof(activeHierarchy)} is not supported on {Application.platform}. " +
                        "Please refer to the documentation for supported platforms.");
                    return;
                }

                if (isScreenReaderEnabled || Application.isEditor)
                {
                    using var amlock = AccessibilityManager.GetExclusiveLock();

                    AccessibilityHierarchyService.activeHierarchy = value;
                    s_ActiveHierarchyChanged?.Invoke(value);
                }
            }
        }

        /// <summary>
        /// Whether the screen reader is turned on or off on the user's device.
        /// </summary>
        /// <remarks>
        /// <para>
        /// You can use this property to customize your application’s interface for screen reader users. For example,
        /// you might want visual elements that usually disappear quickly to persist onscreen for screen reader users.
        /// </para>
        /// <para>
        /// You can also subscribe to the <see cref="AssistiveSupport.screenReaderStatusChanged"/> event to determine
        /// when the user turns the screen reader on or off.
        /// </para>
        /// <para>
        /// To manage system resources efficiently, setting <see cref="AssistiveSupport.activeHierarchy"/> only takes
        /// effect if the value of this property is @@true@@.
        /// </para>
        /// </remarks>
        public static bool isScreenReaderEnabled => screenReaderStatusOverride switch
        {
            ScreenReaderStatusOverride.ForceEnabled => true,
            ScreenReaderStatusOverride.ForceDisabled => false,
            _ => AccessibilityManager.IsScreenReaderEnabled()
        };

        static ScreenReaderStatusOverride s_ScreenReaderStatusOverride;

        /// <summary>
        /// Whether to override the screen reader status given by the operating system.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Set this property if you need to override the value returned by
        /// <see cref="AssistiveSupport.isScreenReaderEnabled"/>.
        /// </para>
        /// <para>
        /// If this property is set to <see cref="AssistiveSupport.ScreenReaderStatusOverride.OSDriven"/>, its default
        /// value, <see cref="AssistiveSupport.isScreenReaderEnabled"/> returns the actual screen reader status given by
        /// the operating system. Otherwise, this property forces <see cref="AssistiveSupport.isScreenReaderEnabled"/>
        /// to return a specific value, regardless of the status given by the operating system.
        /// </para>
        /// <para>
        /// **Note**: This property does not affect the actual status of the screen reader on the user's device.
        /// </para>
        /// </remarks>
        public static ScreenReaderStatusOverride screenReaderStatusOverride
        {
            get => s_ScreenReaderStatusOverride;
            set
            {
                if (s_ScreenReaderStatusOverride == value)
                {
                    return;
                }

                s_ScreenReaderStatusOverride = value;

                if (!isScreenReaderEnabled && !Application.isEditor)
                {
                    AccessibilityHierarchyService.activeHierarchy = null;
                }
            }
        }

        [ExcludeFromCodeCoverage] // not reachable by the code coverage analysis
        internal static void Initialize()
        {
            AccessibilityManager.screenReaderStatusChanged += ScreenReaderStatusChanged;
            AccessibilityManager.nodeFocusChanged += NodeFocusChanged;
        }

        internal static void ScreenReaderStatusChanged(bool enabled)
        {
            if (!isScreenReaderEnabled && !Application.isEditor)
            {
                AccessibilityHierarchyService.activeHierarchy = null;
            }

            screenReaderStatusChanged?.Invoke(enabled);
        }

        static void NodeFocusChanged(AccessibilityNode currentNode)
        {
            nodeFocusChanged?.Invoke(currentNode);
        }
    }
}
