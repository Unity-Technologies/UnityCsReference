// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Provides a method to create visualization contexts for graphs.
/// </summary>
/// <remarks>
/// Use <see cref="CreateVisualizationContext"/> to create a <see cref="Context"/> for a specific graph.
/// Access visualization features such as port previews through <see cref="Context.PortPreview"/>.
/// Dispose of the returned <see cref="Context"/> when it is no longer needed.
/// </remarks>
/// <example>
/// <code>
/// using var context = Registry.CreateVisualizationContext(graph.ID);
/// </code>
/// </example>
/// <seealso cref="Context"/>
/// <seealso cref="Context.PortPreview"/>
public static class Registry
{
    /// <summary>
    /// Creates and registers a new visualization context.
    /// </summary>
    /// <param name="graphID">The authoring graph's ID, accessible through <see cref="Graph.ID"/>. For subgraphs, use the root graph ID.</param>
    /// <returns>The newly created <see cref="Context"/>, registered with the <see cref="Registry"/> and associated with the provided <paramref name="graphID"/>. Use it to apply visualization data to nodes in that graph.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided graph ID isn't valid.</exception>
    /// <remarks>
    /// The authoring graph's ID is used to associate the visualization context with a specific graph.
    /// For subgraphs, using the root graph ID associates the context with the entire graph hierarchy.
    /// A unique identifier for the context is accessible through <see cref="Context.VisualizationContextID"/>.
    /// Dispose of the returned <see cref="Context"/> when it is no longer needed.
    /// Throws an <see cref="ArgumentException"/> when the provided graph ID is not valid.
    /// </remarks>
    /// <example>
    /// <code>
    /// using Context context = Registry.CreateVisualizationContext(graphGuid);
    /// NodeReference nodeRef = context.GetNodeReference(nodeID);
    /// nodeRef.FillAmount = 50f;
    /// </code>
    /// </example>
    public static Context CreateVisualizationContext(Hash128 graphID) => RegistryService.Instance.CreateVisualizationContext(graphID);

    /// <summary>
    /// Retrieves the active <see cref="Context"/> for the specified graph.
    /// </summary>
    /// <param name="graphID">The authoring graph's ID, accessible through <see cref="Graph.ID"/>. In case of subgraphs, this must be the root graph ID.</param>
    /// <returns>The active <see cref="Context"/> for the graph, or <c>null</c> if no active context is registered for it.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided <paramref name="graphID"/> isn't valid.</exception>
    /// <remarks>
    /// A registered <see cref="Context"/> is considered active when its underlying session is attached. If multiple contexts are registered for the graph but none is attached, this method returns <c>null</c>.
    /// The lookup is keyed on the authoring graph's ID; for subgraphs, pass the root graph's ID.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Get the active context for the graph and disable the visualization of node accents.
    /// Context current = Registry.GetActiveContext(graphID);
    /// if (current != null)
    ///     current.NodeAccentEnabled = false;
    /// </code>
    /// </example>
    public static Context GetActiveContext(Hash128 graphID)
    {
        if (!graphID.isValid)
            throw new ArgumentException("The provided graph ID isn't valid.", nameof(graphID));
            
        return RegistryService.Instance.GetActiveContext(graphID);
    }

    internal static event Action<Hash128> contextRegistered
    {
        add => RegistryService.Instance.contextRegistered += value;
        remove => RegistryService.Instance.contextRegistered -= value;
    }
    internal static event Action<Hash128> contextWillUnregister
    {
        add => RegistryService.Instance.contextWillUnregister += value;
        remove => RegistryService.Instance.contextWillUnregister -= value;
    }
    internal static bool TryGetVisualizationSession(Hash128 visualizationContextId, out Session session) => RegistryService.Instance.TryGetVisualizationSession(visualizationContextId, out session);
    internal static bool TryGetVisualizationSessionForGraph(Hash128 graphGuid, out Session visualizationSession) => RegistryService.Instance.TryGetVisualizationSessionForGraph(graphGuid, out visualizationSession);
    internal static void UnregisterAllForGraph(Hash128 graphGuid) => RegistryService.Instance.UnregisterAllForGraph(graphGuid);
}

sealed class RegistryService
{
    internal event Action<Hash128> contextRegistered;
    internal event Action<Hash128> contextWillUnregister;

    readonly Dictionary<Hash128, Context> m_ContextById = new();
    readonly Dictionary<Hash128, List<Context>> m_ContextsByGraph = new();
    static RegistryService s_Instance;

    internal static RegistryService Instance => s_Instance ??= new RegistryService();

    RegistryService()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    void OnPlayModeStateChanged(PlayModeStateChange value)
    {
        // When exiting edit mode or play mode, all graph visualization contexts should be unregistered.
        if (value is PlayModeStateChange.ExitingEditMode or PlayModeStateChange.ExitingPlayMode)
            UnregisterAll();
    }

    internal Context GetActiveContext(Hash128 graphID)
    {
        if (!graphID.isValid)
            throw new ArgumentException("The provided graph ID isn't valid.", nameof(graphID));
        
        if (!m_ContextsByGraph.TryGetValue(graphID, out var contexts) || contexts.Count == 0)
        {
            return null;
        }

        foreach (var context in contexts)
        {
            if (context.Session.SessionIsAttached)
                return context;
        }

        return null;
    }

    internal Context CreateVisualizationContext(Hash128 graphGuid)
    {
        if (!graphGuid.isValid)
            throw new ArgumentException("The provided graph ID is not valid.", nameof(graphGuid));

        var context = new Context(new Session(graphGuid), this);
        Register(context);

        return context;
    }

    internal bool TryGetVisualizationSession(Hash128 visualizationContextId, out Session session)
    {
        session = null;
        if (!m_ContextById.TryGetValue(visualizationContextId, out var context))
            return false;
        session = context.Session;
        return true;
    }

    internal bool TryGetVisualizationSessionForGraph(Hash128 graphGuid, out Session visualizationSession)
    {
        if (!m_ContextsByGraph.TryGetValue(graphGuid, out var contexts) || contexts.Count == 0)
        {
            visualizationSession = null;
            return false;
        }

        visualizationSession = contexts[0].Session;
        return true;
    }

    internal void Unregister(Hash128 visualizationContextId)
    {
        if (!visualizationContextId.isValid || !m_ContextById.TryGetValue(visualizationContextId, out var context))
            return;

        var session = context.Session;

        // Remove from s_SessionsById.
        if (session.SessionIsAttached)
            session.SessionIsAttached = false;
        contextWillUnregister?.Invoke(visualizationContextId);
        m_ContextById.Remove(visualizationContextId);

        var graphID = session.GraphID;

        // Remove from s_ContextsByGraph using graphID.
        if (graphID.isValid && m_ContextsByGraph.TryGetValue(graphID, out var contexts))
        {
            for (var i = contexts.Count -1; i >= 0; i--)
            {
                if (contexts[i].VisualizationContextID == visualizationContextId)
                    contexts.RemoveAt(i);
            }

            if (contexts.Count == 0)
                m_ContextsByGraph.Remove(graphID);

            return;
        }

        // Fallback: if graphID is not found, remove from s_SessionsByGraph using the visualization context id.
        using var dispose = ListPool<Hash128>.Get(out var emptyKeysToRemove);

        foreach (var (otherGraphGuid, otherContexts) in m_ContextsByGraph)
        {
            for (var i = otherContexts.Count -1; i >= 0; i--)
            {
                if (otherContexts[i].VisualizationContextID == visualizationContextId)
                    otherContexts.RemoveAt(i);
            }

            if (otherContexts.Count == 0)
                emptyKeysToRemove.Add(otherGraphGuid);
        }

        foreach (var key in emptyKeysToRemove)
            m_ContextsByGraph.Remove(key);
    }

    internal void UnregisterAllForGraph(Hash128 graphGuid)
    {
        if (!graphGuid.isValid || !m_ContextsByGraph.TryGetValue(graphGuid, out var contexts) || contexts.Count == 0)
            return;

        using var dispose = ListPool<Hash128>.Get(out var contextIds);
        foreach (var context in contexts)
        {
            var session = context.Session;

            if (session.SessionIsAttached)
                session.SessionIsAttached = false;
            contextWillUnregister?.Invoke(session.VisualizationContextID);
            contextIds.Add(session.VisualizationContextID);
        }

        m_ContextsByGraph.Remove(graphGuid);
        foreach (var contextId in contextIds)
            m_ContextById.Remove(contextId);
    }

    internal void UnregisterAll()
    {
        foreach (var context in m_ContextById.Values)
        {
            var session = context.Session;

            if (session.SessionIsAttached)
                session.SessionIsAttached = false;
            contextWillUnregister?.Invoke(session.VisualizationContextID);
        }
        m_ContextById.Clear();
        m_ContextsByGraph.Clear();
    }

    void Register(Context context)
    {
        m_ContextById[context.VisualizationContextID] = context;

        if (!m_ContextsByGraph.ContainsKey(context.Session.GraphID))
            m_ContextsByGraph.Add(context.Session.GraphID, new List<Context>());
        m_ContextsByGraph[context.Session.GraphID].Add(context);

        contextRegistered?.Invoke(context.VisualizationContextID);
    }
}
