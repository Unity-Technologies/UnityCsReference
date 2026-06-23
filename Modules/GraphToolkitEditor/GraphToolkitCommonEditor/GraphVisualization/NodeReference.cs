// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Identifies a node inside a visualization <see cref="Context"/> so visualization changes can be applied to it.
/// </summary>
/// <remarks>
/// Obtain a <see cref="NodeReference"/> from <see cref="Context.GetNodeReference"/>. The reference is only meaningful for the <see cref="Context"/> that produced it.
/// You can use its properties to set, retrieve, or clear customization for that specific node in the graph canvas.
/// Two <see cref="NodeReference"/> values are equal when they share the same <see cref="NodeID"/> and refer to the same <see cref="Context"/> instance.
/// </remarks>
/// <example>
/// <code>
/// // Obtain a reference to a node by its GUID, then drive its visual state.
/// using Context context = Registry.CreateVisualizationContext(graphGuid);
/// NodeReference nodeRef = context.GetNodeReference(nodeID);
///
/// // Show a half-filled accent bar on the node.
/// nodeRef.FillAmount = 50f;
///
/// // Replace the static fill with a looping progress animation.
/// context.Motion.Play(nodeRef, animationSpeed: 1f);
///
/// // Stop the animation. The bar remains visible at the previously set fill amount.
/// context.Motion.Stop(nodeRef);
/// </code>
/// </example>
/// <seealso cref="Context"/>
/// <seealso cref="Context.GetNodeReference(Hash128)"/>
public readonly struct NodeReference : IEquatable<NodeReference>
{
    /// <summary>
    /// Unique identifier of the node this reference points to.
    /// </summary>
    public readonly Hash128 NodeID { get; }

    /// <summary>
    /// Visualization context that produced the current node reference.
    /// </summary>
    public readonly Context Context { get; }

    internal NodeReference(Context context, Hash128 nodeID)
    {
        Context = context;
        NodeID = nodeID;
    }

    /// <summary>
    /// The progress fill amount displayed on the referenced node's accent bar, expressed as a percentage.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when you access this method after you call <see cref="Context.Dispose"/> on the context.</exception>
    /// <remarks>
    /// Accepted values range from -100 to 100. A positive value fills the bar from left to right.
    /// A negative value fills it from right to left. A value of <c>0</c> hides the bar.
    /// Setting this property has no effect when the <see cref="NodeReference"/> has no associated <see cref="Context"/>, such as when it is <c>default</c>.
    /// Throws <see cref="ObjectDisposedException"/> when you access this method after you call <see cref="Context.Dispose"/> on the context.
    /// </remarks>
    public readonly float FillAmount
    {
        set => Context?.NodeAccent.SetFillAmount(NodeID, value);
        get => Context != null && Context.NodeAccent.TryGetFillAmount(NodeID, out var amount) ? amount : 0f;
    }

    /// <summary>
    /// Clears all customization previously applied to the referenced node, removes any fill amount, and stops any looping animation.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when you access this method after you call <see cref="Context.Dispose"/> on the context.</exception>
    /// <remarks>
    /// Resets the referenced node's accent bar to its inactive state.
    /// This call removes any fill amount previously set through <see cref="FillAmount"/> and stops any looping animation started through <see cref="GraphMotion.Play"/>.
    /// If the current reference does not correspond to any node in the graph, any stored visual data for that node is removed and the call has no further effect.
    /// Throws <see cref="ObjectDisposedException"/> when you access this method after you call <see cref="Context.Dispose"/> on the context.
    /// </remarks>
    /// <example>
    /// Remove every visual override previously applied to a node and resets the referenced node's accent bar to its inactive state.
    /// <code>
    /// NodeReference node = context.GetNodeReference(nodeID);
    /// node.ClearCustomization();
    /// </code>
    /// </example>
    public readonly void ClearCustomization()
    {
        Context?.NodeAccent.ClearNodeAccent(NodeID);
    }

    /// <summary>
    /// Indicates whether the current <see cref="NodeReference"/> is equal to another <see cref="NodeReference"/>.
    /// </summary>
    /// <param name="other">The other <see cref="NodeReference"/> to compare with the current instance.</param>
    /// <returns>true if both references share the same <see cref="NodeID"/> and refer to the same <see cref="Context"/> instance. Otherwise, false.</returns>
    public bool Equals(NodeReference other)
    {
        return NodeID == other.NodeID && ReferenceEquals(Context, other.Context);
    }

    /// <summary>
    /// Indicates whether the current <see cref="NodeReference"/> is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with the current <see cref="NodeReference"/>.</param>
    /// <returns>true if <paramref name="obj"/> is a <see cref="NodeReference"/> equal to the current instance. Otherwise, false.</returns>
    public override bool Equals(object obj)
    {
        return obj is NodeReference other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for the current <see cref="NodeReference"/>.
    /// </summary>
    /// <returns>A hash code derived from the <see cref="NodeID"/> and <see cref="Context"/> of the current reference.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(NodeID, Context);
    }

    /// <summary>
    /// Compares two <see cref="NodeReference"/> values for equality.
    /// </summary>
    /// <param name="left">The first <see cref="NodeReference"/> to compare.</param>
    /// <param name="right">The second <see cref="NodeReference"/> to compare.</param>
    /// <returns>true if <paramref name="left"/> is equal to <paramref name="right"/>. Otherwise, false.</returns>
    public static bool operator ==(NodeReference left, NodeReference right) => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="NodeReference"/> values for inequality.
    /// </summary>
    /// <param name="left">The first <see cref="NodeReference"/> to compare.</param>
    /// <param name="right">The second <see cref="NodeReference"/> to compare.</param>
    /// <returns>true if <paramref name="left"/> isn't equal to <paramref name="right"/>. Otherwise, false.</returns>
    public static bool operator !=(NodeReference left, NodeReference right) => !left.Equals(right);
}
