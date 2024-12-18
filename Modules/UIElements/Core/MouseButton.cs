// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Enumerates the mouse buttons to identify specific mouse button interactions.
    /// </summary>
    /// <remarks>
    /// Use this enumeration for mouse-related events to identify and respond to specific mouse button interactions.
    /// For example, you can check the <see cref="Button"/> property of a mouse event to determine which button triggered the event.
    /// </remarks>
    /// <example>
    /// The following example shows how to use the MouseButton enumeration.
    /// <code source="../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/Mouse_button.cs"/>
    /// </example>
    /// <remarks>
    /// SA: [[Button]], [[MouseDownEvent]], [[PointerDownEvent]]
    /// </remarks>

    public enum MouseButton
    {
        /// <summary>
        /// Represents the left mouse button. Typically used for selection or activation.
        /// </summary>
        LeftMouse = 0,
        /// <summary>
        /// Represents the right mouse button. Typically used to open context menus.
        /// </summary>
        RightMouse = 1,
        /// <summary>
        /// Represents the middle mouse button.
        /// </summary>
        MiddleMouse = 2
    }
}
