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
/// A registry that is responsible for creating and managing <see cref="Context"/> instances.
/// </summary>
/// <remarks>
/// Graph visualization contexts provide access to visualization features.
/// TODO: Add more info about other visualization features when they are added in the future.
/// </remarks>
public static class Registry
{
    /// <summary>
    /// Creates a new <see cref="Context"/> and registers it.
    /// </summary>
    /// <param name="graphGuid">The authoring graph's guid, accessible through <see cref="Graph.Guid"/>. In case of subgraphs, this must be the root graph guid.</param>
    /// <returns>The <see cref="Context"/>.</returns>
    /// <throws cref="ArgumentException">Thrown when the provided graph guid is not valid.</throws>
    /// <remarks>
    /// The authoring graph's guid is used to associate the visualization context with a specific graph.
    /// In the case of subgraphs, the root graph guid must be used to ensure that the visualization context is correctly associated with the entire graph hierarchy.
    /// A unique identifier is created for this context. It is accessible through <see cref="Context.VisualizationContextId"/>.
    /// </remarks>
    public static Context CreateVisualizationContext(Hash128 graphGuid) => RegistryService.Instance.CreateVisualizationContext(graphGuid);

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
    readonly Dictionary<Hash128, Session> m_SessionsById = new();
    readonly Dictionary<Hash128, List<Session>> m_SessionsByGraph = new();
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

    internal Context CreateVisualizationContext(Hash128 graphGuid)
    {
        if (!graphGuid.isValid)
            throw new ArgumentException("The provided graph guid is not valid.", nameof(graphGuid));

        var context = new Context(new Session(graphGuid), this);
        Register(context);

        return context;
    }

    internal bool TryGetVisualizationSession(Hash128 visualizationContextId, out Session session)
    {
        return m_SessionsById.TryGetValue(visualizationContextId, out session);
    }

    internal bool TryGetVisualizationSessionForGraph(Hash128 graphGuid, out Session visualizationSession)
    {
        if (!m_SessionsByGraph.TryGetValue(graphGuid, out var sessions) || sessions.Count == 0)
        {
            visualizationSession = null;
            return false;
        }

        visualizationSession = sessions[0];
        return true;
    }

    internal void Unregister(Hash128 visualizationContextId)
    {
        if (!visualizationContextId.isValid || !m_SessionsById.TryGetValue(visualizationContextId, out var session))
            return;

        // Remove from s_SessionsById.
        contextWillUnregister?.Invoke(visualizationContextId);
        m_SessionsById.Remove(visualizationContextId);

        var graphGuid = session.GraphGuid;

        // Remove from s_SessionsByGraph using graphGuid.
        if (graphGuid.isValid && m_SessionsByGraph.TryGetValue(graphGuid, out var sessions))
        {
            for (var i = sessions.Count -1; i >= 0; i--)
            {
                if (sessions[i].VisualizationContextId == visualizationContextId)
                    sessions.RemoveAt(i);
            }

            if (sessions.Count == 0)
                m_SessionsByGraph.Remove(graphGuid);

            return;
        }

        // Fallback: if graphGuid is not found, remove from s_SessionsByGraph using the visualization context id.
        using var dispose = ListPool<Hash128>.Get(out var emptyKeysToRemove);

        foreach (var (otherGraphGuid, otherSessions) in m_SessionsByGraph)
        {
            for (var i = otherSessions.Count -1; i >= 0; i--)
            {
                if (otherSessions[i].VisualizationContextId == visualizationContextId)
                    otherSessions.RemoveAt(i);
            }

            if (otherSessions.Count == 0)
                emptyKeysToRemove.Add(otherGraphGuid);
        }

        foreach (var key in emptyKeysToRemove)
            m_SessionsByGraph.Remove(key);
    }

    internal void UnregisterAllForGraph(Hash128 graphGuid)
    {
        if (!graphGuid.isValid || !m_SessionsByGraph.TryGetValue(graphGuid, out var sessions) || sessions.Count == 0)
            return;

        using var dispose = ListPool<Hash128>.Get(out var contextIds);
        foreach (var session in sessions)
        {
            contextWillUnregister?.Invoke(session.VisualizationContextId);
            contextIds.Add(session.VisualizationContextId);
        }

        m_SessionsByGraph.Remove(graphGuid);
        foreach (var contextId in contextIds)
            m_SessionsById.Remove(contextId);
    }

    internal void UnregisterAll()
    {
        foreach (var session in m_SessionsById.Values)
        {
            contextWillUnregister?.Invoke(session.VisualizationContextId);
        }
        m_SessionsById.Clear();
        m_SessionsByGraph.Clear();
    }

    void Register(Context context)
    {
        m_SessionsById[context.VisualizationContextId] = context.Session;

        if (!m_SessionsByGraph.ContainsKey(context.Session.GraphGuid))
            m_SessionsByGraph.Add(context.Session.GraphGuid, new List<Session>());
        m_SessionsByGraph[context.Session.GraphGuid].Add(context.Session);

        contextRegistered?.Invoke(context.VisualizationContextId);
    }
}
