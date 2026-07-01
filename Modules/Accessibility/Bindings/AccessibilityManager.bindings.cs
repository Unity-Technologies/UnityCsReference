// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.Scripting;

namespace UnityEngine.Accessibility
{
    /// <summary>
    /// Requests and makes updates to the accessibility settings for each platform.
    /// </summary>
    [NativeHeader("Modules/Accessibility/Native/AccessibilityManager.h")]
    [VisibleToOtherModules("UnityEditor.AccessibilityModule")]
    internal class AccessibilityManager
    {
        /// <summary>
        /// Notifications that the operating system can send.
        /// </summary>
        public enum Notification : byte
        {
            /// <summary>
            /// Default notification value.
            /// </summary>
            None,

            /// <summary>
            /// A notification that the operating system sends when the user enables or disables the screen reader.
            /// </summary>
            ScreenReaderStatusChanged,

            /// <summary>
            /// A notification that the operating system sends when an assistive technology focuses on an accessibility
            /// node.
            /// </summary>
            ElementFocused,

            /// <summary>
            /// A notification that the operating system sends when an assistive technology removes its focus from an
            /// accessibility node.
            /// </summary>
            ElementUnfocused,

            /// <summary>
            /// A notification that the operating system sends when the user changes the font scale in the system
            /// settings.
            /// </summary>
            FontScaleChanged,

            /// <summary>
            /// A notification that the operating system sends when the user changes the bold text setting in the system
            /// settings.
            /// </summary>
            BoldTextStatusChanged,

            /// <summary>
            /// A notification that the operating system sends when the user changes the closed captioning setting in
            /// the system settings.
            /// </summary>
            ClosedCaptioningStatusChanged,
        }

        public struct NotificationContext
        {
            /// <summary>
            /// The accessibility node that is currently focused.
            /// </summary>
            /// <remarks>
            /// Present for the <see cref="Notification.ElementFocused"/> and
            /// <see cref="Notification.ElementUnfocused"/> notifications.
            /// </remarks>
            public AccessibilityNode focusedNode { get; set; }

            /// <summary>
            /// The new font scale set by the user in the system settings.
            /// </summary>
            /// <remarks>
            /// Present for the <see cref="Notification.FontScaleChanged"/> notification.
            /// </remarks>
            public float fontScale { get; set; }

            /// <summary>
            /// Whether the user enabled or disabled the bold text system setting.
            /// </summary>
            /// <remarks>
            /// Present for the <see cref="Notification.BoldTextStatusChanged"/> notification.
            /// </remarks>
            public bool isBoldTextEnabled { get; set; }

            /// <summary>
            /// Whether the user enabled or disabled the closed captioning system setting.
            /// </summary>
            /// <remarks>
            /// Present for the <see cref="Notification.ClosedCaptioningStatusChanged"/> notification.
            /// </remarks>
            public bool isClosedCaptioningEnabled { get; set; }

            /// <summary>
            /// Whether the user enabled or disabled the screen reader.
            /// </summary>
            /// <remarks>
            /// Present for the <see cref="Notification.ScreenReaderStatusChanged"/> notification.
            /// </remarks>
            public bool isScreenReaderEnabled { get; set; }

            /// <summary>
            /// The accessibility notification that the operating system sent.
            /// </summary>
            public Notification notification { get; set; }
        }

        // The private constructor ensures that the object is not instantiated from outside the class.
        AccessibilityManager()
        {
        }

        public static AccessibilityManager instance => Nested.s_Instance;

        // Nested class to take advantage of .NET's lazy initialization and thread safety.
        class Nested
        {
            // The explicit static constructor ensures that the C# compiler doesn't mark the type as beforefieldinit.
            static Nested()
            {
            }

            // Read-only property that holds the singleton instance, created the first time it is accessed.
            internal static readonly AccessibilityManager s_Instance = new();
        }

        /// <summary>
        /// Event that is invoked on the main thread when the screen reader is enabled or disabled.
        /// </summary>
        public static event Action<bool> screenReaderStatusChanged;

        /// <summary>
        /// Event that is invoked on the main thread when the screen reader focus changes.
        /// </summary>
        public static event Action<AccessibilityNode> nodeFocusChanged;

        internal static Queue<NotificationContext> asyncNotificationContexts = new();

        bool m_RefreshNodeFramesRequested;

        /// <summary>
        /// Indicates whether our Accessibility support is implemented for the current platform.
        /// </summary>
        public static bool isSupportedPlatform =>
            Application.platform is
                RuntimePlatform.Android or
                RuntimePlatform.IPhonePlayer or
                RuntimePlatform.OSXPlayer or
                RuntimePlatform.WindowsPlayer;

        /// <summary>
        /// Indicates whether a screen reader is enabled.
        /// </summary>
        internal static extern bool IsScreenReaderEnabled();

        /// <summary>
        /// Handles the request for sending an announcement notification to the assistive technology.
        /// </summary>
        internal static extern void SendAnnouncementNotification(string announcement);

        /// <summary>
        /// Handles the request for sending a page scrolled notification to the assistive technology.
        /// </summary>
        internal static extern void SendPageScrolledNotification(string announcement, int nodeId = AccessibilityNodeManager.k_InvalidNodeId);

        /// <summary>
        /// Handles the request for sending a screen changed notification to the assistive technology.
        /// </summary>
        internal static extern void SendScreenChangedNotification(int nodeId = AccessibilityNodeManager.k_InvalidNodeId);

        /// <summary>
        /// Handles the request for sending a layout changed notification to the assistive technology.
        /// </summary>
        internal static extern void SendLayoutChangedNotification(int nodeId = AccessibilityNodeManager.k_InvalidNodeId);

        [RequiredByNativeCode]
        [VisibleToOtherModules("UnityEditor.AccessibilityModule")]
        [ExcludeFromCodeCoverage] // not reachable by the code coverage analysis
        internal static void Internal_Initialize()
        {
            AssistiveSupport.Initialize();
        }

        [RequiredByNativeCode]
        internal static void Internal_Update()
        {
            instance.Internal_Update_Impl();
        }

        void Internal_Update_Impl()
        {
            // Prevent lock if empty.
            if (asyncNotificationContexts.Count == 0)
            {
                return;
            }

            NotificationContext[] contexts;

            lock (asyncNotificationContexts)
            {
                if (asyncNotificationContexts.Count == 0)
                {
                    return;
                }

                contexts = asyncNotificationContexts.ToArray();
                asyncNotificationContexts.Clear();
            }

            using var amLock = GetExclusiveLock();

            foreach (var context in contexts)
            {
                switch (context.notification)
                {
                    case Notification.ScreenReaderStatusChanged:
                    {
                        screenReaderStatusChanged?.Invoke(context.isScreenReaderEnabled);
                        break;
                    }
                    case Notification.ElementFocused:
                    {
                        context.focusedNode.InvokeFocusChanged(true);
                        nodeFocusChanged?.Invoke(context.focusedNode);
                        break;
                    }
                    case Notification.ElementUnfocused:
                    {
                        context.focusedNode.InvokeFocusChanged(false);
                        break;
                    }
                    case Notification.FontScaleChanged:
                    {
                        AccessibilitySettings.InvokeFontScaleChanged(context.fontScale);
                        break;
                    }
                    case Notification.BoldTextStatusChanged:
                    {
                        AccessibilitySettings.InvokeBoldTextStatusChanged(context.isBoldTextEnabled);
                        break;
                    }
                    case Notification.ClosedCaptioningStatusChanged:
                    {
                        AccessibilitySettings.InvokeClosedCaptionStatusChanged(context.isClosedCaptioningEnabled);
                        break;
                    }
                }
            }
        }

        [RequiredByNativeCode]
        internal static void Internal_LateUpdate()
        {
            if (instance.m_RefreshNodeFramesRequested)
            {
                instance.m_RefreshNodeFramesRequested = false;

                AssistiveSupport.activeHierarchy?.RefreshNodeFramesWithoutResetting();
            }
        }

        [RequiredByNativeCode]
        internal static int[] Internal_GetRootNodeIds()
        {
            var rootNodes = AccessibilityHierarchyService.GetRootNodes();

            if (rootNodes == null || rootNodes.Count == 0)
            {
                return null;
            }

            using (ListPool<int>.Get(out var rootNodeIds))
            {
                foreach (var rootNode in rootNodes)
                {
                    rootNodeIds.Add(rootNode.id);
                }

                return rootNodeIds.Count == 0 ? null : rootNodeIds.ToArray();
            }
        }

        [NativeHeader("Modules/Accessibility/Native/AccessibilityManager.h")]
        [FreeFunction("SetAccessibilityNodeDataPtr")]
        internal extern static void SetAccessibilityNodeDataPtr(IntPtr destNodeDataPtr, AccessibilityNodeData sourceNodeData);

        /// <summary>
        /// Returns a struct with information from the managed AccessibilityNode.
        /// </summary>
        internal static bool Internal_GetNode(int nodeId, out AccessibilityNodeData nodeData)
        {
            nodeData = new AccessibilityNodeData();

            if (AccessibilityHierarchyService.TryGetNode(nodeId, out var node))
            {
                node.GetNodeData(ref nodeData);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns a native struct with information from the managed AccessibilityNode.
        /// </summary>
        [RequiredByNativeCode]
        private static bool Internal_GetNode_Native(int nodeId, IntPtr nodeDataPtr)
        {
            if (Internal_GetNode(nodeId, out var nodeData))
            {
                SetAccessibilityNodeDataPtr(nodeDataPtr, nodeData);
                return true;
            }

            return false;
        }

        [RequiredByNativeCode]
        internal static int Internal_GetNodeIdAt(float x, float y)
        {
            return AccessibilityHierarchyService.TryGetNodeAt(x, y, out var node) ?
                node.id : AccessibilityNodeManager.k_InvalidNodeId;
        }

        /// <summary>
        /// Retrieves the ID of the first or last root node in the hierarchy.
        /// </summary>
        [RequiredByNativeCode]
        internal static bool Internal_GetFirstOrLastRootNodeId(bool first, out int managedRootId)
        {
            managedRootId = AccessibilityNodeManager.k_InvalidNodeId;

            var rootNodes = AccessibilityHierarchyService.GetRootNodes();

            if (rootNodes == null)
            {
                return false;
            }

            if (rootNodes.Count != 0)
            {
                managedRootId = first ? rootNodes[0].id : rootNodes[^1].id;
            }

            return true;
        }

        /// <summary>
        /// Retrieves the ID of the first or last child of the managed AccessibilityNode.
        /// </summary>
        [RequiredByNativeCode]
        internal static bool Internal_GetFirstOrLastChildId(int nodeId, bool first, out int childId)
        {
            childId = AccessibilityNodeManager.k_InvalidNodeId;

            if (!AccessibilityHierarchyService.TryGetNode(nodeId, out var node))
            {
                return false;
            }

            if (node.children.Count != 0)
            {
                childId = first ? node.children[0].id : node.children[^1].id;
            }

            return true;
        }

        /// <summary>
        /// Retrieves the ID of the next or previous sibling of the managed AccessibilityNode.
        /// </summary>
        [RequiredByNativeCode]
        internal static bool Internal_GetNextOrPreviousSiblingId(int nodeId, bool next, out int siblingId)
        {
            siblingId = AccessibilityNodeManager.k_InvalidNodeId;

            if (!AccessibilityHierarchyService.TryGetNode(nodeId, out var node))
            {
                return false;
            }

            // If this node has no parent, check for siblings in the root node list.
            var siblingList = node.parent?.children ?? AccessibilityHierarchyService.GetRootNodes();

            if (siblingList == null || siblingList.Count == 0)
            {
                throw new ArgumentException(node.parent == null ?
                    $"Node with ID {nodeId} without parent is not tracked as a root." :
                    $"Node with ID {nodeId} is not a child of its parent.");
            }

            // This node has no siblings.
            if (siblingList.Count == 1)
            {
                return true;
            }

            var index = IndexOf(node, siblingList);
            var siblingIndex = next ? index + 1 : index - 1;

            siblingId = siblingIndex >= 0 && siblingIndex < siblingList.Count ?
                siblingList[siblingIndex].id : AccessibilityNodeManager.k_InvalidNodeId;

            return true;

            static int IndexOf<T>(T elementToFind, IReadOnlyList<T> list)
            {
                var index = 0;

                foreach (var element in list)
                {
                    if (Equals(element, elementToFind))
                    {
                        return index;
                    }

                    index++;
                }

                return -1;
            }
        }

        [RequiredByNativeCode]
        internal static void Internal_OnScreenReaderStatusChanged(bool enabled)
        {
            QueueNotification(new NotificationContext
            {
                notification = Notification.ScreenReaderStatusChanged,
                isScreenReaderEnabled = enabled,
            });
        }

        /// <summary>
        /// Called when the player window is moved or resized either programmatically or by the user.
        /// </summary>
        [RequiredByNativeCode]
        internal static void Internal_OnWindowGeometryChanged()
        {
            instance.m_RefreshNodeFramesRequested = true;
        }

        internal static void QueueNotification(NotificationContext notification)
        {
            instance.QueueNotification_Impl(notification);
        }

        internal void QueueNotification_Impl(NotificationContext notification)
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

        [NativeMethod(IsThreadSafe = true)]
        static extern void Lock();
        [NativeMethod(IsThreadSafe = true)]
        static extern void Unlock();
    }
}
