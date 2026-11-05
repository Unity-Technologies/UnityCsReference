// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.Editor.Implementation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Stores information about a graph visualization context, including its unique identifier, the associated authoring graph's ID, and the visualization store for that graph.
/// </summary>
class Session
{
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

    /// <summary>
    /// The graph view of the window that has the session's graph open, or <c>null</c> if no open window has it.
    /// </summary>
    /// <remarks>
    /// The window is matched on its root graph, so the view still resolves while the window displays a subgraph of
    /// the session's graph. Resolved on every access rather than cached, since the window can be reloaded, closed,
    /// or replaced independently of the session.
    /// </remarks>
    internal GraphView GraphView => ResolveWindow()?.GraphView;

    /// <summary>
    /// The inspector view for the window that has the session's graph open, or <c>null</c> if no open window has
    /// it, or it has no inspector overlay open.
    /// </summary>
    /// <remarks>
    /// Resolved on every access rather than cached, since the inspector overlay can be toggled or rebuilt
    /// independently of the graph view.
    /// </remarks>
    internal ModelInspectorView ModelInspectorView
    {
        get
        {
            var window = ResolveWindow();
            if (window != null && window.TryGetOverlay(ModelInspectorOverlay.idValue, out var inspectorOverlay))
                return inspectorOverlay.rootVisualElement?.Q<ModelInspectorView>();

            return null;
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
    }

    /// <summary>
    /// Binds a window's views to this session and applies the stored visuals to them.
    /// </summary>
    /// <remarks>
    /// Unlike setting <see cref="SessionIsAttached"/>, this always raises <see cref="isAttached"/>, since a window can
    /// bind to a session that is already attached, for example when the window is reopened.
    /// </remarks>
    internal void AttachViews()
    {
        m_SessionIsAttached = true;
        isAttached?.Invoke();
    }

    GraphViewEditorWindow ResolveWindow()
    {
        foreach (var window in GraphViewEditorWindow.OpenedWindows)
        {
            if (window.GraphView != null && window.GetRootGraphModelGuid() == GraphID)
                return window;
        }

        return null;
    }
}
