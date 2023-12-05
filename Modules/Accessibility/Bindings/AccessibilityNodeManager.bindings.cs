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
        /// Creates a new native accessibility node.
        /// </summary>
        /// <remarks>Does not create a node if the given ID is already in use.</remarks>
        /// <returns>Whether the node was successfully created.</returns>
        internal static extern bool CreateNativeNode(int id);

        /// <summary>
        /// Creates a new native accessibility node and populates its fields.
        /// </summary>
        /// <remarks>Does not create a node if the given ID is already in use.</remarks>
        /// <returns>Whether the node was successfully created.</returns>
        internal static extern bool CreateNativeNodeWithData(AccessibilityNodeData nodeData);

        /// <summary>
        /// Destroys the native accessibility node with the given ID and removes
        /// it from the accessibility hierarchy.
        /// </summary>
        internal static extern void DestroyNativeNode(int id, int parentId);

        /// <summary>
        /// Sets the fields of the native accessibility node with the given ID.
        /// </summary>
        internal static extern void SetNodeData(int id, AccessibilityNodeData nodeData);

        /// <summary>
        /// Sets whether the native accessibility node with the given ID fires
        /// accessibility events and can be accessed by assistive technology.
        /// </summary>
        internal static extern void SetIsActive(int id, bool isActive);

        /// <summary>
        /// Sets the label of the native accessibility node with the given ID.
        /// </summary>
        internal static extern void SetLabel(int id, string label);

        /// <summary>
        /// Sets the value of the native accessibility node with the given ID.
        /// </summary>
        internal static extern void SetValue(int id, string value);

        /// <summary>
        /// Sets the hint of the native accessibility node with the given ID.
        /// </summary>
        internal static extern void SetHint(int id, string hint);

        /// <summary>
        /// Sets the role of the native accessibility node with the given ID.
        /// </summary>
        internal static extern void SetRole(int id, AccessibilityRole role);

        /// <summary>
        /// Sets whether the native accessibility node with the given ID allows
        /// direct touch interaction.
        /// </summary>
        internal static extern void SetAllowsDirectInteraction(int id, bool allows);

        /// <summary>
        /// Sets the state of the native accessibility node with the given ID.
        /// </summary>
        internal static extern void SetState(int id, AccessibilityState state);

        /// <summary>
        /// Sets the frame of the native accessibility node with the given ID in
        /// screen coordinates.
        /// </summary>
        internal static extern void SetFrame(int id, Rect frame);

        /// <summary>
        /// Sets the node that contains the native accessibility node with the
        /// given ID.
        /// </summary>
        internal static extern void SetParent(int id, int parentId);

        /// <summary>
        /// Sets the nodes contained by the native accessibility node with the
        /// given ID.
        /// </summary>
        internal static extern void SetChildren(int id, int[] childIds);

        /// <summary>
        /// Gets whether an assistive technology is focused on the native
        /// accessibility node with the given ID.
        /// </summary>
        internal static extern bool GetIsFocused(int id);

        /// <summary>
        /// Sets the accessibility actions that can be performed on the native
        /// accessibility node with the given ID.
        /// </summary>
        internal static extern void SetActions(int id, AccessibilityAction[] actions);

        /// <summary>
        /// Sets the language to use when voicing the native accessibility
        /// node's label, value, and hint (can differ from the system or
        /// application language).
        /// </summary>
        internal static extern void SetLanguage(int id, SystemLanguage language);

        /// <summary>
        /// Called after the native accessibility node with the given ID gains
        /// or loses accessibility focus.
        /// </summary>
        [RequiredByNativeCode]
        internal static void Internal_InvokeFocusChanged(int id, bool isNodeFocused)
        {
            var service = AssistiveSupport.GetService<AccessibilityHierarchyService>();

            if (service == null)
                return;

            if (service.TryGetNode(id, out var node))
            {
                node.NotifyFocusChanged(isNodeFocused);
            }
        }

        /// <summary>
        /// Called when the native accessibility node with the given ID is
        /// selected (the same as tapping on the UI element when
        /// assistive technologies are off).
        /// </summary>
        /// <returns>Whether the node was successfully selected.</returns>
        [RequiredByNativeCode]
        internal static bool Internal_InvokeSelected(int id)
        {
            var service = AssistiveSupport.GetService<AccessibilityHierarchyService>();

            if (service == null)
                return false;

            if (service.TryGetNode(id, out var node))
            {
                return node.InvokeSelected();
            }

            return false;
        }

        /// <summary>
        /// Called when the content of the native accessibility node with the
        /// given ID is incremented by the screen reader.
        /// </summary>
        [RequiredByNativeCode]
        internal static void Internal_InvokeIncremented(int id)
        {
            var service = AssistiveSupport.GetService<AccessibilityHierarchyService>();

            if (service == null)
                return;

            if (service.TryGetNode(id, out var node))
            {
                node.InvokeIncremented();
            }
        }

        /// <summary>
        /// Called when the content of the native accessibility node with the
        /// given ID is decremented by the screen reader.
        /// </summary>
        [RequiredByNativeCode]
        internal static void Internal_InvokeDecremented(int id)
        {
            var service = AssistiveSupport.GetService<AccessibilityHierarchyService>();

            if (service == null)
                return;

            if (service.TryGetNode(id, out var node))
            {
                node.InvokeDecremented();
            }
        }

        /// <summary>
        /// Called when the native accessibility node with the given ID is
        /// dismissed.
        /// </summary>
        /// <returns>True if the node was successfully dismissed, false otherwise.</returns>
        [RequiredByNativeCode]
        internal static bool Internal_InvokeDismissed(int id)
        {
            var service = AssistiveSupport.GetService<AccessibilityHierarchyService>();

            if (service == null)
                return false;

            if (service.TryGetNode(id, out var node))
            {
                return node.Dismissed();
            }

            return false;
        }
    }
}
