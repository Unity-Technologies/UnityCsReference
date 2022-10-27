// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
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
        /// The <see cref="GraphView"/> displaying the preview in the details panel.
        /// </summary>
        GraphView m_PreviewGraphView;

        GraphElement m_CurrentElement;

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
            m_PreviewGraphView = CreatePreviewGraphView();
            scrollView.Add(m_PreviewGraphView);
            return scrollView;
        }

        public override void UpdateDetailsPanel(ItemLibraryItem item)
        {
            base.UpdateDetailsPanel(item);

            m_PreviewGraphView.RemoveElement(m_CurrentElement);

            if (ItemHasPreview(item))
            {
                var graphItem = item as GraphNodeModelLibraryItem;
                var model = CreateGraphElementModel(m_GraphModel, graphItem);
                m_CurrentElement = ModelViewFactory.CreateUI<GraphElement>(m_PreviewGraphView, model);
                if (m_CurrentElement != null)
                {
                    m_CurrentElement.style.position = Position.Relative;
                    m_PreviewGraphView.AddElement(m_CurrentElement);
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
