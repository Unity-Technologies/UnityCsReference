// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Selects a <see cref="BlackboardElement"/> when it receives a mouse down event.
    /// </summary>
    class BlackboardClickSelector : ClickSelector
    {
        /// <inheritdoc />
        protected override bool IsSelected(ModelView modelView)
        {
            if (modelView is BlackboardElement blackboardElement)
            {
                return blackboardElement.IsSelected();
            }

            return false;
        }

        /// <inheritdoc />
        protected override void HandleClick(IMouseEvent e)
        {
            var baseEvent = (EventBase)e;

            if (baseEvent.currentTarget is BlackboardElement blackboardElement)
            {
                SelectElements(blackboardElement, e.shiftKey, e.actionKey);
                baseEvent.StopPropagation();
            }
        }

        /// <summary>
        /// Selects an element.
        /// </summary>
        /// <param name="clickedElement">The element to select.</param>
        /// <param name="isShiftKeyDown">Whether the shift key is down. This is used to alter the way the selection is changed.</param>
        /// <param name="isActionKeyDown">Whether the action key is down. This is used to alter the way the selection is changed.</param>
        public static void SelectElements(BlackboardElement clickedElement, bool isShiftKeyDown, bool isActionKeyDown)
        {
            if (clickedElement.GraphElementModel == null)
                return;

            if (clickedElement.GraphElementModel.IsSelectable())
            {
                if (isShiftKeyDown)
                {
                    clickedElement.BlackboardView?.ExtendSelection(clickedElement);
                }
                else
                {
                    var selectionMode = isActionKeyDown
                        ? SelectElementsCommand.SelectionMode.Toggle
                        : SelectElementsCommand.SelectionMode.Replace;
                    clickedElement.BlackboardView?.Dispatch(new SelectElementsCommand(selectionMode, clickedElement.GraphElementModel));
                }
            }
        }
    }
}
