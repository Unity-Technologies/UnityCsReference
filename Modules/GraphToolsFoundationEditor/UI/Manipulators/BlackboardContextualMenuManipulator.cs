// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Customization of the <see cref="ContextualMenuManipulator"/> for the <see cref="BlackboardView"/>.
    /// </summary>
    class BlackboardContextualMenuManipulator : ContextualMenuManipulator
    {
        /// <inheritdoc />
        public BlackboardContextualMenuManipulator(Action<ContextualMenuPopulateEvent> menuBuilder)
            : base(menuBuilder)
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse, modifiers = EventModifiers.Shift });

            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse, modifiers = EventModifiers.Command });
            }
            else
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse, modifiers = EventModifiers.Control });
            }
        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(SelectOnMouseDown);
            base.RegisterCallbacksOnTarget();
        }

        void SelectOnMouseDown(MouseDownEvent e)
        {
            if (CanStartManipulation(e))
            {
                var baseEvent = (EventBase)e;

                if (baseEvent.currentTarget is BlackboardElement blackboardElement)
                {
                    if (!blackboardElement.IsSelected())
                    {
                        BlackboardClickSelector.SelectElements(blackboardElement, e.shiftKey, e.actionKey);
                    }

                    // Prevent parent groups to change the selection.
                    e.StopPropagation();
                }
            }
        }
    }
}
