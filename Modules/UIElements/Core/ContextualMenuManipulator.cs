// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Manipulator that displays a contextual menu when the user clicks the right mouse button or presses the menu key on the keyboard.
    /// </summary>
    public class ContextualMenuManipulator : PointerManipulator
    {
        Action<ContextualMenuPopulateEvent> m_MenuBuilder;
        private bool m_AcceptClicksIfDisabled;

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal bool acceptClicksIfDisabled
        {
            get => m_AcceptClicksIfDisabled;
            set
            {
                if (m_AcceptClicksIfDisabled == value)
                    return;

                if (target != null)
                    UnregisterCallbacksFromTarget();

                m_AcceptClicksIfDisabled = value;

                if (target != null)
                    RegisterCallbacksOnTarget();
            }
        }

        private CallbackOptions CallbackOptions =>
            acceptClicksIfDisabled ? CallbackOptions.IncludeDisabled : CallbackOptions.Default;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ContextualMenuManipulator(Action<ContextualMenuPopulateEvent> menuBuilder)
        {
            m_MenuBuilder = menuBuilder;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });
            if (IsOSXContextualMenuPlatform())
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Control });
            }
        }

        /// <summary>
        /// Register the event callbacks on the manipulator target.
        /// </summary>
        protected override void RegisterCallbacksOnTarget()
        {
            if (IsOSXContextualMenuPlatform())
            {
                target.RegisterCallback<PointerDownEvent>(OnPointerDownEventOSX, CallbackOptions);
                target.RegisterCallback<PointerUpEvent>(OnPointerUpEventOSX, CallbackOptions);
                target.RegisterCallback<PointerMoveEvent>(OnPointerMoveEventOSX, CallbackOptions);
            }
            else
            {
                target.RegisterCallback<PointerUpEvent>(OnPointerUpEvent, CallbackOptions);
                target.RegisterCallback<PointerMoveEvent>(OnPointerMoveEvent, CallbackOptions);
            }

            target.RegisterCallback<KeyUpEvent>(OnKeyUpEvent, CallbackOptions);
            target.RegisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuEvent, CallbackOptions);
        }

        /// <summary>
        /// Unregister the event callbacks from the manipulator target.
        /// </summary>
        protected override void UnregisterCallbacksFromTarget()
        {
            if (IsOSXContextualMenuPlatform())
            {
                target.UnregisterCallback<PointerDownEvent>(OnPointerDownEventOSX);
                target.UnregisterCallback<PointerUpEvent>(OnPointerUpEventOSX);
                target.UnregisterCallback<PointerMoveEvent>(OnPointerMoveEventOSX);
            }
            else
            {
                target.UnregisterCallback<PointerUpEvent>(OnPointerUpEvent);
                target.UnregisterCallback<PointerMoveEvent>(OnPointerMoveEvent);
            }

            target.UnregisterCallback<KeyUpEvent>(OnKeyUpEvent);
            target.UnregisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuEvent);
        }

        /// <summary>
        /// Check whether the handling of contextual menu events needs to be adapted for OSX.
        /// </summary>
        /// <returns>True if it needs to operate as it would on OSX. False otherwise.</returns>
        protected bool IsOSXContextualMenuPlatform()
        {
            return UIElementsUtility.isOSXContextualMenuPlatform;
        }

        void OnPointerUpEvent(IPointerEvent evt)
        {
            ProcessPointerEvent(evt);
        }

        void OnPointerDownEventOSX(IPointerEvent evt)
        {
            ProcessPointerEvent(evt);
        }

        void OnPointerUpEventOSX(IPointerEvent evt)
        {
            var cmm = target.elementPanel?.contextualMenuManager;
            if (cmm != null && cmm.displayMenuHandledOSX)
                return;

            ProcessPointerEvent(evt);
        }

        void OnPointerMoveEvent(PointerMoveEvent evt)
        {
            if (evt.isPointerUp)
                OnPointerUpEvent(evt);
        }

        void OnPointerMoveEventOSX(PointerMoveEvent evt)
        {
            if (evt.isPointerUp)
                OnPointerUpEventOSX(evt);
            else if (evt.isPointerDown)
                OnPointerDownEventOSX(evt);
        }

        void ProcessPointerEvent(IPointerEvent evt)
        {
            if (CanStartManipulation(evt))
            {
                DoDisplayMenu(evt as EventBase);
            }
        }

        void OnKeyUpEvent(KeyUpEvent evt)
        {
            if (evt.keyCode == KeyCode.Menu)
            {
                DoDisplayMenu(evt);
            }
        }

        void DoDisplayMenu(EventBase evt)
        {
            if (target.elementPanel?.contextualMenuManager != null)
            {
                target.elementPanel.contextualMenuManager.DisplayMenu(evt, target);
                evt.StopPropagation();
            }
        }

        void OnContextualMenuEvent(ContextualMenuPopulateEvent evt)
        {
            m_MenuBuilder?.Invoke(evt);
        }
    }
}
