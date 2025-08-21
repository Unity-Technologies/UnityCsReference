// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Accessibility
{
    /// <summary>
    /// Requests and makes updates to the native accessibility nodes.
    /// </summary>
    [NativeHeader("Modules/Accessibility/Native/AccessibilityNodeManager.h")]
    internal static class AccessibilityNodeManager
    {
        internal const int k_InvalidNodeId = -1;

        /// <summary>
        /// Creates a new native accessibility node and populates its fields.
        /// </summary>
        /// <remarks>
        /// Does not create a node if the given ID is already in use.
        /// </remarks>
        /// <returns>@@true@@ if the node was successfully created and @@false@@ otherwise.</returns>
        internal static extern bool CreateNativeNodeWithData(AccessibilityNodeData nodeData);

        /// <summary>
        /// Destroys the native accessibility node with the given ID and removes it from the accessibility hierarchy.
        /// </summary>
        internal static extern void DestroyNativeNode(int nodeId);

        /// <summary>
        /// Sets whether the native accessibility node with the given ID fires accessibility events and can be accessed
        /// by assistive technology.
        /// </summary>
        internal static extern void SetIsActive(int nodeId, bool isActive);

        /// <summary>
        /// Sets the label of the native accessibility node with the given ID.
        /// </summary>
        internal static extern void SetLabel(int nodeId, string label);

        /// <summary>
        /// Sets the value of the native accessibility node with the given ID.
        /// </summary>
        internal static extern void SetValue(int nodeId, string value);

        /// <summary>
        /// Sets the hint of the native accessibility node with the given ID.
        /// </summary>
        internal static extern void SetHint(int nodeId, string hint);

        /// <summary>
        /// Sets the role of the native accessibility node with the given ID.
        /// </summary>
        internal static extern void SetRole(int nodeId, AccessibilityRole role);

        /// <summary>
        /// Sets whether the native accessibility node with the given ID allows direct touch interaction.
        /// </summary>
        internal static extern void SetAllowsDirectInteraction(int nodeId, bool allows);

        /// <summary>
        /// Sets the state of the native accessibility node with the given ID.
        /// </summary>
        internal static extern void SetState(int nodeId, AccessibilityState state);

        /// <summary>
        /// Sets the frame of the native accessibility node with the given ID in screen coordinates.
        /// </summary>
        internal static extern void SetFrame(int nodeId, Rect frame);

        /// <summary>
        /// Sets the node that contains the native accessibility node with the given ID.
        /// </summary>
        /// <remarks>
        /// If a valid parent ID and index are provided, the node is inserted at the given index in its parent's child
        /// list.
        ///
        /// If the parent ID is valid and the index is not, the node is added to the end of its parent's child list.
        ///
        /// If the index is valid and the parent ID is not, the node is inserted at the given index as a root node.
        ///
        /// If neither the parent ID nor the index are valid, the node is added as a root node to the end of the root
        /// node list.
        /// </remarks>
        internal static extern void SetParent(int nodeId, int parentId, int index = -1);

        /// <summary>
        /// Gets whether an assistive technology is focused on the native accessibility node with the given ID.
        /// </summary>
        internal static extern bool GetIsFocused(int nodeId);

        /// <summary>
        /// Called after the native accessibility node with the given ID gains or loses accessibility focus.
        /// </summary>
        [RequiredByNativeCode]
        internal static void Internal_InvokeFocusChanged(int nodeId, bool isNodeFocused)
        {
            if (AccessibilityHierarchyService.TryGetNode(nodeId, out var node))
            {
                node.NotifyFocusChanged(isNodeFocused);
            }
        }

        /// <summary>
        /// Called when the native accessibility node with the given ID is invoked (the same as tapping on the UI
        /// element when assistive technologies are off).
        /// </summary>
        /// <returns>@@true@@ if the node was successfully invoked and @@false@@ otherwise.</returns>
        [RequiredByNativeCode]
        internal static bool Internal_InvokeNodeInvoked(int nodeId)
        {
            return AccessibilityHierarchyService.TryGetNode(nodeId, out var node) && node.InvokeNodeInvoked();
        }

        /// <summary>
        /// Called when the content of the native accessibility node with the given ID is incremented by the screen
        /// reader.
        /// </summary>
        /// <remarks>
        /// On Windows, this event is only supported for nodes with the slider role and whose value contains a number.
        /// </remarks>
        [RequiredByNativeCode]
        internal static bool Internal_InvokeIncremented(int nodeId)
        {
            return AccessibilityHierarchyService.TryGetNode(nodeId, out var node) && node.InvokeIncremented();
        }

        /// <summary>
        /// Called when the content of the native accessibility node with the given ID is decremented by the screen
        /// reader.
        /// </summary>
        /// <remarks>
        /// On Windows, this event is only supported for nodes with the slider role and whose value contains a number.
        /// </remarks>
        [RequiredByNativeCode]
        internal static bool Internal_InvokeDecremented(int nodeId)
        {
            return AccessibilityHierarchyService.TryGetNode(nodeId, out var node) && node.InvokeDecremented();
        }

        /// <summary>
        /// Called when the native accessibility node with the given ID is scrolled by the screen reader in the
        /// specified direction.
        /// </summary>
        [RequiredByNativeCode]
        internal static bool Internal_InvokeScrolled(int nodeId, AccessibilityScrollDirection direction)
        {
            return AccessibilityHierarchyService.TryGetNode(nodeId, out var node) && node.InvokeScrolled(direction);
        }

        /// <summary>
        /// Called when the native accessibility node with the given ID is dismissed.
        /// </summary>
        /// <returns>@@true@@ if the node was successfully dismissed and @@false@@ otherwise.</returns>
        [RequiredByNativeCode]
        internal static bool Internal_InvokeDismissed(int nodeId)
        {
            return AccessibilityHierarchyService.TryGetNode(nodeId, out var node) && node.InvokeDismissed();
        }
    }
}
