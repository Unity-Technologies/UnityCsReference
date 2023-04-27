// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Selects a <see cref="GraphElement"/> when it receives a mouse down event.
    /// </summary>
    class GraphViewClickSelector : ClickSelector
    {
        /// <inheritdoc />
        protected override bool IsSelected(ModelView modelView)
        {
            if (modelView is GraphElement graphElement)
            {
                return graphElement.IsSelected();
            }

            return false;
        }

        /// <inheritdoc />
        protected override void HandleClick(IMouseEvent e)
        {
            var baseEvent = (EventBase)e;

            if (baseEvent.currentTarget is GraphElement graphElement)
            {
                SelectElements(graphElement, e.actionKey);
            }
        }

        /// <summary>
        /// Selects an element.
        /// </summary>
        /// <param name="clickedElement">The element to select.</param>
        /// <param name="isActionKeyDown">Whether the action key is down. This is used to alter the way the selection is changed.</param>
        public static void SelectElements(GraphElement clickedElement, bool isActionKeyDown)
        {
            var clickedGraphElement = clickedElement is Marker marker ? marker.ParentModel : clickedElement.GraphElementModel;
            if (clickedGraphElement.IsSelectable())
            {
                var selectionMode = isActionKeyDown
                    ? SelectElementsCommand.SelectionMode.Toggle
                    : SelectElementsCommand.SelectionMode.Replace;

                clickedElement.GraphView?.Dispatch(new SelectElementsCommand(selectionMode, clickedGraphElement));
            }
        }
    }
}
