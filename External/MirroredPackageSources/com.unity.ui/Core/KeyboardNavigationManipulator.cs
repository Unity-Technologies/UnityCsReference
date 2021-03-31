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
        /// Selects the previous item.
        /// </summary>
        Previous,
        /// <summary>
        /// Selects the next item.
        /// </summary>
        Next,
        /// <summary>
        /// Moves the selection up one page (in a list which has scrollable area).
        /// </summary>
        PageUp,
        /// <summary>
        /// Moves the selection down one page (in a list which has scrollable area).
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
            if (target.panel?.contextType == ContextType.Editor)
                OnEditorKeyDown(evt);
            else
                OnRuntimeKeyDown(evt);
        }

        void OnRuntimeKeyDown(KeyDownEvent evt)
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
                }
                // TODO why do we want to invoke the callback in this case? Looks weird.
                return KeyboardNavigationOperation.None;
            }

            Invoke(GetOperation(), evt);
        }

        void OnEditorKeyDown(KeyDownEvent evt)
        {
            KeyboardNavigationOperation GetOperation()
            {
                switch (evt.keyCode)
                {
                    case KeyCode.A when evt.actionKey: return KeyboardNavigationOperation.SelectAll;
                    case KeyCode.Escape: return KeyboardNavigationOperation.Cancel;
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter: return KeyboardNavigationOperation.Submit;
                    case KeyCode.UpArrow: return KeyboardNavigationOperation.Previous;
                    case KeyCode.DownArrow: return KeyboardNavigationOperation.Next;
                    case KeyCode.Home: return KeyboardNavigationOperation.Begin;
                    case KeyCode.End: return KeyboardNavigationOperation.End;
                    case KeyCode.PageUp: return KeyboardNavigationOperation.PageUp;
                    case KeyCode.PageDown: return KeyboardNavigationOperation.PageDown;
                }

                return KeyboardNavigationOperation.None;
            }

            Invoke(GetOperation(), evt);
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
            }
        }

        void Invoke(KeyboardNavigationOperation operation, EventBase evt)
        {
            if (operation == KeyboardNavigationOperation.None)
                return;

            m_Action?.Invoke(operation, evt);
        }
    }
}
