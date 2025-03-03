// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.Scripting;

namespace UnityEngine.Accessibility
{
    /// <summary>
    /// Requests and makes updates to the accessibility settings for each
    /// platform.
    /// </summary>
    [NativeHeader("Modules/Accessibility/Native/AccessibilityManager.h")]
    [VisibleToOtherModules("UnityEditor.AccessibilityModule")]
    internal static class AccessibilityManager
    {
        public struct NotificationContext
        {
            public AccessibilityNotification notification { get; set; }
            public bool isScreenReaderEnabled { get; set; }
            public string announcement { get; set; }
            public bool wasAnnouncementSuccessful { get; set; }
            public AccessibilityNode currentNode { get; set; }
            public AccessibilityNode nextNode { get; set; }
            public float fontScale { get; set; }
            public bool isBoldTextEnabled { get; set; }
            public bool isClosedCaptioningEnabled { get; set; }

            public AccessibilityNotificationContext nativeContext { get; set; }

            public NotificationContext(ref AccessibilityNotificationContext nativeNotification)
            {
                nativeContext = nativeNotification;
                notification = nativeNotification.notification;
                isScreenReaderEnabled = nativeNotification.isScreenReaderEnabled;
                announcement = nativeNotification.announcement;
                wasAnnouncementSuccessful = nativeNotification.wasAnnouncementSuccessful;

                AccessibilityNode node = null;

                AssistiveSupport.activeHierarchy?.TryGetNode(nativeNotification.currentNodeId, out node);
                currentNode = node;
                AssistiveSupport.activeHierarchy?.TryGetNode(nativeNotification.nextNodeId, out node);
                nextNode = node;

                fontScale = 1;
                isBoldTextEnabled = false;
                isClosedCaptioningEnabled = false;
            }
        }

        internal static Queue<NotificationContext> asyncNotificationContexts = new();

        /// <summary>
        /// Indicates whether our Accessibility support is implemented for the current platform.
        /// </summary>
        public static bool isSupportedPlatform =>
            Application.platform is RuntimePlatform.Android or RuntimePlatform.IPhonePlayer;

        /// <summary>
        /// Event that is invoked on the main thread when the screen reader is
        /// enabled or disabled.
        /// </summary>
        public static event Action<bool> screenReaderStatusChanged;

        /// <summary>
        /// Event that is invoked on the main thread when the screen reader
        /// focus changes.
        /// </summary>
        public static event Action<AccessibilityNode> nodeFocusChanged;

        /// <summary>
        /// Indicates whether a screen reader is enabled.
        /// </summary>
        internal static extern bool IsScreenReaderEnabled();

        /// <summary>
        /// Handles the request for sending an accessibility notification to the
        /// assistive technology.
        /// </summary>
        internal static extern void SendAccessibilityNotification(in AccessibilityNotificationContext context);

        /// <summary>
        /// Retrieves the current accessibility language that assistive technologies
        /// use for the application.
        /// </summary>
        internal static extern SystemLanguage GetApplicationAccessibilityLanguage();

        /// <summary>
        /// Sets the accessibility language that assistive technologies use for
        /// the application.
        /// </summary>
        internal static extern void SetApplicationAccessibilityLanguage(SystemLanguage languageCode);

        [RequiredByNativeCode]
        [VisibleToOtherModules("UnityEditor.AccessibilityModule")]
        internal static void Internal_Initialize()
        {
            AssistiveSupport.Initialize();
        }

        [RequiredByNativeCode]
        static void Internal_Update()
        {
            // Prevent lock if empty.
            if (asyncNotificationContexts.Count == 0)
                return;

            NotificationContext[] contexts;

            lock (asyncNotificationContexts)
            {
                if (asyncNotificationContexts.Count == 0)
                    return;

                contexts = asyncNotificationContexts.ToArray();
                asyncNotificationContexts.Clear();
            }

            using var amLock = GetExclusiveLock();

            foreach (var context in contexts)
            {
                switch (context.notification)
                {
                    case AccessibilityNotification.ScreenReaderStatusChanged:
                    {
                        screenReaderStatusChanged?.Invoke(context.isScreenReaderEnabled);
                        break;
                    }
                    case AccessibilityNotification.ElementFocused:
                    {
                        context.currentNode.InvokeFocusChanged(true);
                        nodeFocusChanged?.Invoke(context.currentNode);
                        break;
                    }
                    case AccessibilityNotification.ElementUnfocused:
                    {
                        context.currentNode.InvokeFocusChanged(false);
                        break;
                    }
                    case AccessibilityNotification.FontScaleChanged:
                    {
                        AccessibilitySettings.InvokeFontScaleChanged(context.fontScale);
                        break;
                    }
                    case AccessibilityNotification.BoldTextStatusChanged:
                    {
                        AccessibilitySettings.InvokeBoldTextStatusChanged(context.isBoldTextEnabled);
                        break;
                    }
                    case AccessibilityNotification.ClosedCaptioningStatusChanged:
                    {
                        AccessibilitySettings.InvokeClosedCaptionStatusChanged(context.isClosedCaptioningEnabled);
                        break;
                    }
                }
            }
        }

        [RequiredByNativeCode]
        internal static int[] Internal_GetRootNodeIds()
        {
            var service = AssistiveSupport.GetService<AccessibilityHierarchyService>();
            var rootNodes = service?.GetRootNodes();

            if (rootNodes == null || rootNodes.Count == 0)
            {
                return null;
            }

            using (ListPool<int>.Get(out var rootNodeIds))
            {
                for (var i = 0; i < rootNodes.Count; i++)
                    rootNodeIds.Add(rootNodes[i].id);

                return rootNodeIds.Count == 0 ? null : rootNodeIds.ToArray();
            }
        }

        // Returns a struct with information from the managed AccessibilityNode.
        [RequiredByNativeCode]
        internal static bool Internal_GetNode(int id, ref AccessibilityNodeData nodeData)
        {
            var service = AssistiveSupport.GetService<AccessibilityHierarchyService>();

            if (service == null)
            {
                return false;
            }

            if (service.TryGetNode(id, out var node))
            {
                node.GetNodeData(ref nodeData);
                return true;
            }

            return false;
        }

        [RequiredByNativeCode]
        internal static int Internal_GetNodeIdAt(float x, float y)
        {
            var service = AssistiveSupport.GetService<AccessibilityHierarchyService>();

            return service != null && service.TryGetNodeAt(x, y, out var node) ?
                node.id : AccessibilityNodeManager.k_InvalidNodeId;
        }

        [RequiredByNativeCode]
        internal static void Internal_OnAccessibilityNotificationReceived(ref AccessibilityNotificationContext context)
        {
            // Ignore the global notification and only rely on the per-node notification.
            if (context.notification == AccessibilityNotification.ElementFocused)
                return;

            QueueNotification(new NotificationContext(ref context));
        }

        internal static void QueueNotification(NotificationContext notification)
        {
            lock (asyncNotificationContexts)
            {
                asyncNotificationContexts.Enqueue(notification);
            }
        }

        internal static IDisposable GetExclusiveLock()
        {
            return new ExclusiveLock();
        }

        sealed class ExclusiveLock : IDisposable
        {
            bool m_Disposed;

            public ExclusiveLock()
            {
                Lock();
            }

            ~ExclusiveLock()
            {
                InternalDispose();
            }

            void InternalDispose()
            {
                if (!m_Disposed)
                {
                    Unlock();
                    m_Disposed = true;
                }
            }

            public void Dispose()
            {
                InternalDispose();
                GC.SuppressFinalize(this);
            }
        }

        [ThreadSafe]
        static extern void Lock();
        [ThreadSafe]
        static extern void Unlock();
    }
}
