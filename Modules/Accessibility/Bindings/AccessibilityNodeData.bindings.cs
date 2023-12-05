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
    /// Role of an accessibility node.
    /// </summary>
    [NativeHeader("Modules/Accessibility/Native/AccessibilityNodeData.h")]
    [Flags]
    public enum AccessibilityRole : ushort
    {
        /// The accessibility node has no roles.
        None                    = 0,

        /// The accessibility node behaves like a button.
        Button                  = 1 << 0,

        /// The accessibility node behaves like an image.
        Image                   = 1 << 1,

        /// The accessibility node behaves like static text that can't change.
        StaticText              = 1 << 2,

        /// The accessibility node behaves like a search field.
        SearchField             = 1 << 3,

        /// The accessibility node behaves like a keyboard key.
        KeyboardKey             = 1 << 4,

        /// The accessibility node is a header that divides content into
        /// sections, such as the title of a navigation bar.
        Header                  = 1 << 5,

        /// The accessibility node behaves like a tab bar.
        TabBar                  = 1 << 6,

        /// The accessibility node behaves like a slider, allowing continuous
        /// adjustment through a range of values.
        Slider                  = 1 << 7,

        /// The accessibility node behaves like a toggle.
        Toggle                  = 1 << 8,
    }

    /// <summary>
    /// State of an accessibility node.
    /// </summary>
    [NativeHeader("Modules/Accessibility/Native/AccessibilityNodeData.h")]
    [Flags]
    public enum AccessibilityState : ushort
    {
        /// The accessibility node is in none of the other states.
        None            = 0,

        /// The accessibility node is currently in a disabled state and does not
        /// respond to user interaction.
        Disabled        = 1 << 0,

        /// The accessibility node is currently in a selected state.
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
