// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// A context class that holds and provides access to visualization data and methods for a graph.
/// </summary>
/// <remarks>
/// This context is identified by a unique <see cref="VisualizationContextId"/> assigned at creation.
/// </remarks>
public sealed class Context : IDisposable
{
    readonly RegistryService m_Owner;
    bool m_Disposed;

    /// <summary>
    /// The unique identifier for this context.
    /// </summary>
    public Hash128 VisualizationContextId => Session.VisualizationContextId;

    internal Session Session { get; }

    internal Context(Session session, RegistryService owner)
    {
        Session = session;
        m_Owner = owner;
    }

    /// <summary>
    /// Disposes of the context, unregistering it from the owner registry.
    /// </summary>
    /// <remarks>
    /// Calling <see cref="Dispose"/> will mark the context as disposed and unregister it from the owner registry using its unique <see cref="VisualizationContextId"/>.
    /// Make sure to call this method when the context is no longer needed.
    /// </remarks>
    public void Dispose()
    {
        if (m_Disposed)
            return;

        m_Disposed = true;
        m_Owner.Unregister(VisualizationContextId);
    }
}
