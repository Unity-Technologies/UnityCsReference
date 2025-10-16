// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Manipulator to detect when the mouse enters or leaves a transition, showing and hiding the connectors.
    /// </summary>
    [UnityRestricted]
    internal class TransitionHoverDetector : MouseManipulator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransitionHoverDetector"/> class.
        /// </summary>
        public TransitionHoverDetector()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            target.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseEnterEvent>(OnMouseEnter);
            target.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
        }

        void OnMouseEnter(MouseEnterEvent evt)
        {
            if ((evt.pressedButtons & 1 << (int)MouseButton.LeftMouse) == 0)
            {
                if (target is AbstractTransition transition && !transition.TransitionModel.IsSingleStateTransition)
                {
                    transition.Hovered = true;

                    if (transition.IsSelected())
                    {
                        var fromStateGuid = transition.TransitionModel.FromNodeGuid;
                        var fromStateUI = fromStateGuid.GetView<GraphElement>(transition.GraphView);
                        (fromStateUI as INodeWithConnector)?.ShowConnector(transition);

                        var toStateGuid = transition.TransitionModel.ToNodeGuid;
                        var toStateUI = toStateGuid.GetView<GraphElement>(transition.GraphView);
                        (toStateUI as INodeWithConnector)?.ShowConnector(transition);
                    }
                }
            }
        }

        void OnMouseLeave(MouseLeaveEvent evt)
        {
            if (target is AbstractTransition transition && !transition.TransitionModel.IsSingleStateTransition)
            {
                transition.Hovered = false;
            }
        }
    }
}
