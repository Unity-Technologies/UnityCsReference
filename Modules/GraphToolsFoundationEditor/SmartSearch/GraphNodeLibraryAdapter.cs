// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ItemLibrary.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// <see cref="ItemLibraryAdapter"/> for <see cref="GraphElementModel"/>.
    /// </summary>
    class GraphNodeLibraryAdapter : GraphElementLibraryAdapter
    {
        readonly GraphModel m_GraphModel;

        /// <summary>
        /// The <see cref="GraphView"/> calling the search.
        /// </summary>
        GraphView m_HostGraphView;

        /// <summary>
        /// The <see cref="GraphElement"/> of the current selected item.
        /// </summary>
        public GraphElement CurrentElement { get; private set; }

        /// <summary>
        /// The <see cref="GraphView"/> displaying the preview in the details panel.
        /// </summary>
        public GraphView PreviewGraphView { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphNodeLibraryAdapter"/> class.
        /// </summary>
        /// <param name="graphModel">The graph in which this adapter is used.</param>
        /// <param name="title">The title to use when searching with this adapter.</param>
        /// <param name="toolName">Unique, human-readable name of the tool using this adapter.</param>
        public GraphNodeLibraryAdapter(GraphModel graphModel, string title, string toolName = null)
            : base(title, toolName)
        {
            m_GraphModel = graphModel;
        }

        const string k_USSPath = "ItemLibraryGraphView.uss";
        public const string USSClassName = "ge-library-graph-view";

        GraphView CreatePreviewGraphView()
        {
            var graphview = m_HostGraphView.CreateSimplePreview();
            if (graphview != null)
            {
                graphview.AddStylesheet_Internal(k_USSPath);
                graphview.AddToClassList(USSClassName);
            }
            return graphview;
        }

        /// <inheritdoc />
        public override void SetHostGraphView(GraphView graphView)
        {
            base.SetHostGraphView(graphView);
            m_HostGraphView = graphView;
        }

        /// <inheritdoc />
        protected override ScrollView MakeDetailsPreviewContainer()
        {
            var scrollView = base.MakeDetailsPreviewContainer();
            PreviewGraphView = CreatePreviewGraphView();
            scrollView.Add(PreviewGraphView);
            return scrollView;
        }

        public override void UpdateDetailsPanel(ItemLibraryItem item)
        {
            base.UpdateDetailsPanel(item);

            PreviewGraphView.RemoveElement(CurrentElement);

            if (ItemHasPreview(item))
            {
                var graphItem = item as GraphNodeModelLibraryItem;
                var model = CreateGraphElementModel(m_GraphModel, graphItem);
                CurrentElement = ModelViewFactory.CreateUI<GraphElement>(PreviewGraphView, model);
                if (CurrentElement != null)
                {
                    CurrentElement.style.position = Position.Relative;
                    PreviewGraphView.AddElement(CurrentElement);
                }
            }
        }

        public override bool ItemHasPreview(ItemLibraryItem item)
        {
            return item is GraphNodeModelLibraryItem;
        }

        protected static GraphElementModel CreateGraphElementModel(GraphModel graphModel, GraphNodeModelLibraryItem item)
        {
            return item.CreateElement.Invoke(
                new GraphNodeCreationData(graphModel, Vector2.zero, SpawnFlags.Orphan));
        }
    }
}
