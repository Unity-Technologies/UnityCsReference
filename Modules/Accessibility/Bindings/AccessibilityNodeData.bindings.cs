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
    /// Options for defining the role of an <see cref="AccessibilityNode"/> to
    /// assistive technologies.
    /// </summary>
    /// <remarks>
    /// You can use values from this enum to set the
    /// <see cref="AccessibilityNode.role"/>. This property helps inform
    /// assistive technologies how to interact with your
    /// <see cref="AccessibilityNode"/>. Setting accurate roles improves the
    /// usability and user experience of your UI by enabling assistive
    /// technologies to set clear expectations for users. A missing role can
    /// block a user from interacting with your UI, and an incorrect role can
    /// cause confusion and frustration. You can use the default value,
    /// <see cref="AccessibilityRole.None"/>, for nodes whose role is not
    /// described by any of the enum options. Some examples are container
    /// elements and highly customized controls.
    /// </remarks>
    /// <example>
    /// The following example demonstrates assigning an [[AccessibilityRole]] to a
    /// UI element based on its [[UIElements.VisualElement]] type.
    /// <code source="../Tests/AccessibilityExamples/Assets/Examples/AccessibilityManager.cs"/>
    /// </example>
    /// <remarks>
    /// SA: [[wiki:mobile-accessibility]].
    /// </remarks>
    [NativeHeader("Modules/Accessibility/Native/AccessibilityNodeData.h")]
    [Flags]
    public enum AccessibilityRole : ushort
    {
        /// <summary>The accessibility node has no roles.</summary>
        None                    = 0,

        /// <summary>The accessibility node behaves like a button.</summary>
        Button                  = 1 << 0,

        /// <summary>The accessibility node behaves like an image.</summary>
        Image                   = 1 << 1,

        /// <summary>The accessibility node behaves like static text that can't change.</summary>
        StaticText              = 1 << 2,

        /// <summary>The accessibility node behaves like a search field.</summary>
        SearchField             = 1 << 3,

        /// <summary>The accessibility node behaves like a keyboard key.</summary>
        /// <remarks>For Android, this requires at least API level 29.</remarks>
        KeyboardKey             = 1 << 4,

        /// <summary>The accessibility node behaves like a header that divides content into sections (for example, the title of a navigation bar).</summary>
        /// <remarks>For Android, this requires at least API level 28.</remarks>
        Header                  = 1 << 5,

        /// <summary>The accessibility node behaves like an ordered list of tabs.</summary>
        TabBar                  = 1 << 6,

        /// <summary>The accessibility node behaves like a slider. The value of this node can be continuously adjusted through a range.</summary>
        Slider                  = 1 << 7,

        /// <summary>The accessibility node behaves like a toggle.</summary>
        /// <remarks>For iOS, this requires at least iOS 17.</remarks>
        Toggle                  = 1 << 8,
    }

    /// <summary>Describes the state of an accessibility node.</summary>
    [NativeHeader("Modules/Accessibility/Native/AccessibilityNodeData.h")]
    [Flags]
    public enum AccessibilityState : ushort
    {
        /// <summary>The accessibility node is in none of the other states.</summary>
        None            = 0,

        /// <summary>The accessibility node is currently in a disabled state and does not respond to user interaction.</summary>
        Disabled        = 1 << 0,

        /// <summary>The accessibility node is currently in a selected state (for example, a selected row in a table or a selected button within a segmented control).</summary>
        Selected        = 1 << 1,
    }

    /// <summary>
    /// The data stored in an accessibility node.
    /// </summary>
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions.Custom, "MonoAccessibilityNodeData")]
    [NativeHeader("Modules/Accessibility/Bindings/AccessibilityNodeData.bindings.h")]
    [NativeHeader("Modules/Accessibility/Native/AccessibilityNodeData.h")]
    internal struct AccessibilityNodeData
    {
        /// <summary>
        /// The ID of the accessibility node.
        /// </summary>
        public int id { get; set; }

        /// <summary>
        /// Whether the node fires accessibility events and can be accessed by
        /// assistive technology.
        /// </summary>
        public bool isActive { get; set; }

        /// <summary>
        /// A succinct description of the accessibility node.
        /// </summary>
        public string label { get; set; }

        /// <summary>
        /// The current value of the accessibility node.
        /// </summary>
        public string value { get; set; }

        /// <summary>
        /// Additional information about the accessibility node.
        /// For example, the result of performing an action on the node.
        /// </summary>
        public string hint { get; set; }

        /// <summary>
        /// The role of the accessibility node.
        /// </summary>
        public AccessibilityRole role { get; set; }

        /// <summary>
        /// Whether the accessibility node allows direct touch interaction.
        /// </summary>
        /// <remarks>
        /// This is only supported on iOS.
        /// </remarks>
        public bool allowsDirectInteraction { get; set; }

        /// <summary>
        /// The state of the accessibility node.
        /// </summary>
        public AccessibilityState state { get; set; }

        /// <summary>
        /// The frame of the accessibility node in screen coordinates.
        /// </summary>
        public Rect frame { get; set; }

        /// <summary>
        /// The ID of the node that contains the accessibility node.
        /// </summary>
        public int parentId { get; set; }

        /// <summary>
        /// The IDs of the nodes contained by the accessibility node.
        /// </summary>
        public int[] childIds { get; set; }

        /// <summary>
        /// Whether an assistive technology is focused on the accessibility
        /// node.
        /// </summary>
        public bool isFocused { get; }

        /// <summary>
        /// The language to use when voicing the accessibility node's label,
        /// value, and hint (can differ from the system or application
        /// language).
        /// </summary>
        internal SystemLanguage language { get; set; }

        /// <summary>
        /// Whether the accessibility node implements the
        /// <see cref="AccessibilityNode.selected"/> callback.
        /// </summary>
        /// <remarks>
        /// Adds the Click action to nodes on Android.
        /// </remarks>
        public bool implementsSelected { get; set; }

        /// <summary>
        /// Whether the accessibility node implements the
        /// <see cref="AccessibilityNode.dismissed"/> callback.
        /// </summary>
        /// <remarks>
        /// Adds the Dismiss action to nodes on Android.
        /// </remarks>
        public bool implementsDismissed { get; set; }
    }
}
