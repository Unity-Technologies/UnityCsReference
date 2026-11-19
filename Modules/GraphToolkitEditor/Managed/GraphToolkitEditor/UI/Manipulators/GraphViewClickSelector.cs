// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Selects a <see cref="GraphElement"/> when it receives a mouse down event.
    /// </summary>
    [UnityRestricted]
    internal class GraphViewClickSelector : ClickSelector
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
        protected override void DoSelect(IMouseEvent e)
        {
            var baseEvent = (EventBase)e;

            if (baseEvent.currentTarget is GraphElement graphElement)
            {
                SelectElements(graphElement, e.actionKey, false);
            }
        }

        /// <inheritdoc />
        protected override void DoInspect(IMouseEvent e)
        {
            var baseEvent = (EventBase)e;

            if (baseEvent.currentTarget is GraphElement graphElement)
            {
                graphElement.GraphView.Dispatch(new DisplayInInspectorCommand());
            }
        }

        /// <inheritdoc />
        protected override void DoSelectAndInspect(IMouseEvent e)
        {
            var baseEvent = (EventBase)e;

            if (baseEvent.currentTarget is GraphElement graphElement)
            {
                SelectElements(graphElement, e.actionKey, true);
            }
        }

        /// <summary>
        /// Selects an element.
        /// </summary>
        /// <param name="clickedElement">The element to select.</param>
        /// <param name="isActionKeyDown">Whether the action key is down. This is used to alter the way the selection is changed.</param>
        /// <param name="displayInInspector">Whether to trigger an update of the inspector.</param>
        public static void SelectElements(GraphElement clickedElement, bool isActionKeyDown, bool displayInInspector)
        {
            var clickedGraphElement = clickedElement is Marker marker ? marker.ParentModel : clickedElement.GraphElementModel;
            if (clickedGraphElement.IsSelectable())
            {
                var selectionMode = isActionKeyDown
                    ? SelectElementsCommand.SelectionMode.Toggle
                    : SelectElementsCommand.SelectionMode.Replace;

                clickedElement.GraphView?.Dispatch(new SelectElementsCommand(selectionMode, displayInInspector, clickedGraphElement));
            }
        }
    }
}
