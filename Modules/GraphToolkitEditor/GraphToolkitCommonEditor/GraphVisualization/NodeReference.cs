// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Identifies a node inside a visualization <see cref="Context"/> so visualization changes such as fill amount and progress can be applied to it.
/// </summary>
/// <remarks>
/// Obtain a <see cref="NodeReference"/> from <see cref="Context.GetNodeReference"/>. The reference is only meaningful for the <see cref="Context"/> that produced it.
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
public readonly struct NodeReference : IEquatable<NodeReference>
{
    /// <summary>
    /// Unique identifier of the node this reference points to.
    /// </summary>
    public readonly Hash128 NodeID { get; }

    /// <summary>
    /// The visualization context that produced this reference.
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
    /// <remarks>
    /// Accepted values range from -100 to 100. A positive value fills the bar from left to right.
    /// A negative value fills it from right to left. A value of <c>0</c> hides the bar.
    /// Setting this property has no effect when the <see cref="NodeReference"/> has no associated <see cref="Context"/>, such as when it is <c>default</c>.
    /// </remarks>
    public readonly float FillAmount
    {
        set => Context?.NodeAccent.SetFillAmount(NodeID, value);
        get => Context != null && Context.NodeAccent.TryGetFillAmount(NodeID, out var amount) ? amount : 0f;
    }

    /// <summary>
    /// Clears all customization previously applied to the referenced node, removes any fill amount, and stops any looping animation.
    /// </summary>
    /// <remarks>
    /// Resets the referenced node's accent bar to its inactive state.
    /// This call removes any fill amount previously set through <see cref="FillAmount"/> and stops any looping animation started through <see cref="GraphMotion.Play"/>.
    /// Once the request completes, the node returns to the visual state it would have without any visualization data.
    /// Calling this method has no effect when the <see cref="NodeReference"/> has no associated <see cref="Context"/>, such as when it is <c>default</c>.
    /// </remarks>
    public readonly void ClearCustomization()
    {
        Context?.NodeAccent.ClearNodeAccent(NodeID);
    }

    /// <summary>
    /// Indicates whether this <see cref="NodeReference"/> is equal to another <see cref="NodeReference"/>.
    /// </summary>
    /// <param name="other">The other <see cref="NodeReference"/> to compare with this instance.</param>
    /// <returns>true if both references share the same <see cref="NodeID"/> and refer to the same <see cref="Context"/> instance. Otherwise, false.</returns>
    /// <example>
    /// <code>
    /// NodeReference a = context.GetNodeReference(nodeID);
    /// NodeReference b = context.GetNodeReference(nodeID);
    /// bool sameTarget = a.Equals(b); // true: same context, same node ID
    /// </code>
    /// </example>
    public bool Equals(NodeReference other)
    {
        return NodeID == other.NodeID && ReferenceEquals(Context, other.Context);
    }

    /// <summary>
    /// Indicates whether this <see cref="NodeReference"/> is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with this <see cref="NodeReference"/>.</param>
    /// <returns>true if <paramref name="obj"/> is a <see cref="NodeReference"/> equal to this instance. Otherwise, false.</returns>
    /// <example>
    /// <code>
    /// NodeReference nodeRef = context.GetNodeReference(nodeID);
    /// object boxed = context.GetNodeReference(nodeID);
    /// bool sameTarget = nodeRef.Equals(boxed); // true
    /// </code>
    /// </example>
    public override bool Equals(object obj)
    {
        return obj is NodeReference other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for this <see cref="NodeReference"/>.
    /// </summary>
    /// <returns>A hash code derived from the <see cref="NodeID"/> and <see cref="Context"/> of this reference.</returns>
    /// <example>
    /// <code>
    /// HashSet&lt;NodeReference&gt; seen = new HashSet&lt;NodeReference&gt;();
    /// NodeReference nodeRef = context.GetNodeReference(nodeID);
    /// seen.Add(nodeRef); // GetHashCode is used implicitly by the HashSet
    /// </code>
    /// </example>
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
