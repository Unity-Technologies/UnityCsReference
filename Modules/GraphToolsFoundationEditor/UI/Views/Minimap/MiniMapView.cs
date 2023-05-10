// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CommandStateObserver;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// View to display the MiniMap.
    /// </summary>
    class MiniMapView : RootView
    {
        public new static readonly string ussClassName = "minimap-view";

        ModelViewUpdater m_UpdateObserver;
        ModelView m_MiniMap;
        Label m_ZoomLabel;

        public MiniMapViewModel MiniMapViewModel => (MiniMapViewModel)Model;

        /// <summary>
        /// Creates and initializes a new instance of the <see cref="MiniMapView"/> class.
        /// </summary>
        /// <param name="window">The <see cref="EditorWindow"/> containing this view.</param>
        /// <param name="parentGraphView">The <see cref="GraphView"/> associated with this view.</param>
        public static MiniMapView Create(EditorWindow window, GraphView parentGraphView)
        {
            var view = new MiniMapView(window, parentGraphView);
            view.Initialize();
            return view;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="MiniMapView"/> class. Call <see cref="RootView.Initialize"/> to initialize it.
        /// </summary>
        /// <param name="window">The <see cref="EditorWindow"/> containing this view.</param>
        /// <param name="parentGraphView">The <see cref="GraphView"/> associated with this view.</param>
        protected MiniMapView(EditorWindow window, GraphView parentGraphView)
            : base(window, parentGraphView.GraphTool)
        {
            Model = new MiniMapViewModel(parentGraphView);

            this.AddStylesheet_Internal("MiniMapView.uss");
            AddToClassList(ussClassName);
        }

        /// <inheritdoc />
        protected override void RegisterModelObservers()
        {}

        /// <inheritdoc />
        protected override void RegisterViewObservers()
        {
            if (m_UpdateObserver == null && MiniMapViewModel.ParentGraphView != null)
            {
                m_UpdateObserver = new ModelViewUpdater(this, MiniMapViewModel.GraphModelState,
                    MiniMapViewModel.SelectionState, MiniMapViewModel.GraphViewState, GraphTool.ToolState);
                GraphTool.ObserverManager.RegisterObserver(m_UpdateObserver);
            }
        }

        /// <inheritdoc />
        protected override void UnregisterModelObservers()
        {}

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
        public override void BuildUI()
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
        public override void UpdateFromModel()
        {
            if (panel == null)
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
                    BuildUI();
                }
                else if (graphModelObservation.UpdateType != UpdateType.None ||
                         selectionObservation.UpdateType != UpdateType.None ||
                         graphViewObservation.UpdateType != UpdateType.None)
                {
                    var miniMap = MiniMapViewModel.GraphModel.GetView_Internal(this);
                    miniMap?.UpdateFromModel();

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
