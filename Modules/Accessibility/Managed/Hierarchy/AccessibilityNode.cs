// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.Accessibility
{
    /// <summary>
    /// A node in the <see cref="AccessibilityHierarchy"/> representing a visual element, such as a UI element or an
    /// element that is part of your game, that needs to be accessible to the screen reader.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Accessibility nodes are data structures that enable screen readers to focus, announce and execute user actions
    /// on them. They represent visual elements in the application, but they exist and function independently of their
    /// corresponding visual elements. Changes to the visual representation of an element, such as its visibility,
    /// layering order or screen coordinates, do not affect the accessibility node that represents it.
    /// </para>
    /// <para>
    /// To create an accessibility node, call <see cref="AccessibilityHierarchy.AddNode"/> on the hierarchy you want to
    /// add the node to. When calling this method, you can optionally specify the node's label and parent in the
    /// hierarchy. This method returns the created node. Use it to set different attributes and define the node's
    /// identity and functionality.
    /// </para>
    /// <para>
    /// When a screen reader is active, users can navigate and interact with an application using specific gestures or
    /// commands that are easier to perform and prevent accidental actions. For this purpose, on mobile platforms,
    /// standard gestures, such as tap or swipe, do not work, or they perform different actions while the screen reader
    /// is on. Screen reader gestures and commands can vary across platforms, but they trigger the same accessibility
    /// events. For examples of gestures and commands on different platforms, refer to the **Events** section on this
    /// page. Subscribe to these events to detect user actions and to respond accordingly.
    /// </para>
    /// <para>
    /// These APIs are currently supported on the following platforms:
    ///
    ///- <see cref="RuntimePlatform.Android"/> - starting with Android 8.0 (API level 26)
    ///- <see cref="RuntimePlatform.IPhonePlayer"/>
    ///- <see cref="RuntimePlatform.OSXPlayer"/>
    ///- <see cref="RuntimePlatform.WindowsPlayer"/>
    ///
    /// SA:
    ///
    ///- [[wiki:accessibility|Accessibility for mobile applications]]
    ///- &lt;a href="https://github.com/Unity-Technologies/a11y-public-sample" &gt;Sample project using the accessibility APIs&lt;/a&gt;
    ///- &lt;a href="https://support.google.com/accessibility/android/answer/6151827" &gt;TalkBack gestures on Android&lt;/a&gt;
    ///- &lt;a href="https://support.apple.com/en-us/guide/iphone/iph3e2e2281/ios" &gt;VoiceOver gestures on iPhone&lt;/a&gt;
    ///- &lt;a href="https://support.microsoft.com/en-us/windows/chapter-2-narrator-basics-5ff4591e-7b6d-245e-c95d-ce83c0a1a8d4" &gt;Narrator commands on Windows&lt;/a&gt;
    ///- &lt;a href="https://support.apple.com/en-us/guide/voiceover/vo14111/mac" &gt;VoiceOver commands on Mac&lt;/a&gt;
    /// </para>
    /// </remarks>
    public partial class AccessibilityNode
    {
        /// <summary>
        /// Event invoked on the main thread when the accessibility node gains or loses screen reader focus.
        /// </summary>
        /// <remarks>
        /// Subscribe to this event if you need to know when the node gains or loses screen reader focus. For example,
        /// to select a text field when the user navigates to it, so that it can receive keyboard input.
        /// </remarks>
        public event Action<AccessibilityNode, bool> focusChanged;

        /// <summary>
        /// Event invoked when the user performs an "activate" action when focused on the accessibility node.
        /// </summary>
        /// <returns>@@true@@ if the action succeeds and @@false@@ otherwise.</returns>
        /// <remarks>
        /// <para>
        /// Subscribe to this event to inform the screen reader that the node can be activated and to respond
        /// appropriately when the user performs this action. For example, activating a button, toggling a checkbox, or
        /// opening a dropdown.
        /// </para>
        /// <para>
        /// Your callback should activate the visual element represented by the node and perform any other appropriate
        /// tasks. For example, you might use this event to activate a control that requires a gesture which would be
        /// difficult for screen reader users to perform, or has a different meaning when the screen reader is on.
        /// </para>
        /// <para>
        /// After performing any tasks, return an appropriate value to indicate success or failure.
        /// </para>
        /// <para>
        /// On mobile platforms, the "activate" action always sends a tap gesture in the center of the node's
        /// <see cref="AccessibilityNode.frame"/>, making the use of this event optional. However, if the visual element
        /// that the node represents does not intersect the center of the node's frame (for example, the node's frame
        /// covers a toggle as well as its label), it does not receive the tap gesture. Subscribing to this event allows
        /// you to activate the element regardless of its position relative to the node's frame. **Note**: Subscribing
        /// to this event does not prevent the tap gesture from being sent, so make sure the node's frame does not
        /// overlap with other interactive elements.
        /// </para>
        /// <para>
        /// On Android, subscribing to this event also prompts the screen reader to provide instructions on how to
        /// activate the node.
        /// </para>
        /// <para>
        /// On Windows and macOS, this event is required for screen reader users to be able to activate the node.
        /// </para>
        /// <para>
        /// **Note**: On Windows, this event is not triggered for nodes with the role
        /// <see cref="AccessibilityRole.Image"/>.
        /// </para>
        /// </remarks>
        public event Func<bool> invoked;

        /// <summary>
        /// Event invoked when the user performs an "increment" action when focused on the accessibility node.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Subscribe to this event if the node has the role <see cref="AccessibilityRole.Slider"/>.
        /// </para>
        /// <para>
        /// Your callback should increment the value of the visual element represented by the node as well as the node's
        /// <see cref="AccessibilityNode.value"/> by an appropriate amount.
        /// </para>
        /// <para>
        /// On iOS and macOS, if your callback does not change the node's value (which might happen, for example, if the
        /// slider represented by the node is already at its maximum value), the screen reader indicates to the user
        /// that value adjustment cannot continue due to a border or boundary.
        /// </para>
        /// <para>
        /// **Notes**
        ///
        ///- On macOS, this event is only triggered for nodes with the role <see cref="AccessibilityRole.Slider"/>.
        ///- On Windows, this event is only triggered for nodes with the role <see cref="AccessibilityRole.Slider"/> whose <see cref="AccessibilityNode.value"/> contains a number.
        /// </para>
        /// </remarks>
        public event Action incremented;

        /// <summary>
        /// Event invoked when the user performs a "decrement" action when focused on the accessibility node.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Subscribe to this event if the node has the role <see cref="AccessibilityRole.Slider"/>.
        /// </para>
        /// <para>
        /// Your callback should decrement the value of the visual element represented by the node as well as the node's
        /// <see cref="AccessibilityNode.value"/> by an appropriate amount.
        /// </para>
        /// <para>
        /// On iOS and macOS, if your callback does not change the node's value (which might happen, for example, if the
        /// slider represented by the node is already at its minimum value), the screen reader indicates to the user
        /// that value adjustment cannot continue due to a border or boundary.
        /// </para>
        /// <para>
        /// **Notes**
        ///
        ///- On macOS, this event is only triggered for nodes with the role <see cref="AccessibilityRole.Slider"/>.
        ///- On Windows, this event is only triggered for nodes with the role <see cref="AccessibilityRole.Slider"/> whose <see cref="AccessibilityNode.value"/> contains a number.
        /// </para>
        /// </remarks>
        public event Action decremented;

        /// <summary>
        /// Event invoked when the user performs a "scroll" action when focused on the accessibility node.
        /// </summary>
        /// <returns>@@true@@ if the action succeeds and @@false@@ otherwise.</returns>
        /// <remarks>
        /// <para>
        /// Subscribe to this event to support scrolling in an application-specific way, such as a scroll by page
        /// action. This is not the same as standard scrolling, which is supported by default and does not trigger an
        /// accessibility event.
        /// </para>
        /// <para>
        /// Your callback should scroll the content of the scroll view containing the visual element represented by the
        /// node by an appropriate amount based on the direction provided. For example, if the scrolling direction is
        /// <see cref="AccessibilityScrollDirection.Forward"/>, scroll the content up or to the left (depending on the
        /// scroll view's orientation) by one page.
        /// </para>
        /// <para>
        /// If the scrolling succeeds for the specified direction, return @@true@@ and call
        /// <see cref="IAccessibilityNotificationDispatcher.SendPageScrolledAnnouncement"/> to provide the user with
        /// information about the new content of the screen, and to update the screen reader focus accordingly. For
        /// example, to move it off an accessibility node that may have been scrolled out of the screen.
        /// </para>
        /// <para>
        /// If the scrolling fails for the specified direction (which might happen, for example, if the scroll view's
        /// content is already at the top, and the user tries to scroll up), return @@false@@.
        /// </para>
        /// <para>
        /// On Android, if the node has the role <see cref="AccessibilityRole.Slider"/>, then when the user performs a
        /// "scroll" action, the events <see cref="AccessibilityNode.incremented"/> or
        /// <see cref="AccessibilityNode.decremented"/> are triggered instead of this one.
        /// </para>
        /// <para>
        /// **Platform support**: This event is not triggered on macOS.
        /// </para>
        /// </remarks>
        public event Func<AccessibilityScrollDirection, bool> scrolled;

        /// <summary>
        /// Event invoked when the user performs a "dismiss" action when focused on the accessibility node.
        /// </summary>
        /// <returns>@@true@@ if the action succeeds and @@false@@ otherwise.</returns>
        /// <remarks>
        /// <para>
        /// Subscribe to this event if the visual element that the node represents can be revealed modally or in a
        /// hierarchy. For example, you might subscribe to this event if the node represents a dialog box to give users
        /// a deliberate dismiss action that closes it.
        /// </para>
        /// <para>
        /// Your callback should dismiss the visual element represented by the node. After performing any tasks, return
        /// an appropriate value to indicate success or failure.
        /// </para>
        /// <para>
        /// On Android, subscribing to this event enables the "dismiss" action and prompt screen reader to provide
        /// instructions on how to activate it. This action is available in the TalkBack local context menu and is
        /// different from the "back" system gesture, which activates the Back button of the device.
        /// </para>
        /// <para>
        /// **Platform support**: This event is not triggered on Windows. To dismiss a view on Windows, bind your
        /// "dismiss" code to the **Escape** key.
        /// </para>
        /// </remarks>
        public event Func<bool> dismissed;

        AccessibilityHierarchy m_Hierarchy;

        internal List<AccessibilityNode> childList = new();

        /// <summary>
        /// The node's children in the accessibility hierarchy.
        /// </summary>
        /// <remarks>
        /// To add a new child, call <see cref="AccessibilityHierarchy.AddNode"/> or
        /// <see cref="AccessibilityHierarchy.InsertNode"/> on the hierarchy that the node belongs to, passing the node
        /// as the parent. To make an existing node a child of this node, or move a child to a different node, call
        /// <see cref="AccessibilityHierarchy.MoveNode"/>. To remove a child, call
        /// <see cref="AccessibilityHierarchy.RemoveNode"/>.
        /// </remarks>
        public IReadOnlyList<AccessibilityNode> children => childList;

        /// <summary>
        /// The node's parent in the accessibility hierarchy.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the node is at the root level of the hierarchy, the value of this property is @@null@@.
        /// </para>
        /// <para>
        /// To change the node's parent, call <see cref="AccessibilityHierarchy.MoveNode"/> on the hierarchy that the
        /// node belongs to.
        /// </para>
        /// </remarks>
        public AccessibilityNode parent { get; private set; }

        string m_Label;

        /// <summary>
        /// A short description of the accessibility node.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The node's label should provide a concise, human-readable description of the visual element represented by
        /// the node.
        /// </para>
        /// <para>
        /// The label is essential to screen reader users because it provides the text that the screen reader announces
        /// when a user focuses on the node to communicate the purpose or content of the visual element that the node
        /// represents.
        /// <para>
        /// </para>
        /// Generally, all accessibility nodes should have an appropriate label. Nodes with an empty label can cause
        /// unwanted screen reader behavior.
        /// <para>
        /// </para>
        /// A good label is short, informative, and does not include the node's type. For example, the label for a Save
        /// button should be "Save", not "Save button". To ensure proper screen reader intonation, start the label with
        /// a capital letter and avoid ending it with a period.
        /// </para>
        /// <para>
        /// The label works in tandem with other node properties to provide a comprehensive experience to the user.
        /// While the label provides the main identifier of the accessibility node, additional information can be
        /// supplied through properties such as the <see cref="AccessibilityNode.value"/>,
        /// <see cref="AccessibilityNode.hint"/>, <see cref="AccessibilityNode.role"/> and
        /// <see cref="AccessibilityNode.state"/>.
        /// </para>
        /// </remarks>
        public string label
        {
            get => m_Label;
            set
            {
                if (string.Equals(m_Label, value))
                {
                    return;
                }

                m_Label = value;

                if (IsInActiveHierarchy())
                {
                    AccessibilityNodeManager.SetLabel(id, value);
                }
            }
        }

        string m_Value;

        /// <summary>
        /// The value of the visual element that the accessibility node represents.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The node's value can be used to provide dynamic feedback about the node's content or input, such as the text
        /// within a text field, the position of a slider, or the progress of a task.
        /// </para>
        /// <para>
        /// Set this property only for nodes whose content cannot be fully conveyed by their label. For example, the
        /// label of a node that represents a slider might be "Volume", but its value is "50%", which indicates the
        /// current volume level. In this case, users need to know not just the slider's identity but also its current
        /// value. Conversely, for a node representing a Save button, the label alone provides all the necessary
        /// information, and setting a value would be redundant and confusing.
        /// </para>
        /// <para>
        /// If the value is set, the screen reader announces it when the user focuses on the node, before or after
        /// reading the node's label (depending on the platform).
        /// </para>
        /// <para>
        /// To ensure users receive accurate and up-to-date information, update this property whenever the state of the
        /// node changes. For example, update the value of a node representing a text field whenever the user enters new
        /// text.
        /// </para>
        /// <para>
        /// **Notes**:
        ///
        /// On macOS, this property has **no** effect on nodes with the following roles:
        ///
        ///- <see cref="AccessibilityRole.Toggle"/>
        ///- <see cref="AccessibilityRole.TabButton"/>
        ///
        /// On Windows, this property has effect only on nodes with the following roles:
        ///
        ///- <see cref="AccessibilityRole.Button"/>
        ///- <see cref="AccessibilityRole.Image"/>
        ///- <see cref="AccessibilityRole.SearchField"/>
        ///- <see cref="AccessibilityRole.Slider"/>
        ///- <see cref="AccessibilityRole.TextField"/>
        ///- <see cref="AccessibilityRole.Dropdown"/>
        ///- <see cref="AccessibilityRole.ScrollView"/>
        ///
        /// On Windows, nodes with the role <see cref="AccessibilityRole.ScrollView"/> must have
        /// a value containing a number between 0 and 100 to accurately communicate the scroll percentage
        /// to the screen reader. For scroll views that support both vertical and horizontal scrolling, the value
        /// must contain two numbers, with the vertical scroll percentage listed first.
        /// For example, a value of `50, 75` indicates that the scroll view represented by the node is scrolled 50% vertically and 75% horizontally.
        /// </para>
        /// </remarks>
        public string value
        {
            get => m_Value;
            set
            {
                if (string.Equals(m_Value, value))
                {
                    return;
                }

                m_Value = value;

                if (IsInActiveHierarchy())
                {
                    AccessibilityNodeManager.SetValue(id, value);
                }
            }
        }

        string m_Hint;

        /// <summary>
        /// Additional guidance or context for interacting with the accessibility node.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The node's hint supplements the other node attributes by offering concise instructions or clarifications to
        /// screen reader users, especially when they are not immediately obvious. For example, input requirements,
        /// instructions for interacting with custom controls, or the results of performing an action on the node.
        /// </para>
        /// <para>
        /// Set this property only when additional context or instructions are needed beyond what the label and other
        /// node attributes provide. For example, a node representing a Save button with the label "Save" does not need
        /// a hint like "Double-tap to save your changes". However, a node representing a Username text field might
        /// include a hint like "Use only letters and numbers."
        /// </para>
        /// <para>
        /// Some accessibility roles or events provide built-in hints. For example, when focusing on a node with the
        /// role <see cref="AccessibilityRole.Toggle"/>, the screen reader may automatically say "Double-tap to toggle."
        /// after announcing the node. In such cases, setting this property is unnecessary.
        /// </para>
        /// <para>
        /// When the user focuses on the node, the screen reader first announces the node's label and any other set node
        /// attributes. If the hint is set, the screen reader says it last. On some platforms, the hint functions as a
        /// tooltip, so the screen reader says it when the user pauses over the node.
        /// </para>
        /// <para>
        /// To ensure proper screen reader intonation, begin the hint with a verb, capitalize the first letter, and end
        /// the hint with a period.
        /// </para>
        /// <para>
        /// **Note**: On Windows, this property has no effect on nodes with the following roles:
        ///
        ///- <see cref="AccessibilityRole.StaticText"/>
        ///- <see cref="AccessibilityRole.Header"/>
        ///
        /// **Platform support**: On Android, this property is only supported starting with Android 8.0 (API level 26).
        /// </para>
        /// </remarks>
        public string hint
        {
            get => m_Hint;
            set
            {
                if (string.Equals(m_Hint, value))
                {
                    return;
                }

                m_Hint = value;

                if (IsInActiveHierarchy())
                {
                    AccessibilityNodeManager.SetHint(id, value);
                }
            }
        }

        Rect m_Frame;

        /// <summary>
        /// The bounding rectangle of the accessibility node in screen coordinates.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The node's frame defines the position and size of the node in screen coordinates. It is essential to screen
        /// reader users, as it provides the coordinates where the screen reader draws its cursor when focused on the
        /// node.
        /// </para>
        /// <para>
        /// If the visual element represented by the node is in world space, convert its world coordinates to screen
        /// coordinates in order to set the frame.
        /// </para>
        /// <para>
        /// Update the node's frame whenever the position or size of the visual element it represents changes. For
        /// example, when the element is moved, resized, or animated, when the user scrolls the application's interface,
        /// or when the orientation of the screen changes. Ensure that the coordinates of the visual element are
        /// up-to-date before updating the frame by waiting until its layout is finalized. For example, at the end of
        /// the frame in which the layout change occurred.
        /// </para>
        /// <para>
        /// The node's frame can be set either through this property or through the
        /// <see cref="AccessibilityNode.frameGetter"/> delegate. Using the delegate should simplify the update of the
        /// node's frame by automatically keeping it in sync with the coordinates of the visual element represented by
        /// the node.
        /// </para>
        /// <para>
        /// If this property is not set, it gets its value from the <see cref="AccessibilityNode.frameGetter"/>.
        /// </para>
        /// <para>
        /// **Notes**:
        ///
        ///- If the node has <see cref="AccessibilityNode.isActive"/> set to @@true@@, and the frame is outside of the application window, the screen reader may still be able to focus on the node.
        ///- If the frame's size is zero, the screen reader may skip the node and its children even if they have <see cref="AccessibilityNode.isActive"/> set to @@true@@.
        ///- If the <see cref="AccessibilityNode.frameGetter"/> is not set, calling <see cref="AccessibilityHierarchy.RefreshNodeFrames"/> sets the value of this property to <see cref="Rect.zero"/>.
        /// </para>
        /// </remarks>
        public Rect frame
        {
            get => m_Frame == default ? m_Frame = frameGetter?.Invoke() ?? Rect.zero : m_Frame;
            set
            {
                // Set the frame even if it is the same, because it needs to be converted to screen coordinates on the
                // native side, which could be different if the app window was moved, for example.
                m_Frame = value;

                if (IsInActiveHierarchy())
                {
                    AccessibilityNodeManager.SetFrame(id, value);
                }
            }
        }

        Func<Rect> m_FrameGetter;

        /// <summary>
        /// Delegate that calculates the frame of the accessibility node, automatically keeping it up-to-date.
        /// </summary>
        /// <returns>The calculated frame of the accessibility node, in screen coordinates.</returns>
        /// <remarks>
        /// <para>
        /// The node's frame can be set either through this delegate or through the
        /// <see cref="AccessibilityNode.frame"/> property. Using this delegate should simplify the update of the node's
        /// frame by automatically keeping it in sync with the coordinates of the visual element represented by the
        /// node.
        /// </para>
        /// <para>
        /// If the <see cref="AccessibilityNode.frame"/> is not set, it gets its value from this delegate.
        /// </para>
        /// <para>
        /// **Note**: If this delegate is not set, calling <see cref="AccessibilityHierarchy.RefreshNodeFrames"/> sets
        /// the <see cref="AccessibilityNode.frame"/> to <see cref="Rect.zero"/>.
        /// </para>
        /// </remarks>
        public Func<Rect> frameGetter
        {
            get => m_FrameGetter;
            set
            {
                // Set the frame even if it is the same, because it needs to be converted to screen coordinates on the
                // native side, which could be different if the app window was moved, for example.
                m_FrameGetter = value;

                if (IsInActiveHierarchy())
                {
                    AccessibilityNodeManager.SetFrame(id, frame);
                }
            }
        }

        /// <summary>
        /// The unique identifier of the accessibility node.
        /// </summary>
        /// <remarks>
        /// The node's identifier is unique within the application and is assigned by Unity.
        /// </remarks>
        public int id { get; private set; }

        AccessibilityRole m_Role;

        /// <summary>
        /// The type of user interface element that the accessibility node represents.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The node's role defines the type or purpose of the visual element represented by the node. It assigns a
        /// semantic meaning to the node, such as a heading, a button, or a toggle, that informs screen readers how to
        /// interact with the node and announce its purpose appropriately.
        /// </para>
        /// <para>
        /// For example, on mobile platforms, a node with the role <see cref="AccessibilityRole.Button"/> can be
        /// interacted with by performing a double-tap gesture, while a node with the role
        /// <see cref="AccessibilityRole.Slider"/> can be interacted with by performing a swipe gesture. Alternatively,
        /// a node with the role <see cref="AccessibilityRole.Header"/> can be used in heading navigation, which allows
        /// users to more efficiently navigate an application by moving from one heading to the next without having to
        /// navigate through all the content in between.
        /// </para>
        /// <para>
        /// Setting accurate roles improves the usability and user experience of your user interface by enabling the
        /// screen reader to set clear expectations for the user. A missing role can block the user from interacting
        /// with your application, and an incorrect role can cause confusion and frustration.
        /// </para>
        /// <para>
        /// If the role is set, the screen reader might announce it when the user focuses on the node, usually after
        /// reading the node's label and value.
        /// </para>
        /// <para>
        /// If the visual element that the node represents has a role that is not covered by any of the predefined
        /// <see cref="AccessibilityRole"/>s, use the default value of this property,
        /// <see cref="AccessibilityRole.None"/>, and provide information about the node's purpose and behavior in
        /// properties such as the <see cref="AccessibilityNode.label"/> and <see cref="AccessibilityNode.hint"/>.
        /// </para>
        /// </remarks>
        public AccessibilityRole role
        {
            get => m_Role;
            set
            {
                if (m_Role == value)
                {
                    return;
                }

                m_Role = value;

                if (IsInActiveHierarchy())
                {
                    AccessibilityNodeManager.SetRole(id, value);
                }
            }
        }

        AccessibilityState m_State;

        /// <summary>
        /// The status of the visual element that the accessibility node represents.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The node's state represents the current status or condition of the visual element represented by the node.
        /// It allows screen readers to provide users with dynamic feedback about the state of interactive elements,
        /// such as whether a checkbox is checked or whether a button is disabled.
        /// </para>
        /// <para>
        /// If the state is set, the screen reader announces it when the user focuses on the node, before or after
        /// reading the node's label and value (depending on the platform).
        /// </para>
        /// <para>
        /// To ensure users receive accurate and up-to-date information, update this property whenever the state of the
        /// node changes. For example, update the state of a node representing a checkbox whenever the user toggles it.
        /// </para>
        /// <para>
        /// If the visual element that the node represents has a state that is not covered by any of the predefined
        /// <see cref="AccessibilityState"/>s, use the default value of this property,
        /// <see cref="AccessibilityState.None"/>, and provide information about the node's current state in properties
        /// such as the <see cref="AccessibilityNode.value"/> and <see cref="AccessibilityNode.hint"/>.
        /// </para>
        /// </remarks>
        public AccessibilityState state
        {
            get => m_State;
            set
            {
                if (m_State == value)
                {
                    return;
                }

                m_State = value;

                if (IsInActiveHierarchy())
                {
                    AccessibilityNodeManager.SetState(id, value);
                }
            }
        }

        bool m_IsActive = true;

        /// <summary>
        /// Whether the accessibility node is exposed to screen readers. The default value is @@true@@.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property controls the visibility of the node to screen readers, ensuring that only relevant elements
        /// are accessible to the user. For example, elements that are covered by a modal view should not be
        /// accessible.
        /// </para>
        /// <para>
        /// If this property is set to @@false@@, the screen reader ignores the node and prevents the user from focusing
        /// on it. This is useful when temporarily disabling the node is preferred over removing it from the hierarchy.
        /// </para>
        /// </remarks>
        public bool isActive
        {
            get => m_IsActive;
            set
            {
                if (m_IsActive == value)
                {
                    return;
                }

                m_IsActive = value;

                if (IsInActiveHierarchy())
                {
                    AccessibilityNodeManager.SetIsActive(id, value);
                }
            }
        }

        /// <summary>
        /// Whether the accessibility node is currently focused by the screen reader.
        /// </summary>
        public bool isFocused => IsInActiveHierarchy() && AccessibilityNodeManager.GetIsFocused(id);

        bool m_AllowsDirectInteraction;

        /// <summary>
        /// Whether the accessibility node allows direct touch interaction while the screen reader is active.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property controls whether the node can be interacted with using standard gestures, bypassing the screen
        /// reader. This is useful for elements such as a piano keyboard, a drawing canvas, or gameplay that requires
        /// quick or complex gestures.
        /// </para>
        /// <para>
        /// If this property is set to @@true@@, the screen reader provides instructions on how to enable direct touch.
        /// The user can then directly interact with the user interface area corresponding to the node's
        /// <see cref="AccessibilityNode.frame"/> without the screen reader interfering.
        /// </para>
        /// <para>
        /// If this property is set to @@true@@ and the node does not have a <see cref="AccessibilityNode.label"/>, the
        /// screen reader announces the node as a "direct touch area".
        /// </para>
        /// <para>
        /// Generally, accessibility nodes should have this property set to @@false@@ to ensure that the screen reader
        /// can provide the necessary context and instructions to the user. Only set this property to @@true@@ in cases
        /// where the user experience would be significantly impaired by the screen reader's interference, and where the
        /// user can still understand and interact with the content of the direct touch area without the screen reader's
        /// assistance.
        /// </para>
        /// <para>
        /// If necessary, use the <see cref="AccessibilityNode.hint"/> property to provide users with instructions on
        /// how to interact with the content of the node when direct interaction is enabled.
        /// </para>
        /// <para>
        /// **Platform support**: This property is only supported on iOS.
        /// </para>
        /// </remarks>
        public bool allowsDirectInteraction
        {
            get => m_AllowsDirectInteraction;
            set
            {
                if (m_AllowsDirectInteraction == value)
                {
                    return;
                }

                m_AllowsDirectInteraction = value;

                if (IsInActiveHierarchy())
                {
                    AccessibilityNodeManager.SetAllowsDirectInteraction(id, value);
                }
            }
        }

        internal AccessibilityNode(int nodeId, AccessibilityHierarchy hierarchy)
        {
            id = nodeId;
            m_Hierarchy = hierarchy;

            if (!IsInActiveHierarchy())
            {
                return;
            }

            var nodeData = new AccessibilityNodeData
            {
                nodeId = nodeId
            };

            CreateNativeNodeWithData(ref nodeData);
        }

        void CreateNativeNodeWithData(ref AccessibilityNodeData nodeData)
        {
            // Ignore unsupported platforms, where AccessibilityNodeManager.CreateNativeNodeWithData returns false.
            if (!AccessibilityManager.isSupportedPlatform)
            {
                return;
            }

            if (!AccessibilityNodeManager.CreateNativeNodeWithData(nodeData))
            {
                throw new InvalidOperationException($"{nameof(CreateNativeNodeWithData)}: Could not create native " +
                    $"accessibility node with ID {nodeData.nodeId}. Please check the Player log for more details.");
            }
        }

        internal void GetNodeData(ref AccessibilityNodeData nodeData)
        {
            var nodeChildIds = new int[children.Count];

            for (var i = 0; i < children.Count; ++i)
            {
                nodeChildIds[i] = children[i].id;
            }

            nodeData.childIds = nodeChildIds;

            nodeData.label = label;
            nodeData.value = value;
            nodeData.hint = hint;

            nodeData.frame = frame;

            nodeData.nodeId = id;
            nodeData.parentId = parent?.id ?? AccessibilityNodeManager.k_InvalidNodeId;

            nodeData.role = role;
            nodeData.state = state;

            nodeData.isActive = isActive;
            nodeData.allowsDirectInteraction = allowsDirectInteraction;

            nodeData.implementsInvoked = invoked != null;
            nodeData.implementsScrolled = scrolled != null;
            nodeData.implementsDismissed = dismissed != null;
        }

        internal void AllocateNative()
        {
            if (!IsInActiveHierarchy())
            {
                return;
            }

            var nodeData = new AccessibilityNodeData
            {
                label = label,
                value = value,
                hint = hint,

                frame = frame,

                nodeId = id,
                parentId = parent?.id ?? AccessibilityNodeManager.k_InvalidNodeId,

                role = role,
                state = state,

                isActive = isActive,
                allowsDirectInteraction = allowsDirectInteraction,

                implementsInvoked = invoked != null,
                implementsScrolled = scrolled != null,
                implementsDismissed = dismissed != null,
            };

            CreateNativeNodeWithData(ref nodeData);

            foreach (var child in children)
            {
                child.AllocateNative();
            }
        }

        internal void FreeNative(bool freeChildren)
        {
            if (freeChildren)
            {
                foreach (var child in children)
                {
                    child.FreeNative(true);
                }
            }

            if (IsInActiveHierarchy())
            {
                AccessibilityNodeManager.DestroyNativeNode(id);
            }
        }

        internal void Destroy(bool destroyChildren)
        {
            // Free the native side of the node first, that way no changes on managed need to be synchronized.
            FreeNative(freeChildren: destroyChildren);

            parent?.childList.Remove(this);

            // Test boolean value once instead of once per loop iteration.
            if (destroyChildren)
            {
                for (var i = childList.Count - 1; i >= 0; i--)
                {
                    childList[i].Destroy(true);
                }
            }
            else // Re-parent all children to node's parent.
            {
                foreach (var child in childList)
                {
                    // Even if the parent is null (i.e. the node is a root), we need to assign it as the children's
                    // parent because that happens when this method is called by AccessibilityHierarchy.RemoveNode and
                    // that can happen with a root node with destroyChildren being false (therefore, the children became
                    // roots themselves).
                    child.SetParent(parent);
                    parent?.childList.Add(child);
                }
            }

            childList.Clear();

            m_Hierarchy = null;
        }

        bool IsInActiveHierarchy()
        {
            return m_Hierarchy != null && AssistiveSupport.activeHierarchy == m_Hierarchy;
        }

        internal void SetParent(AccessibilityNode nodeParent, int index = -1)
        {
            parent = nodeParent;

            // Even if the parent is not changing, the index may have changed, so update the native node.
            if (IsInActiveHierarchy())
            {
                var parentId = nodeParent?.id ?? AccessibilityNodeManager.k_InvalidNodeId;
                AccessibilityNodeManager.SetParent(id, parentId, index);
            }
        }

        /// <summary>
        /// A hash used for comparisons.
        /// </summary>
        /// <returns>A unique hash code.</returns>
        public override int GetHashCode()
        {
            return id;
        }

        /// <summary>
        /// Provides a debugging string.
        /// </summary>
        /// <returns>A string containing the accessibility node ID and generational version.</returns>
        public override string ToString()
        {
            return $"AccessibilityNode(ID: {id}, Label: \"{label}\")";
        }

        internal void NotifyFocusChanged(bool isNodeFocused)
        {
            AccessibilityManager.QueueNotification(new AccessibilityManager.NotificationContext
            {
                notification = isNodeFocused ? AccessibilityManager.Notification.ElementFocused : AccessibilityManager.Notification.ElementUnfocused,
                focusedNode = this,
            });
        }

        internal void InvokeFocusChanged(bool isNodeFocused)
        {
            focusChanged?.Invoke(this, isNodeFocused);
        }

        internal bool InvokeNodeInvoked()
        {
            return invoked?.Invoke() ?? false;
        }

        internal bool InvokeIncremented()
        {
            if (incremented == null)
            {
                return false;
            }

            incremented.Invoke();

            return true;
        }

        internal bool InvokeDecremented()
        {
            if (decremented == null)
            {
                return false;
            }

            decremented?.Invoke();

            return true;
        }

        internal bool InvokeScrolled(AccessibilityScrollDirection direction)
        {
            return scrolled?.Invoke(direction) ?? false;
        }

        internal bool InvokeDismissed()
        {
            return dismissed?.Invoke() ?? false;
        }
    }
}
