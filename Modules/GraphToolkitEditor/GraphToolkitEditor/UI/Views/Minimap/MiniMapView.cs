// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.CSO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// View to display the MiniMap.
    /// </summary>
    [UnityRestricted]
    internal class MiniMapView : RootView
    {
        public new static readonly string ussClassName = "minimap-view";

        ModelViewUpdater m_UpdateObserver;
        ModelView m_MiniMap;
        Label m_ZoomLabel;

        public MiniMapViewModel MiniMapViewModel => (MiniMapViewModel)Model;

        /// <summary>
        /// Creates a new instance of the <see cref="MiniMapView"/> class.
        /// </summary>
        /// <param name="window">The <see cref="EditorWindow"/> containing this view.</param>
        /// <param name="graphTool">The tool hosting this view.</param>
        /// <param name="viewModel">The model for the view.</param>
        public MiniMapView(EditorWindow window, GraphTool graphTool, MiniMapViewModel viewModel)
            : base(window, graphTool)
        {
            Model = viewModel;

            this.AddPackageStylesheet("MiniMapView.uss");
            AddToClassList(ussClassName);
        }

        /// <inheritdoc />
        protected override void RegisterCommandHandlers(CommandHandlerRegistrar registrar) { }

        /// <inheritdoc />
        protected override void RegisterModelObservers()
        { }

        /// <inheritdoc />
        protected override void RegisterViewObservers()
        {
            if (m_UpdateObserver == null && MiniMapViewModel.ParentGraphView != null)
            {
                m_UpdateObserver = new ModelViewUpdater(this, MiniMapViewModel.GraphModelState,
                    MiniMapViewModel.SelectionState, MiniMapViewModel.GraphViewState, GraphTool.ToolState);
                GraphTool?.ObserverManager?.RegisterObserver(m_UpdateObserver);
            }
        }

        public override bool TryPauseViewObservers()
        {
            if (base.TryPauseViewObservers())
            {
                if (m_UpdateObserver != null) GraphTool?.ObserverManager?.UnregisterObserver(m_UpdateObserver);
                return true;
            }
            return false;
        }

        public override bool TryResumeViewObservers()
        {
            if (base.TryResumeViewObservers())
            {
                if (m_UpdateObserver != null) GraphTool?.ObserverManager?.RegisterObserver(m_UpdateObserver);
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        protected override void UnregisterModelObservers()
        { }

        /// <inheritdoc />
        protected override void UnregisterViewObservers()
        {
            if (m_UpdateObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_UpdateObserver);
                m_UpdateObserver = null;
            }
        }

        /// <summary>
        /// Rebuilds the whole minimap UI.
        /// </summary>
        public override void BuildUITree()
        {
            m_ZoomLabel?.RemoveFromHierarchy();

            if (m_MiniMap != null)
            {
                m_MiniMap.RemoveFromHierarchy();
                m_MiniMap.RemoveFromRootView();
                m_MiniMap = null;
            }

            if (MiniMapViewModel.GraphModel != null)
            {
                m_MiniMap = ModelViewFactory.CreateUI<ModelView>(this, MiniMapViewModel.GraphModel);
            }

            if (m_MiniMap != null)
            {
                Add(m_MiniMap);
            }

            m_ZoomLabel = new Label();
            m_ZoomLabel.AddToClassList("ge-zoom-label");
            Add(m_ZoomLabel);
            UpdateZoomLevel();
        }

        /// <inheritdoc />
        public override void Update()
        {
            if (panel == null)
                return;

            if (m_UpdateObserver == null)
                return;

            using (var toolObservation = m_UpdateObserver.ObserveState(GraphTool.ToolState))
            using (var selectionObservation = m_UpdateObserver.ObserveState(MiniMapViewModel.SelectionState))
            using (var graphModelObservation = m_UpdateObserver.ObserveState(MiniMapViewModel.GraphModelState))
            using (var graphViewObservation = m_UpdateObserver.ObserveState(MiniMapViewModel.GraphViewState))
            {
                if (toolObservation.UpdateType != UpdateType.None ||
                    graphModelObservation.UpdateType == UpdateType.Complete)
                {
                    // Another GraphModel loaded, or big changes in the GraphModel.
                    BuildUITree();
                }
                else if (graphModelObservation.UpdateType != UpdateType.None ||
                         selectionObservation.UpdateType != UpdateType.None ||
                         graphViewObservation.UpdateType != UpdateType.None)
                {
                    var miniMap = MiniMapViewModel.GraphModel.GetView(this);
                    miniMap?.UpdateView(UpdateFromModelVisitor.genericUpdateFromModelVisitor);

                    UpdateZoomLevel();
                }
            }
        }

        /// <summary>
        /// Updates the zoom level label.
        /// </summary>
        public void UpdateZoomLevel()
        {
            var zoom = MiniMapViewModel.GraphViewState?.Scale.x ?? -1f;

            if (m_ZoomLabel != null)
            {
                m_ZoomLabel.text = (zoom < 0) ? "" : $"Zoom: {zoom:P0}";
            }
        }
    }
}
