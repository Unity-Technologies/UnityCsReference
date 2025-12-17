// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.CSO;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for error toolbar buttons.
    /// </summary>
    [UnityRestricted]
    internal abstract class ErrorToolbarButton : EditorToolbarButton, IAccessContainerWindow, IToolbarElement
    {
        ErrorToolbarUpdateObserver m_UpdateObserver;

        /// <inheritdoc />
        public EditorWindow containerWindow { get; set; }

        /// <summary>
        /// The graph tools.
        /// </summary>
        protected GraphTool GraphTool => (containerWindow as GraphViewEditorWindow)?.GraphTool;

        /// <summary>
        /// The graph view.
        /// </summary>
        protected GraphView GraphView => (containerWindow as GraphViewEditorWindow)?.GraphView;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorToolbarButton"/> class.
        /// </summary>
        protected ErrorToolbarButton()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            clicked += OnClick;
        }

        /// <summary>
        /// Event handler for <see cref="AttachToPanelEvent"/>.
        /// </summary>
        /// <param name="evt">The event to handle.</param>
        protected void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            if (GraphTool != null)
            {
                if (m_UpdateObserver == null)
                {
                    m_UpdateObserver = new ErrorToolbarUpdateObserver(this, GraphTool.ToolState, GraphView.GraphViewModel.ProcessingErrorsState);
                    GraphTool.ObserverManager?.RegisterObserver(m_UpdateObserver);
                }

                if (GraphView != null)
                {
                    GraphTool.RegisterCommandHandler(
                        new CommandHandlerFunctor<GraphViewStateComponent, SetCurrentErrorCommand>()
                            .Bind(GraphView.GraphViewModel.GraphViewState)
                    );
                }
            }

            Update();
        }

        /// <summary>
        /// Event handler for <see cref="DetachFromPanelEvent"/>.
        /// </summary>
        /// <param name="evt">The event to handle.</param>
        protected void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            GraphTool?.UnregisterCommandHandler<SetCurrentErrorCommand>();
            GraphTool?.ObserverManager?.UnregisterObserver(m_UpdateObserver);
            m_UpdateObserver = null;
        }

        /// <summary>
        /// Handles click on the button.
        /// </summary>
        protected abstract void OnClick();

        /// <summary>
        /// Updates the button.
        /// </summary>
        public virtual void Update()
        {
            bool enabled = GraphTool?.ToolState.GraphModel != null;
            bool hasError = GraphView != null && GraphView.GraphViewModel.ProcessingErrorsState.Errors.Count > 0;
            SetEnabled(enabled && hasError);
        }

        /// <summary>
        /// Sends a command to reframe the graph view to show error at <propref name="index"/>.
        /// </summary>
        /// <param name="index">The index of the error.</param>
        protected void FrameAndSelectError(int index)
        {
            var isNextError = GraphView.GraphViewModel.GraphViewState.ErrorIndex < index;
            var errors = GraphView.GraphViewModel.ProcessingErrorsState.Errors;
            if (errors.Count > 0)
            {
                GraphElementModel errorModel = null;
                var iterCount = 0;
                while (errorModel == null && iterCount <= errors.Count)
                {
                    if (index >= errors.Count)
                        index = 0;

                    if (index < 0)
                        index = errors.Count - 1;

                    var errorModelGuid = errors[index].ParentModelGuid;
                    if (!GraphTool.ToolState.GraphModel.TryGetModelFromGuid(errorModelGuid, out errorModel))
                    {
                        if (isNextError)
                            index++;
                        else
                            index--;
                    }
                    iterCount++;
                }

                var ui = errorModel?.GetView<GraphElement>(GraphView);
                if (ui != null)
                {
                    var rectToFit = GraphView.CalculateRectToFitElements(new[] { ui });
                    GraphView.CalculateFrameTransform(rectToFit, GraphView.layout, GraphView.frameBorder, out var frameTranslation, out var frameScaling);

                    GraphTool.Dispatch(new SetCurrentErrorCommand(index, frameTranslation, frameScaling));
                }
            }
        }
    }
}
