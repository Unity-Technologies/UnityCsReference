using System;

namespace UnityEngine.UIElements
{
    public enum KeyboardNavigationOperation
    {
        None,
        SelectAll,
        Cancel,
        Submit,
        Previous,
        Next,
        PageUp,
        PageDown,
        Begin,
        End,
    }

    public class KeyboardNavigationManipulator : Manipulator
    {
        readonly Action<KeyboardNavigationOperation, EventBase> m_Action;

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
            m_Action?.Invoke(operation, evt);
        }
    }
}
