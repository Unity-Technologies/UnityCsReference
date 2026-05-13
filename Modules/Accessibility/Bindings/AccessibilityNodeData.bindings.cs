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
    /// Options for defining the role of an <see cref="AccessibilityNode"/> to screen readers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// You can use the values in this enumeration to set the <see cref="AccessibilityNode.role"/>. This property
    /// assigns a semantic meaning to the accessibility node, which informs screen readers how to interact with it and
    /// announce its purpose appropriately.
    /// </para>
    /// <para>
    /// Setting accurate roles improves the usability and user experience of your user interface by enabling the screen
    /// reader to set clear expectations for the user. A missing role can block the user from interacting with your
    /// application, and an incorrect role can cause confusion and frustration.
    /// </para>
    /// <para>
    /// If the visual element that the node represents has a role that is not covered by any of the enumeration options,
    /// use the default value, <see cref="AccessibilityRole.None"/>, and provide information about the node's purpose
    /// and behavior in properties such as the <see cref="AccessibilityNode.label"/> and
    /// <see cref="AccessibilityNode.hint"/>.
    /// </para>
    /// <para>
    /// SA:
    ///
    ///- [[wiki:accessibility|Accessibility for mobile applications]]
    ///- &lt;a href="https://github.com/Unity-Technologies/a11y-public-sample" &gt;Sample project using the accessibility APIs&lt;/a&gt;
    /// </para>
    /// </remarks>
    /// <example>
    /// The following example demonstrates assigning an [[AccessibilityRole]] to a UI element based on its
    /// [[UIElements.VisualElement]] type.
    /// <code source="../Tests/AccessibilityExamples/Assets/Examples/AccessibilityManager.cs"/>
    /// </example>
    [NativeHeader("Modules/Accessibility/Native/AccessibilityNodeData.h")]
    public enum AccessibilityRole : byte
    {
        /// <summary>
        /// The accessibility node has none of the predefined roles.
        /// </summary>
        /// <remarks>
        /// Use this role if the visual element that the node represents does not fit any of the predefined roles in
        /// this enumeration. Provide information about the node's purpose and behavior in properties such as the
        /// <see cref="AccessibilityNode.label"/> and <see cref="AccessibilityNode.hint"/>.
        /// </remarks>
        None,

        /// <summary>
        /// The accessibility node behaves like a button.
        /// </summary>
        /// <remarks>
        /// If this role is set on a node, the screen reader announces the node as a "button". On Android and macOS,
        /// after a short pause, it also provides instructions on how to activate the node.
        ///\\
        ///\\
        /// Subscribe to the <see cref="AccessibilityNode.invoked"/> event to inform the screen reader that the node can
        /// be activated, and perform an appropriate action when the user activates it, such as invoking the button
        /// represented by the node.
        /// </remarks>
        Button,

        /// <summary>
        /// The accessibility node behaves like an image.
        /// </summary>
        /// <remarks>
        /// If this role is set on a node, the screen reader announces the node as an "image".
        ///\\
        ///\\
        /// On Windows, if the node represents a clickable image, then it should have a role that supports
        /// <see cref="AccessibilityNode.invoked"/>, such as <see cref="AccessibilityRole.Button"/>, instead of this
        /// role.
        /// </remarks>
        Image,

        /// <summary>
        /// The accessibility node behaves like static text that can't change.
        /// </summary>
        /// <remarks>
        /// On macOS, if this role is set on a node that has a <see cref="AccessibilityNode.label"/> assigned, the
        /// screen reader announces the node as a "text". On other platforms, this role does not affect the node's
        /// announcement but provides the screen reader with semantic information about the node.
        ///\\
        ///\\
        /// On Windows and macOS, if this role is set on a node without a label, the screen reader ignores the node.
        /// </remarks>
        StaticText,

        /// <summary>
        /// The accessibility node behaves like a search field.
        /// </summary>
        /// <remarks>
        /// **Platform-specific behavior**
        ///\\
        ///- **Android**: This role does not affect the node's announcement but provides the screen reader with semantic information about the node.
        ///- **iOS**: If this role is set on a node, the screen reader announces the node as a "search field".
        ///- **macOS**: If this role is set on a node, the screen reader announces the node as a "search text field".
        ///- **Windows**: If this role is set on a node, the screen reader announces the node as "edit". The resulting behavior of this role is identical to that of <see cref="AccessibilityRole.TextField"/>.
        ///\\
        /// On Windows and macOS, subscribe to the <see cref="AccessibilityNode.focusChanged"/> event to select the
        /// search field represented by the node when the user navigates to it, so that it can receive keyboard input.
        /// </remarks>
        SearchField,

        /// <summary>
        /// The accessibility node behaves like a keyboard key.
        /// </summary>
        /// <remarks>
        /// On mobile platforms, this role enables touch typing, which allows the user to activate the node by
        /// performing a single-tap gesture instead of the standard double-tap gesture.
        ///\\
        ///\\
        /// Subscribe to the <see cref="AccessibilityNode.invoked"/> event to inform the screen reader that the node can
        /// be activated, and perform an appropriate action when the user activates it, such as invoking the key
        /// represented by the node.
        ///\\
        ///\\
        /// **Platform support**
        ///\\
        ///- On Android, this role is only supported starting with Android 10 (API level 29).
        ///- This role has no effect on desktop platforms.
        ///\\
        /// SA:
        ///\\
        ///- &lt;a href="https://support.google.com/accessibility/android/answer/6006598?hl=en#:~:text=Edit%20text%20with%20Gboard" &gt;Touch typing on Android&lt;/a&gt;
        ///- &lt;a href="https://support.apple.com/en-us/guide/iphone/iph3e2e3d1d/ios#:~:text=Touch%20typing" &gt;Touch typing on iOS&lt;/a&gt;
        /// </remarks>
        KeyboardKey,

        /// <summary>
        /// The accessibility node behaves like a heading that divides content into sections, such as the title of a
        /// navigation bar.
        /// </summary>
        /// <remarks>
        /// On Windows, if this role is set on a node without a <see cref="AccessibilityNode.label"/>, the screen reader
        /// announces the node as a "header". On other platforms, if this role is set on a node, the screen reader
        /// announces the node as a "heading" regardless of whether the node has a label set.
        ///\\
        ///\\
        /// On mobile platforms, this role enables heading navigation, which allows users to more efficiently navigate
        /// an application by moving from one heading to the next without having to navigate through all the nodes in
        /// between. On Android, this navigation mode can be activated through the "Headings"
        /// &lt;a href="https://support.google.com/accessibility/android/answer/6006598?hl=en#:~:text=Choose%20reading%20controls" &gt;reading control&lt;/a&gt;
        /// in TalkBack. On iOS, it can be accessed through the "Headings" control in the
        /// &lt;a href="https://support.apple.com/en-us/111796" &gt;VoiceOver rotor&lt;/a&gt;.
        ///\\
        ///\\
        /// On macOS, accessibility nodes with this role may be listed in the "Window Spots" menu of the
        /// &lt;a href="https://support.apple.com/en-us/guide/voiceover/mchlp2719/mac" &gt;VoiceOver rotor&lt;/a&gt;.
        ///\\
        ///\\
        /// **Platform support**: On Android, this role is only supported starting with Android 9 (API level 28).
        /// </remarks>
        Header,

        /// <summary>
        /// The accessibility node behaves like an ordered list of tabs.
        /// </summary>
        /// <remarks>
        /// Tab bar nodes are specialized container nodes. See <see cref="AccessibilityRole.Container"/> for
        /// platform-specific screen reader behavior that is common to both container and tab bar nodes.
        ///\\
        ///\\
        /// **Platform behavior specific to this role**
        ///\\
        ///- **Android**: This role does not affect the node's announcement but provides the screen reader with semantic information about the node.
        ///- **iOS**: If this role is set on a node, the screen reader announces the node's children as "tabs". It also announces a tab's position in the tab bar and the total number of tabs in it.
        ///- **macOS**: If this role is set on a node, the screen reader announces the node as a "tab group". If the node's children have <see cref="AccessibilityRole.TabButton"/> set, the screen reader also announces their position in the tab bar and the total number of tabs in it.
        ///- **Windows**: If this role is set on a node, the screen reader announces the node as a "tab".
        ///\\
        /// For proper functionality, tab bar nodes must have a <see cref="AccessibilityNode.label"/> set, and their
        /// child nodes must have <see cref="AccessibilityRole.TabButton"/> assigned.
        ///\\
        ///\\
        /// Set <see cref="AccessibilityState.Selected"/> on the child node representing the selected tab to inform the
        /// screen reader of which tab is currently selected.
        /// </remarks>
        TabBar,

        /// <summary>
        /// The accessibility node behaves like a slider that allows continuous adjustment through a range of values.
        /// </summary>
        /// <remarks>
        /// **Platform-specific behavior**
        ///\\
        ///- **Android**: If this role is set on a node, the screen reader announces the node as a "slider". After a short pause, it provides instructions on how to adjust its value.
        ///- **iOS**: If this role is set on a node, the screen reader announces the node as "adjustable". After a short pause, it provides instructions on how to adjust its value.
        ///- **macOS**: If this role is set on a node, the screen reader announces the node as a "slider". After a short pause, it provides instructions on how to interact with it.
        ///- **Windows**: If this role is set on a node, the screen reader announces the node as a "slider".
        ///\\
        /// Subscribe to the <see cref="AccessibilityNode.incremented"/> and
        /// <see cref="AccessibilityNode.decremented"/> events to perform an appropriate action when the user increases
        /// or decreases the node's value, such as changing the value of the slider represented by the node. On Windows,
        /// these events are only triggered for nodes whose <see cref="AccessibilityNode.value"/> contains a number.
        /// \\
        /// On Windows, subscribe to the <see cref="AccessibilityNode.focusChanged"/> event to select the slider
        /// represented by the node when the user navigates to it, so that it can receive keyboard input.
        /// </remarks>
        Slider,

        /// <summary>
        /// The accessibility node behaves like a toggle.
        /// </summary>
        /// <remarks>
        /// **Platform-specific behavior**
        ///\\
        ///- **Android**: If this role is set on a node, the screen reader announces the node as a "switch". After a short pause, it provides instructions on how to toggle it. If the node has <see cref="AccessibilityState.Selected"/> set, the screen reader reads "on" before announcing the node's label. Otherwise, it reads "off".
        ///- **iOS**: If this role is set on a node, the screen reader announces the node as a "switch button". After a short pause, it provides instructions on how to toggle it. If the node has <see cref="AccessibilityState.Selected"/> set, the screen reader reads "selected" before announcing the node's label. Otherwise, it does not read the node's state.
        ///- **macOS**: If this role is set on a node, the screen reader announces the node as a "checkbox". After a short pause, it provides instructions on how to toggle it. If the node has <see cref="AccessibilityState.Selected"/> set, the screen reader reads "checked" after announcing the node's label. Otherwise, it reads "unchecked".
        ///- **Windows**: If this role is set on a node, the screen reader announces the node as a "checkbox". If the node has <see cref="AccessibilityState.Selected"/> set, the screen reader reads "checked" after announcing the node's label. Otherwise, it reads "unchecked".
        ///\\
        /// Subscribe to the <see cref="AccessibilityNode.invoked"/> event to inform the screen reader that the node can
        /// be activated, and perform an appropriate action when the user activates it, such as changing the value of
        /// the toggle represented by the node.
        ///\\
        ///\\
        /// **Platform support**: On iOS, this role is only supported starting with iOS 17.
        /// </remarks>
        Toggle,

        /// <summary>
        /// The accessibility node is a container of other nodes (examples of containers include tab bars and scroll
        /// views).
        /// </summary>
        /// <remarks>
        /// Apply this role to parent <see cref="AccessibilityNode"/> elements to enable smoother and more efficient
        /// screen reader navigation.
        ///\\
        ///\\
        /// **When to use**
        ///\\
        ///\\
        /// You can use this role to organize complex interfaces. For example, you can represent a tab bar as a
        /// container node with multiple tabs as child nodes. This allows users to navigate between tabs without having
        /// to navigate through all the other elements on the screen.
        ///\\
        ///\\
        /// This role is especially useful in:
        ///\\
        ///\\
        ///- Tab groups or sections of a user interface that need distinct boundaries.
        ///- Navigation bars or toolbars that contain buttons or other controls.
        ///- Popups, dialogs, or other temporary views.
        ///- Forms or panels that contain related input fields or controls.
        ///- Scroll views or other containers that contain a large amount of content.
        ///\\
        /// **Android behavior**
        ///\\
        ///\\
        /// Container nodes themselves are not directly focusable, but they do provide the screen reader with key
        /// information that enhances navigation:
        ///\\
        ///\\
        ///- They enable container navigation, which can be activated through the "Containers" &lt;a href="https://support.google.com/accessibility/android/answer/6006598?hl=en#:~:text=Choose%20reading%20controls" &gt;reading control&lt;/a&gt; in TalkBack. In this navigation mode, users can move from one container to the next without having to navigate through all the nodes in between.
        ///- Starting with Android 14 (API level 34), the screen reader may announce when the user enters or exits a container.
        ///\\
        /// To ensure proper announcements, container nodes must have a <see cref="AccessibilityNode.label"/> set.
        ///\\
        ///\\
        /// Setting <see cref="AccessibilityNode.isActive"/> on a container node has no effect.
        ///\\
        ///\\
        /// **iOS behavior**
        ///\\
        ///\\
        /// Container nodes are not directly focusable during standard screen reader navigation (called flat navigation
        /// on iOS), but they provide essential context for the screen reader:
        ///\\
        ///\\
        ///- They enable container navigation, which can be activated through the "Containers" control in the &lt;a href="https://support.apple.com/en-us/111796" &gt;VoiceOver rotor&lt;/a&gt;. As on Android, this navigation mode allows users to navigate efficiently between containers.
        ///- They enable &lt;a href="https://support.apple.com/en-us/guide/iphone/iphfa3d32c50/ios#:~:text=Use%20flat%20or%20grouped%20navigation" &gt;grouped navigation&lt;/a&gt;, which can be accessed through the "Navigation Style" control in the VoiceOver rotor. In grouped navigation, container nodes are focusable. When navigating sequentially, the screen reader focuses on the container node directly instead of focusing on its child nodes. To navigate through the container's child nodes, the user must move into the container by performing a dedicated gesture. Once in a container, the user must first move out of it to navigate to nodes outside of the container. This navigation style is particularly useful in complex interfaces, where it simplifies and speeds up navigation.
        ///- In flat navigation, the screen reader announces the container node's label when the user enters the container by focusing on any of its child nodes. In grouped navigation, the screen reader announces the container node both when entering and when exiting it.
        ///\\
        /// Container nodes must have a <see cref="AccessibilityNode.label"/> set to ensure proper functionality in all
        /// of the cases presented above.
        ///\\
        ///\\
        /// Setting <see cref="AccessibilityNode.isActive"/> on a container node has no effect.
        ///\\
        ///\\
        /// **macOS behavior**
        ///\\
        ///\\
        /// As opposed to Android and iOS, container nodes are focusable in standard screen reader navigation on macOS.
        ///\\
        ///\\
        /// As in the grouped navigation style on iOS, the screen reader focuses on the container node directly instead
        /// of focusing on its child nodes. To access the container's child nodes, the user must move into the container
        /// by using a dedicated VoiceOver command. Once in a container, the user must first move out of it to navigate
        /// to sibling nodes or other parts of the hierarchy. This navigation style is useful for the user because it
        /// streamlines navigation in complex interfaces specific to desktop applications.
        ///\\
        ///\\
        /// This navigation style is available for any parent nodes, even if they do not have this role set. However,
        /// setting this role allows the screen reader to announce the node as a "group" and provide instructions on how
        /// to interact with its child nodes.
        ///\\
        ///\\
        /// The screen reader announces when the user enters or exits any kind of parent node. When moving into it, the
        /// screen reader also announces the number of its child nodes.
        ///\\
        ///\\
        /// Container nodes may be listed in the "Window Spots" menu of the
        /// &lt;a href="https://support.apple.com/en-us/guide/voiceover/mchlp2719/mac" &gt;VoiceOver rotor&lt;/a&gt;.
        ///\\
        ///\\
        /// Setting other roles on a parent node may result in unintended screen reader behavior.
        ///\\
        ///\\
        /// For proper functionality, container nodes must have a <see cref="AccessibilityNode.label"/> set.
        ///\\
        ///\\
        /// Unlike on Android and iOS, setting <see cref="AccessibilityNode.isActive"/> to @@false@@ on a container node
        /// deactivates both the node itself and its child nodes.
        ///\\
        ///\\
        /// **Windows behavior**
        ///\\
        ///\\
        /// Container nodes are not directly focusable, but they do provide the screen reader with key information that
        /// enhances navigation.
        ///\\
        ///\\
        /// If a container node has <see cref="AccessibilityNode.isActive"/> set to @@true@@ and has a
        /// <see cref="AccessibilityNode.label"/> set, the screen reader announces its label and role (as a "group")
        /// when the user enters the container by focusing on any of its child nodes.
        ///\\
        ///\\
        /// Setting <see cref="AccessibilityNode.isActive"/> on a container node has no effect on its child nodes.
        /// </remarks>
        Container,

        /// <summary>
        /// The accessibility node behaves like a text field.
        /// </summary>
        /// <remarks>
        /// **Platform-specific behavior**
        ///\\
        ///- **Android**: If this role is set on a node, the screen reader announces the node as an "edit box".
        ///- **iOS**: This role has no effect.
        ///- **macOS**: If this role is set on a node, the screen reader announces the node as a "text".
        ///- **Windows**: If this role is set on a node, the screen reader announces the node as "edit".
        ///\\
        /// On Windows and macOS, subscribe to the <see cref="AccessibilityNode.focusChanged"/> event to select the text
        /// field represented by the node when the user navigates to it, so that it can receive keyboard input.
        /// </remarks>
        TextField,

        /// <summary>
        /// The accessibility node behaves like a dropdown list.
        /// </summary>
        /// <remarks>
        /// **Platform-specific behavior**
        ///\\
        ///\\
        ///- **Android**: If this role is set on a node, the screen reader announces the node as a "dropdown list". After a short pause, it provides instructions on how to open it.
        ///- **iOS**: This role has no effect.
        ///- **macOS**: If this role is set on a node, the screen reader announces the node as a "pop up button". After a short pause, it provides instructions on how to open it. If the node has <see cref="AccessibilityState.Expanded"/> set, the screen reader reads "expanded" after announcing the node's label. Otherwise, it reads "collapsed".
        ///- **Windows**: If this role is set on a node, the screen reader announces the node as a "combo box". If the node has <see cref="AccessibilityState.Expanded"/> set, the screen reader reads "expanded" after announcing the node's label. Otherwise, it reads "collapsed".
        ///\\
        /// Subscribe to the <see cref="AccessibilityNode.invoked"/> event to inform the screen reader that the node can
        /// be activated, and perform an appropriate action when the user activates it, such as opening the dropdown
        /// represented by the node.
        ///\\
        ///\\
        /// For proper functionality, a dropdown's child nodes must have <see cref="AccessibilityRole.Button"/> or
        /// <see cref="AccessibilityRole.Toggle"/> assigned.
        ///\\
        ///\\
        /// Set <see cref="AccessibilityState.Selected"/> on the child node representing the selected option to inform
        /// the screen reader of which option is currently selected.
        /// </remarks>
        Dropdown,

        /// <summary>
        /// The accessibility node behaves like an item in an ordered list of tabs.
        /// </summary>
        /// <remarks>
        /// **Platform-specific behavior**
        ///\\
        ///\\
        ///- **Android**: If this role is set on a node, the screen reader announces the node as a "button". After a short pause, it provides instructions on how to activate the node.
        ///- **iOS**: If this role is set on a node and the node's parent has <see cref="AccessibilityRole.TabBar"/> set, the screen reader announces the node as a "tab". Otherwise, it announces the node as a "button".
        ///- **macOS**: If this role is set on a node, the screen reader announces the node as a "tab". After a short pause, it provides instructions on how to select the node. If the node's parent has <see cref="AccessibilityRole.TabBar"/> set, the screen reader also announces the tab's position in the tab bar and the total number of tabs in it.
        ///- **Windows**: If this role is set on a node, the screen reader announces the node as a "tab item".
        ///\\
        /// For proper functionality, the parent node of tab button nodes must have
        /// <see cref="AccessibilityRole.TabBar"/> assigned.
        ///\\
        ///\\
        /// If the tab represented by the tab button node is selected, assign <see cref="AccessibilityState.Selected"/>
        /// to the node to inform the screen reader of which tab is currently selected.
        ///\\
        ///\\
        /// Subscribe to the <see cref="AccessibilityNode.invoked"/> event to inform the screen reader that the node can
        /// be activated, and perform an appropriate action when the user activates it, such as selecting the tab
        /// represented by the node.
        /// </remarks>
        TabButton,

        /// <summary>
        /// The accessibility node behaves like a scrollable container.
        /// </summary>
        /// <remarks>
        /// Scroll view nodes are specialized container nodes. Refer to <see cref="AccessibilityRole.Container"/> for
        /// platform-specific screen reader behavior that is common to both container and scroll view nodes.
        ///\\
        ///\\
        /// **Platform behavior specific to this role**
        ///\\
        ///\\
        ///- **Android**: This role does not affect the node's announcement but provides the screen reader with semantic information about the node.
        ///- **iOS**: This role does not have any additional effect compared to <see cref="AccessibilityRole.Container"/>.
        ///- **macOS**: If this role is set on a node, the screen reader announces the node as a "scroll area".
        ///- **Windows**: This role enables screen reader scrolling &lt;a href="https://support.microsoft.com/en-us/windows/chapter-6-using-narrator-with-touch-60f8f38b-23fa-ebe2-4345-c900d1b2e22f" &gt;gestures on Windows touch-screen devices&lt;/a&gt;. It does not affect the node's announcement.
        ///
        /// For proper functionality, a scroll view's active child nodes must be subscribed to the
        /// <see cref="AccessibilityNode.scrolled"/> event.
        /// </remarks>
        ScrollView,
    }

    /// <summary>
    /// Options for defining the state of an <see cref="AccessibilityNode"/> to screen readers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// You can use the values in this enumeration to set the <see cref="AccessibilityNode.state"/>. This property
    /// enables screen readers to describe the state of interactive elements, such as whether a checkbox is checked or
    /// whether a button is disabled.
    /// </para>
    /// <para>
    /// If the visual element that the node represents has a state that is not represented by an enumeration option, use
    /// the default value, <see cref="AccessibilityState.None"/>, and provide information about the node's current state
    /// in properties such as the <see cref="AccessibilityNode.value"/> and <see cref="AccessibilityNode.hint"/>.
    /// </para>
    /// <para>
    /// SA:
    ///
    ///- [[wiki:accessibility|Accessibility for mobile applications]]
    ///- &lt;a href="https://github.com/Unity-Technologies/a11y-public-sample" &gt;Sample project using the accessibility APIs&lt;/a&gt;
    /// </para>
    /// </remarks>
    [NativeHeader("Modules/Accessibility/Native/AccessibilityNodeData.h")]
    [Flags]
    public enum AccessibilityState : byte
    {
        /// <summary>
        /// The accessibility node is not in a predefined state.
        /// </summary>
        /// <remarks>
        /// Use this state if the visual element that the node represents does not have any of the predefined states in
        /// this enumeration. Provide information about the node's current state (for example, whether a collapsible
        /// section is expanded or collapsed) in properties such as the <see cref="AccessibilityNode.value"/> and
        /// <see cref="AccessibilityNode.hint"/>.
        /// </remarks>
        None = 0,

        /// <summary>
        /// The visual element represented by the accessibility node is currently disabled and does not respond to user
        /// interaction.
        /// </summary>
        /// <remarks>
        /// This state is purely informative. It indicates the status of the visual element that the node represents.
        /// Setting this state does not prevent the user from interacting with the node (for example, the
        /// <see cref="AccessibilityNode.invoked"/> event is still triggered if the user wants to activate the node).
        /// To prevent user interaction with the visual element represented by the node, make sure it is disabled and
        /// restrict its actions in your accessibility event handlers (for example, return @@false@@ in your event
        /// handler for <see cref="AccessibilityNode.invoked"/>).
        ///\\
        ///\\
        /// **Platform-specific behavior**
        ///\\
        ///- **Android**: If this role is set on a node, the screen reader announces the node as "disabled".
        ///- **iOS**: If this role is set on a node, the screen reader announces the node as "dimmed".
        ///- **macOS**: If this role is set on a node, the screen reader announces the node as "dimmed".
        ///- **Windows**: If this role is set on a node, the screen reader announces the node as "disabled".
        /// </remarks>
        Disabled = 1 << 0,

        /// <summary>
        /// The visual element represented by the accessibility node is currently selected, such as a selected table row
        /// or a selected button in a segmented control.
        /// </summary>
        /// <remarks>
        /// **Platform-specific behavior**
        ///\\
        ///\\
        /// For nodes with a role other than <see cref="AccessibilityRole.Toggle"/>:
        ///\\
        ///- **Android**: If this role is set on a node, the screen reader announces the node as "selected".
        ///- **iOS**: If this role is set on a node, the screen reader announces the node as "selected".
        ///- **macOS**: If this role is set on a node, the screen reader announces the node as "selected".
        ///- **Windows**: If this role is set on a node, the screen reader announces the node as "on".
        ///\\
        /// For nodes with the role <see cref="AccessibilityRole.Toggle"/>, see that role's documentation.
        ///\\
        ///\\
        /// **Notes**
        ///
        ///- On macOS, this state is only supported for nodes with the role <see cref="AccessibilityRole.Button"/>, <see cref="AccessibilityRole.Toggle"/> or <see cref="AccessibilityRole.Dropdown"/>.
        ///- On Windows, this state is only supported for nodes with the role <see cref="AccessibilityRole.Button"/> or <see cref="AccessibilityRole.Toggle"/>
        /// </remarks>
        Selected = 1 << 1,

        /// <summary>
        /// The visual element represented by the accessibility node is currently expanded, such as an expanded dropdown
        /// list or menu.
        /// </summary>
        /// <remarks>
        /// If this role is set on a node with the role <see cref="AccessibilityRole.Dropdown"/>, the screen reader
        /// announces the node as "expanded". Otherwise, it announces the node as "collapsed".
        ///\\
        ///\\
        /// **Notes**
        ///\\
        ///\\
        ///- This state is only supported for nodes with the role <see cref="AccessibilityRole.Dropdown"/>.
        ///- On macOS, if the role <see cref="AccessibilityRole.Dropdown"/> is unset from a node, the screen reader continues to announce the expanded/collapsed state of the node if its new role is compatible with this state. This is a platform limitation.
        ///\\
        /// **Platform support**: This state has no effect on mobile platforms.
        /// </remarks>
        Expanded = 1 << 2,
    }

    /// <summary>
    /// Describes the direction of a scrolling action.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The values in this enumeration are used by the <see cref="AccessibilityNode.scrolled"/> event, which is sent
    /// when a screen reader user performs a scrolling action.
    /// </para>
    /// <para>
    /// SA:
    ///
    ///- [[wiki:accessibility|Accessibility for mobile applications]]
    ///- &lt;a href="https://github.com/Unity-Technologies/a11y-public-sample" &gt;Sample project using the accessibility APIs&lt;/a&gt;
    /// </para>
    /// </remarks>
    [NativeHeader("Modules/Accessibility/Native/AccessibilityNodeData.h")]
    public enum AccessibilityScrollDirection : byte
    {
        /// <summary>
        /// The direction of the scrolling action is unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// The user is scrolling forward.
        /// </summary>
        /// <remarks>
        /// **Platform support**: The forward scroll direction isn't supported on Windows.
        /// </remarks>
        Forward,

        /// <summary>
        /// The user is scrolling backward.
        /// </summary>
        /// <remarks>
        /// **Platform support**: The backward scroll direction is not supported on Windows.
        /// </remarks>
        Backward,

        /// <summary>
        /// The user is scrolling from right to left.
        /// </summary>
        /// <remarks>
        /// **Platform support**: The right-to-left scroll direction is not supported on Android.
        /// </remarks>
        Left,

        /// <summary>
        /// The user is scrolling from left to right.
        /// </summary>
        /// <remarks>
        /// **Platform support**: The left-to-right scroll direction is not supported on Android.
        /// </remarks>
        Right,

        /// <summary>
        /// The user is scrolling from bottom to top.
        /// </summary>
        /// <remarks>
        /// **Platform support**: The bottom-to-top scroll direction is not supported on Android.
        /// </remarks>
        Up,

        /// <summary>
        /// The user is scrolling from top to bottom.
        /// </summary>
        /// <remarks>
        /// **Platform support**: The top-to-bottom scroll direction is not supported on Android.
        /// </remarks>
        Down,
    }

    /// <summary>
    /// The data stored in an accessibility node.
    /// </summary>
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/Accessibility/Native/AccessibilityNodeData.h")]
    internal struct AccessibilityNodeData
    {
        public AccessibilityNodeData()
        {
            nodeId = AccessibilityNodeManager.k_InvalidNodeId;
            parentId = AccessibilityNodeManager.k_InvalidNodeId;
            childIds = Array.Empty<int>();

            isActive = true;

            frame = default;

            label = null;
            value = null;
            hint = null;

            role = AccessibilityRole.None;
            state = AccessibilityState.None;

            allowsDirectInteraction = false;

            implementsInvoked = false;
            implementsScrolled = false;
            implementsDismissed = false;
        }

        /// <summary>
        /// The IDs of the nodes contained by the accessibility node.
        /// </summary>
        public int[] childIds { get; set; }

        /// <summary>
        /// A succinct description of the accessibility node.
        /// </summary>
        public string label { get; set; }

        /// <summary>
        /// The current value of the accessibility node.
        /// </summary>
        public string value { get; set; }

        /// <summary>
        /// Additional information about the accessibility node. For example, the result of performing an action on the
        /// node.
        /// </summary>
        public string hint { get; set; }

        /// <summary>
        /// The frame of the accessibility node in screen coordinates.
        /// </summary>
        public Rect frame { get; set; }

        /// <summary>
        /// The ID of the accessibility node.
        /// </summary>
        public int nodeId { get; set; }

        /// <summary>
        /// The ID of the node that contains the accessibility node.
        /// </summary>
        public int parentId { get; set; }

        /// <summary>
        /// The role of the accessibility node.
        /// </summary>
        public AccessibilityRole role { get; set; }

        /// <summary>
        /// The state of the accessibility node.
        /// </summary>
        public AccessibilityState state { get; set; }

        /// <summary>
        /// Whether the node fires accessibility events and can be accessed by assistive technology.
        /// </summary>
        public bool isActive { get; set; }

        /// <summary>
        /// Whether the accessibility node allows direct touch interaction.
        /// </summary>
        /// <remarks>
        /// This is only supported on iOS.
        /// </remarks>
        public bool allowsDirectInteraction { get; set; }

        /// <summary>
        /// Whether the accessibility node's <see cref="AccessibilityNode.invoked"/> event has subscribers.
        /// </summary>
        /// <remarks>
        /// This is needed to enable the <see cref="AccessibilityNode.invoked"/> event on Android and Windows.
        /// </remarks>
        public bool implementsInvoked { get; set; }

        /// <summary>
        /// Whether the accessibility node's <see cref="AccessibilityNode.scrolled"/> event has subscribers.
        /// </summary>
        /// <remarks>
        /// This is needed to enable the <see cref="AccessibilityNode.scrolled"/> event on Android and Windows.
        /// </remarks>
        public bool implementsScrolled { get; set; }

        /// <summary>
        /// Whether the accessibility node's <see cref="AccessibilityNode.dismissed"/> event has subscribers.
        /// </summary>
        /// <remarks>
        /// This is needed to enable the <see cref="AccessibilityNode.dismissed"/> event on Android.
        /// </remarks>
        public bool implementsDismissed { get; set; }
    }
}
