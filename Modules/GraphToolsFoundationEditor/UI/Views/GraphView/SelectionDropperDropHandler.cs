// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Handles drag and drop of variable declarations from the <see cref="SelectionDropper"/>.
    /// Create variable nodes based on the variable declarations dragged
    /// </summary>
    class SelectionDropperDropHandler : IDragAndDropHandler
    {
        readonly List<VariableDeclarationModel> m_DraggedElements = new List<VariableDeclarationModel>();
        ISelectionDraggerTarget m_CurrentTarget;

        const float k_DragDropSpacer = 25f;

        protected GraphView GraphView { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectionDropperDropHandler"/> class.
        /// </summary>
        /// <param name="graphView">The view receiving the dragged elements.</param>
        public SelectionDropperDropHandler(GraphView graphView)
        {
            GraphView = graphView;
        }

        void GetDraggedElements()
        {
            var graphElementModels = SelectionDropper.GetDraggedElements();
            var graphModel = GraphView.GraphModel;
            m_DraggedElements.Clear();
            foreach (var model in graphElementModels)
            {
                if (model is VariableDeclarationModel variableDeclarationModel && CanBeDropped(variableDeclarationModel, graphModel))
                {
                    m_DraggedElements.Add(variableDeclarationModel);
                }
            }
        }

        static bool CanBeDropped(VariableDeclarationModel variableDeclarationModel, GraphModel graphModel)
        {
            return variableDeclarationModel.GraphModel == graphModel
                && graphModel.Stencil is Stencil stencil
                && stencil.CanCreateVariableNode(variableDeclarationModel, graphModel);
        }

        /// <inheritdoc />
        public virtual bool CanHandleDrop()
        {
            var dndContent = SelectionDropper.GetDraggedElements();
            return dndContent.OfType<VariableDeclarationModel>().Any();
        }

        /// <inheritdoc />
        public virtual void OnDragUpdated(DragUpdatedEvent e)
        {
            if (m_DraggedElements.Count > 0)
                DragAndDrop.visualMode = e.ctrlKey ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
            else
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;

            var previousTarget = m_CurrentTarget;
            m_CurrentTarget = (e.target as VisualElement)?.GetFirstOfType<Port>();
            if (m_CurrentTarget != previousTarget)
            {
                previousTarget?.ClearDropHighlightStatus();
                m_CurrentTarget?.SetDropHighlightStatus(m_DraggedElements);
            }

            e.StopPropagation();
        }

        /// <inheritdoc />
        public virtual void OnDragPerform(DragPerformEvent e)
        {
            if (m_DraggedElements.Count > 0)
            {
                m_DraggedElements.Sort(GroupItemOrderComparer_Internal.Default);

                var contentViewContainer = GraphView.ContentViewContainer;
                var variablesWithInfo = m_DraggedElements.Select(
                    (e1, i) =>
                    (
                        e1,
                        contentViewContainer.WorldToLocal(e.mousePosition) - i * k_DragDropSpacer * Vector2.down)
                    ).ToList();

                m_CurrentTarget?.ClearDropHighlightStatus();

                var command = new CreateNodeCommand();

                var portTarget = (e.target as VisualElement)?.GetFirstOfType<Port>();
                var variablesCount = variablesWithInfo.Count();
                foreach (var (model, position) in variablesWithInfo)
                {
                    if (portTarget != null && variablesCount == 1 && portTarget.CanAcceptDrop(new List<GraphElementModel>{ model }))
                        command.WithNodeOnPort(model, portTarget.PortModel, position, true);
                    else
                        command.WithNodeOnGraph(model, position);
                }

                GraphView.Dispatch(command);
            }

            m_CurrentTarget = null;
            m_DraggedElements.Clear();
            e.StopPropagation();
        }

        /// <inheritdoc />
        public virtual void OnDragEnter(DragEnterEvent e)
        {
            GetDraggedElements();
        }

        /// <inheritdoc />
        public virtual void OnDragLeave(DragLeaveEvent e)
        {
        }

        /// <inheritdoc />
        public virtual void OnDragExited(DragExitedEvent e)
        {
            m_DraggedElements.Clear();
        }
    }
}
