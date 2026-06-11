// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Provides access to visualization data and features for a graph.
/// </summary>
/// <remarks>
/// Create a <see cref="Context"/> through <see cref="Registry.CreateVisualizationContext"/>. The context remains valid until you call <see cref="Dispose"/>.
/// While the context is alive, you can apply visualization changes (such as fill amounts and animations) to nodes in the associated graph.
/// Each context has a unique <see cref="VisualizationContextID"/> assigned at creation, which lets you distinguish ownership when multiple consumers operate on the same graph.
/// Disposing the context unregisters it from the <see cref="Registry"/> and clears any visualization data added through it.
/// </remarks>
/// <example>
/// <code>
/// // Create a visualization context for a graph, then drive a node's visual state.
/// Context context = Registry.CreateVisualizationContext(graphID);
/// NodeReference nodeRef = context.GetNodeReference(nodeID);
/// nodeRef.FillAmount = 50f;
///
/// // Clean up when no longer needed.
/// context.Dispose();
/// </code>
/// </example>
/// <seealso cref="Registry"/>
/// <seealso cref="NodeReference"/>
/// <seealso cref="GraphMotion"/>
public sealed class Context : IDisposable
{
    readonly RegistryService m_Owner;
    GraphMotion m_Motion;
    bool m_Disposed;

    PortPreviewManager m_PortPreview;

    NodeAccentManager m_NodeAccentManager;

    /// <summary>
    /// Indicates whether this context can still be used. The value becomes false after <see cref="Dispose"/> is called.
    /// </summary>
    public bool IsValid => !m_Disposed;

    /// <summary>
    /// Indicates whether the graph identified by the associated <see cref="Graph.ID"/> is currently loaded.
    /// </summary>
    /// <remarks>
    /// A graph is considered loaded when a graph view window is open for it and the underlying graph asset is attached to that window.
    /// Use this property to check whether visualization changes applied through this context can take immediate visual effect on the graph canvas.
    /// </remarks>
    public bool IsGraphLoaded => Session.GraphView?.GraphModel != null;

    /// <summary>
    /// Whether node customizations are displayed in the graph canvas.
    /// </summary>
    /// <remarks>
    /// When the value is true, all node customization changes are re-executed against the current graph canvas.
    /// When the value is false, any node customization clears.
    /// The system preserves the changes so they're restored when the value is set to true again.
    /// </remarks>
    public bool NodeCustomizationEnabled
    {
        get => NodeAccent.Enabled;
        set => NodeAccent.Enabled = value;
    }

    internal NodeAccentManager NodeAccent
    {
        get
        {
            ThrowIfDisposed();
            return m_NodeAccentManager ??= new NodeAccentManager(Session);
        }
    }

    /// <summary>
    /// The unique identifier for this context.
    /// </summary>
    public Hash128 VisualizationContextID => Session.VisualizationContextID;

    /// <summary>
    /// Retrieves a reference to a port in the graph associated with this context, using the port's unique identifier.
    /// </summary>
    /// <param name="portID">Unique identifier of the target port.</param>
    /// <returns>
    /// A <see cref="PortReference"/> that references the port with the specified ID in the graph associated with this context.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when you access this method after you call <see cref="Context.Dispose"/> on the context.</exception>
    /// <exception cref="ArgumentException">Thrown when you provide an invalid <paramref name="portID"/>.</exception>
    /// <remarks>
    /// The returned <see cref="PortReference"/> can be used to set, retrieve, or clear the port preview for that specific port in the graph canvas.
    /// Throws <see cref="ObjectDisposedException"/> when you access this property after you call <see cref="Context.Dispose"/> on the context.
    /// Throws <see cref="ArgumentException"/> when you provide an invalid <paramref name="portID"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// context.GetPortReference(portID).SetPreview("42.0");
    /// </code>
    /// </example>
    public PortReference GetPortReference(Hash128 portID)
    {
        ThrowIfDisposed();

        if (!portID.isValid)
            throw new ArgumentException("The provided port ID is not valid.", nameof(portID));

        return new PortReference(this, portID);
    }

    /// <summary>
    /// Whether the port preview feature is enabled for this visualization context. Enabled by default.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when you access this property after you call <see cref="Context.Dispose"/> on the context.</exception>
    /// <remarks>
    /// When enabled, the port previews are visible in the graph canvas and can be set and cleared using <see cref="PortReference.SetPreview(string)"/> and <see cref="PortReference.ClearPreview()"/>.
    /// When disabled, the port previews are hidden in the graph canvas.
    /// Throws <see cref="ObjectDisposedException"/> when you access this property after you call <see cref="Context.Dispose"/> on the context.
    /// </remarks>
    public bool PortPreviewEnabled
    {
        get => PortPreview.Enabled;
        set => PortPreview.Enabled = value;
    }

    internal PortPreviewManager PortPreview
    {
        get
        {
            ThrowIfDisposed();
            return m_PortPreview ??= new PortPreviewManager(Session);
        }
    }

    /// <summary>
    /// The animation system used to drive progress animations on nodes in this visualization context.
    /// </summary>
    /// <remarks>
    /// Use this property to start, stop, or pause progress animations on individual nodes identified by a <see cref="NodeReference"/>.
    /// This context owns the returned <see cref="GraphMotion"/> instance, which remains valid for the lifetime of the context.
    /// Calls made through it apply only to nodes in the associated graph.
    /// </remarks>
    public GraphMotion Motion => m_Motion;

    internal Session Session { get; }

    internal Context(Session session, RegistryService owner)
    {
        Session = session;
        m_Owner = owner;
        m_Motion = new GraphMotion(this);
    }

    /// <summary>
    /// Returns a <see cref="NodeReference"/> that identifies the node with the given ID within this visualization context.
    /// </summary>
    /// <remarks>
    /// The returned <see cref="NodeReference"/> is only meaningful for this <see cref="Context"/> and references the node by its ID rather than by direct lookup. The node doesn't need to exist in the current graph canvas at the time of this call.
    /// </remarks>
    /// <param name="nodeID">The unique identifier of the node to reference.</param>
    /// <returns>A <see cref="NodeReference"/> that targets the node identified by <paramref name="nodeID"/> within this <see cref="Context"/>.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when this <see cref="Context"/> has been disposed.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="nodeID"/> isn't a valid <see cref="Hash128"/>.</exception>
    /// <example>
    /// <code>
    /// NodeReference nodeRef = context.GetNodeReference(nodeID);
    /// nodeRef.FillAmount = 75f;
    /// </code>
    /// </example>
    public NodeReference GetNodeReference(Hash128 nodeID)
    {
        ThrowIfDisposed();

        if (!nodeID.isValid)
            throw new ArgumentException("The provided node ID isn't valid.", nameof(nodeID));

        return new NodeReference(this, nodeID);
    }

    /// <summary>
    /// Releases this context and clears all associated visualization data.
    /// </summary>
    /// <remarks>
    /// Unregisters this context from the <see cref="Registry"/> and clears any associated visualization data.
    /// Any subsequent calls after the first successful dispose are ignored.
    /// </remarks>
    /// <example>
    /// <code>
    /// Context context = Registry.CreateVisualizationContext(graphGuid);
    /// // ... use the context ...
    /// context.Dispose();
    /// </code>
    /// </example>
    public void Dispose()
    {
        if (m_Disposed)
            return;

        m_Disposed = true;
        m_Owner.Unregister(VisualizationContextID);
    }

    /// <summary>
    /// Clears all visualization data associated with this context.
    /// </summary>
    /// <remarks>
    /// Removes any visuals on the graph canvas that were added through this context.
    /// The context itself is not disposed and remains valid for setting new visualization data.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Set up a context and apply visualization changes to a few nodes.
    /// Context context = Registry.CreateVisualizationContext(graphGuid);
    ///
    /// NodeReference firstNode = context.GetNodeReference(firstNodeID);
    /// firstNode.FillAmount = 75f;
    ///
    /// NodeReference secondNode = context.GetNodeReference(secondNodeID);
    /// context.Motion.Play(secondNode, animationSpeed: 1f);
    ///
    /// // Reset all visualization between runs without disposing the context.
    /// // Fill amounts and animations applied above are cleared; the context can be reused.
    /// context.ClearAllVisualization();
    /// </code>
    /// </example>
    public void ClearAllVisualization()
    {
        NodeAccent.ClearAll();
    }

    void ThrowIfDisposed()
    {
        if (m_Disposed)
            throw new ObjectDisposedException(nameof(Context));
    }
}
