// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.Editor.Implementation;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Stores information about a graph visualization context, including its unique identifier, the associated authoring graph's ID, and the visualization store for that graph.
/// </summary>
class Session
{
    GraphView m_GraphView = null;
    bool m_SessionIsAttached;

    internal event Action isAttached;
    internal event Action isDetached;

    internal bool SessionIsAttached
    {
        get => m_SessionIsAttached;
        set
        {
            if (m_SessionIsAttached == value)
                return;

            m_SessionIsAttached = value;

            if (m_SessionIsAttached)
                isAttached?.Invoke();
            else
                isDetached?.Invoke();
        }
    }
    
    /// <summary>
    /// The unique identifier for the <see cref="Context"/>.
    /// </summary>
    public Hash128 VisualizationContextID { get; }

    /// <summary>
    /// The ID of the graph associated with the visualization session.
    /// </summary>
    /// <remarks>
    /// This is the same identifier as the one accessible through <see cref="Graph.ID"/> on the authoring graph.
    /// In the case of subgraphs, this ID must be the one of the root graph, not the subgraph being visualized.
    /// </remarks>
    public Hash128 GraphID { get; }

    /// <summary>
    /// The store for visualization data related to the graph visualization session.
    /// </summary>
    public Store Store { get; }

    internal GraphView GraphView
    {
        get
        {
            if (m_GraphView == null)
                ResolveGraphView();
            return m_GraphView;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Session"/> class.
    /// </summary>
    public Session(Hash128 graphID)
    {
        VisualizationContextID = Hash128Helpers.GenerateUnique();
        GraphID = graphID;
        Store = new Store();

        ResolveGraphView();
    }

    void ResolveGraphView()
    {
        foreach (var window in GraphViewEditorWindow.OpenedWindows)
        {
            var graphModel = window.GraphView?.GraphModel;
            if (graphModel != null && graphModel.Guid == GraphID)
            {
                m_GraphView = window.GraphView;
                break;
            }
        }
    }
}
