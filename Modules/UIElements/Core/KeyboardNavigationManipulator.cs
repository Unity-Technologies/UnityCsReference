// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents an operation that the user is trying to accomplish through a specific input mechanism.
    /// </summary>
    /// <remarks>
    /// Tests the received callback value for <see cref="KeyboardNavigationManipulator"/> against the values of this enum to implement the operation in your UI.
    /// </remarks>
    public enum KeyboardNavigationOperation
    {
        /// <summary>
        /// Default value. Indicates an uninitialized enum value.
        /// </summary>
        None,
        /// <summary>
        /// Selects all UI selectable elements or text.
        /// </summary>
        SelectAll,
        /// <summary>
        /// Cancels the current UI interaction.
        /// </summary>
        Cancel,
        /// <summary>
        /// Submits or concludes the current UI interaction.
        /// </summary>
        Submit,
        /// <summary>
        /// Moves up to the previous item.
        /// </summary>
        Previous,
        /// <summary>
        /// Moves down to the next item.
        /// </summary>
        Next,
        /// <summary>
        /// Moves to the right.
        /// </summary>
        MoveRight,
        /// <summary>
        /// Moves to the left.
        /// </summary>
        MoveLeft,
        /// <summary>
        /// Moves the selection up one page (in a list that has a scrollable area).
        /// </summary>
        PageUp,
        /// <summary>
        /// Moves the selection down one page (in a list that has a scrollable area).
        /// </summary>
        PageDown,
        /// <summary>
        /// Selects the first element.
        /// </summary>
        Begin,
        /// <summary>
        /// Selects the last element.
        /// </summary>
        End,
    }

    /// <summary>
    /// Provides a default implementation for translating input device specific events to higher level navigation operations as commonly possible with a keyboard.
    /// </summary>
    public class KeyboardNavigationManipulator : Manipulator
    {
        readonly Action<KeyboardNavigationOperation, EventBase> m_Action;

        /// <summary>
        /// Initializes and returns an instance of KeyboardNavigationManipulator, configured to invoke the specified callback.
        /// </summary>
        /// <param name="action">This action is invoked when specific low level events are dispatched to the target <see cref="VisualElement"/>,
        /// with a specific value of <see cref="KeyboardNavigationOperation"/> and a reference to the original low level event.</param>
        public KeyboardNavigationManipulator(Action<KeyboardNavigationOperation, EventBase> action)
        {
            m_Action = action;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
            target.RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
            target.RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<NavigationMoveEvent>(OnNavigationMove);
            target.UnregisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
            target.UnregisterCallback<NavigationCancelEvent>(OnNavigationCancel);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        internal void OnKeyDown(KeyDownEvent evt)
        {
            // At the moment these actions are not mapped dynamically in the InputSystemEventSystem component.
            // When that becomes the case in the future, remove the following and use corresponding Navigation events.
            KeyboardNavigationOperation GetOperation()
            {
                switch (evt.keyCode)
                {
                    case KeyCode.A when evt.actionKey: return KeyboardNavigationOperation.SelectAll;
                    case KeyCode.Home: return KeyboardNavigationOperation.Begin;
                    case KeyCode.End: return KeyboardNavigationOperation.End;
                    case KeyCode.PageUp: return KeyboardNavigationOperation.PageUp;
                    case KeyCode.PageDown: return KeyboardNavigationOperation.PageDown;
                    // Workaround for navigation with arrow keys. It will trigger the sound of an incorrect key being pressed.
                    // Since we use navigation events, the input system should already prevent the sound. See case UUM-26264.
                    case KeyCode.DownArrow:
                    case KeyCode.UpArrow:
                    case KeyCode.LeftArrow:
                    case KeyCode.RightArrow:
                        evt.StopPropagation();
                        break;
                }
                return KeyboardNavigationOperation.None;
            }

            var op = GetOperation();
            if (op != KeyboardNavigationOperation.None)
            {
                Invoke(op, evt);
            }
        }

        void OnNavigationCancel(NavigationCancelEvent evt)
        {
            Invoke(KeyboardNavigationOperation.Cancel, evt);
        }

        void OnNavigationSubmit(NavigationSubmitEvent evt)
        {
            Invoke(KeyboardNavigationOperation.Submit, evt);
        }

        void OnNavigationMove(NavigationMoveEvent evt)
        {
            switch (evt.direction)
            {
                case NavigationMoveEvent.Direction.Up:
                    Invoke(KeyboardNavigationOperation.Previous, evt);
                    break;
                case NavigationMoveEvent.Direction.Down:
                    Invoke(KeyboardNavigationOperation.Next, evt);
                    break;
                case NavigationMoveEvent.Direction.Left:
                    Invoke(KeyboardNavigationOperation.MoveLeft, evt);
                    break;
                case NavigationMoveEvent.Direction.Right:
                    Invoke(KeyboardNavigationOperation.MoveRight, evt);
                    break;
            }
        }

        void Invoke(KeyboardNavigationOperation operation, EventBase evt)
        {
            m_Action?.Invoke(operation, evt);
        }
    }
}
