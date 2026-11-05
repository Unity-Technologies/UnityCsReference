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
    /// The root graph of the window that has the session's graph open, or <c>null</c> if no open window has it.
    /// </summary>
    /// <remarks>
    /// This is the graph identified by <see cref="GraphID"/> regardless of which graph the window currently displays.
    /// </remarks>
    internal GraphModel GraphModel
    {
        get
        {
            ResolveWindow(out var graphModel);
            return graphModel;
        }
    }

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
    /// bind to a session that is already attached, for example when the window is reopened or rebuilds its views
    /// after navigating into or out of a local subgraph.
    /// </remarks>
    internal void AttachViews()
    {
        m_SessionIsAttached = true;
        isAttached?.Invoke();
    }

    /// <summary>
    /// Resolves an element of the session's graph, searching its local subgraphs as well as the graph the window
    /// currently displays.
    /// </summary>
    /// <param name="elementID">The ID of the element to resolve.</param>
    /// <param name="element">The resolved element, or <c>null</c> if neither the session's graph hierarchy nor the displayed graph's hierarchy contains it.</param>
    /// <returns><c>true</c> if the element was resolved.</returns>
    /// <remarks>
    /// Returns <c>false</c> when the graph's asset is not open, since the hierarchy cannot be searched then.
    /// The displayed graph is searched separately because an asset subgraph the window navigated into is not one of
    /// the root graph's local subgraphs.
    /// <see cref="GraphElementModel.GraphModel"/> on the resolved element tells which graph owns it.
    /// </remarks>
    internal bool TryResolveElement(Hash128 elementID, out GraphElementModel element)
    {
        element = null;

        var window = ResolveWindow(out var graphModel);
        if (graphModel == null)
            return false;

        if (graphModel.TryGetModelFromGuid(elementID, out element, true))
            return true;

        var displayedGraphModel = window.GraphView.GraphModel;
        return displayedGraphModel != null && displayedGraphModel != graphModel
            && displayedGraphModel.TryGetModelFromGuid(elementID, out element, true);
    }

    /// <summary>
    /// Whether <paramref name="elementID"/> identifies a valid element of type <typeparamref name="T"/> in the
    /// session's graph hierarchy or in the graph the window currently displays.
    /// </summary>
    /// <typeparam name="T">The model type the element must have.</typeparam>
    /// <param name="elementID">The ID of the element to look for.</param>
    /// <returns><c>false</c> if the graph's asset is open and neither hierarchy contains the element as a <typeparamref name="T"/>; otherwise <c>true</c>.</returns>
    /// <remarks>
    /// Returns <c>true</c> when the graph's asset is not open: the element cannot be checked, so callers
    /// must keep the request rather than reject it.
    /// </remarks>
    internal bool IsElementValid<T>(Hash128 elementID) where T : GraphElementModel
    {
        return IsElementValid<T>(elementID, out _);
    }

    /// <inheritdoc cref="IsElementValid{T}(Hash128)"/>
    /// <param name="elementID">The ID of the element to look for.</param>
    /// <param name="element">The resolved element, or <c>null</c> if it could not be resolved as a <typeparamref name="T"/>, including when the graph's asset is not open.</param>
    internal bool IsElementValid<T>(Hash128 elementID, out T element) where T : GraphElementModel
    {
        element = null;
        if (GraphModel == null)
            return true;

        if (TryResolveElement(elementID, out var resolvedElement) && resolvedElement is T typedElement)
        {
            element = typedElement;
            return true;
        }

        return false;
    }

    GraphViewEditorWindow ResolveWindow()
    {
        return ResolveWindow(out _);
    }

    GraphViewEditorWindow ResolveWindow(out GraphModel graphModel)
    {
        var windows = GraphViewEditorWindow.OpenedWindows;
        for (var i = 0; i < windows.Count; ++i)
        {
            var window = windows[i];
            if (window.GraphView == null)
                continue;

            var rootGraphModel = window.GetRootGraphModel();
            if (rootGraphModel == null || rootGraphModel.Guid != GraphID)
                continue;

            graphModel = rootGraphModel;
            return window;
        }

        graphModel = null;
        return null;
    }
}
