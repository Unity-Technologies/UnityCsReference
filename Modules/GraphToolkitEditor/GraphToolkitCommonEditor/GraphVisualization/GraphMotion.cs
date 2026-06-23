// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Animation system for elements displayed in the graph canvas of a visualization <see cref="Context"/>.
/// </summary>
/// <remarks>
/// Obtain an instance from <see cref="Context.Motion"/>. Each method targets a graph element identified by a reference produced by the same <see cref="Context"/>.
/// The owning context scopes animations: stopping or pausing through one <see cref="GraphMotion"/> instance only affects the graph elements referenced by that instance.
/// Call <see cref="Play"/> after <see cref="Pause"/> to resume the looped animation.
/// </remarks>
/// <example>
/// <code>
/// NodeReference nodeRef = context.GetNodeReference(nodeID);
///
/// // Start a looping animation on the node.
/// context.Motion.Play(nodeRef);
///
/// // Pause it without hiding the accent bar.
/// context.Motion.Pause(nodeRef);
///
/// // Resume the animation at a faster speed.
/// context.Motion.Play(nodeRef, animationSpeed: 2f);
///
/// // Stop the animation. If a FillAmount was set, the bar remains visible at that fill.
/// context.Motion.Stop(nodeRef);
/// </code>
/// </example>
/// <seealso cref="Context.Motion"/>
public sealed class GraphMotion
{
    Context m_Context;
    internal GraphMotion(Context context)
    {
        m_Context = context;
    }

    /// <summary>
    /// Starts a looping animation on the referenced wire.
    /// </summary>
    /// <param name="wire">The wire on which to play the animation.</param>
    /// <param name="animationSpeed">Speed multiplier for the animation. Higher values move the animated segment faster.</param>
    /// <example>
    /// Start a flow animation on a wire to highlight an active data path through the graph at the default speed.
    /// <code>
    /// WireReference wire = context.GetWireReference(outputPortID, inputPortID);
    /// context.Motion.Play(wire);
    /// </code>
    /// </example>
    public void Play(WireReference wire, float animationSpeed = 1f) => m_Context.WireVisuals.PlayAnimation(wire, animationSpeed);

    /// <summary>
    /// Stops the looping animation on the referenced wire.
    /// </summary>
    /// <param name="wire">The wire on which to stop the animation.</param>
    /// <example>
    /// Stop a flow animation that was previously started on a wire using <see cref="Play"/>.
    /// <code>
    /// WireReference wire = context.GetWireReference(outputPortID, inputPortID);
    /// context.Motion.Stop(wire);
    /// </code>
    /// </example>
    public void Stop(WireReference wire) => m_Context.WireVisuals.StopAnimation(wire);

    /// <summary>
    /// Pauses the looping animation on the referenced wire without clearing it.
    /// </summary>
    /// <remarks>
    /// Freezes the flow animation at its current segment offset while keeping the wire's customization (dashed pattern, width, opacity) applied.
    /// Call <see cref="Play(WireReference, float)"/> again on the same wire to resume the animation at the requested speed.
    /// If you pause a wire that has no active animation there is no visible effect.
    /// </remarks>
    /// <param name="wire">The wire on which to pause the animation.</param>
    /// <example>
    /// <code>
    /// WireReference wire = context.GetWireReference(outputPortID, inputPortID);
    /// context.Motion.Play(wire);
    /// // Pause the animation; call Play again to resume.
    /// context.Motion.Pause(wire);
    /// </code>
    /// </example>
    public void Pause(WireReference wire) => m_Context.WireVisuals.PauseAnimation(wire);

    /// <summary>
    /// Starts a looping animation on the referenced node's accent.
    /// </summary>
    /// <remarks>
    /// The animation loops indefinitely until you call <see cref="Stop"/> or <see cref="Pause"/>.
    /// While the animation plays, the accent bar overrides any fill amount previously set through <see cref="NodeReference.FillAmount"/> or the node model's <c>FillAmount</c> property.
    /// Call <see cref="Play"/> again on the same node after <see cref="Pause"/> to resume the animation at the new speed.
    /// </remarks>
    /// <param name="node">The node on which to play the animation.</param>
    /// <param name="animationSpeed">Speed multiplier for the animation. Higher values move the animated segment faster. The default value is 1.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="node"/> doesn't correspond to a node in the current session.</exception>
    /// <example>
    /// <code>
    /// NodeReference nodeRef = context.GetNodeReference(nodeID);
    /// context.Motion.Play(nodeRef);
    /// </code>
    /// </example>
    public void Play(NodeReference node, float animationSpeed = 1f) => m_Context.NodeAccent.PlayLoopAnimation(node.NodeID, animationSpeed);

    /// <summary>
    /// Stops the looping animation on the referenced node's accent and hides it.
    /// </summary>
    /// <remarks>
    /// If a fill amount has been set on the node (either via <see cref="NodeReference.FillAmount"/> or via the node model's <c>FillAmount</c> property), the bar remains visible at that fill amount instead. Throws ArgumentException when <paramref name="node"/> doesn't correspond to a node in the current session.
    /// </remarks>
    /// <param name="node">The node on which to stop the animation.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="node"/> doesn't correspond to a node in the current session.</exception>
    /// <example>
    /// <code>
    /// NodeReference nodeRef = context.GetNodeReference(nodeID);
    /// context.Motion.Play(nodeRef, animationSpeed: 1f);
    /// // Later, stop the animation.
    /// context.Motion.Stop(nodeRef);
    /// </code>
    /// </example>
    public void Stop(NodeReference node) => m_Context.NodeAccent.StopLoopAnimation(node.NodeID);

    /// <summary>
    /// Pauses the looping animation on the referenced node's accent without hiding it.
    /// </summary>
    /// <remarks>
    /// Freezes the looping animation at its current state and keeps the accent bar visible on the node.
    /// Call <see cref="Play"/> again to resume the animation at the requested speed.
    /// If you pause a node that has no active animation there is no visible effect.
    /// </remarks>
    /// <param name="node">The node on which to pause the animation.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="node"/> doesn't correspond to a node in the current session.</exception>
    /// <example>
    /// <code>
    /// NodeReference nodeRef = context.GetNodeReference(nodeID);
    /// context.Motion.Play(nodeRef, animationSpeed: 1f);
    /// // Pause the animation; call Play again to resume.
    /// context.Motion.Pause(nodeRef);
    /// </code>
    /// </example>
    public void Pause(NodeReference node) => m_Context.NodeAccent.PauseLoopAnimation(node.NodeID);
}
