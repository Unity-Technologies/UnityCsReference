// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Identifies a transition inside a visualization <see cref="Context"/> so visualization changes can be applied to it.
/// </summary>
/// <remarks>
/// Obtain a <see cref="TransitionReference"/> from <see cref="Context.GetTransitionReference"/>, providing the
/// unique identifier of the transition. The reference is only meaningful for the <see cref="Context"/> that
/// produced it. You can use its properties to set, retrieve, or clear customization for that specific transition
/// in the state machine canvas.
/// Two <see cref="TransitionReference"/> values are equal when they share the same <see cref="TransitionID"/> and
/// refer to the same <see cref="Context"/> instance.
/// </remarks>
/// <example>
/// <code>
/// using var debugContext = Registry.CreateVisualizationContext(myStateMachine.ID);
/// ITransition startTransition = myStateMachine.GetTransitions(startState, nextState).First();
///
/// TransitionReference startTransitionRef = debugContext.GetTransitionReference(startTransition.ID);
/// startTransitionRef.LineColor = DebugStyles.Pending;
/// debugContext.Motion.Play(startTransitionRef);
/// </code>
/// </example>
/// <seealso cref="Context"/>
/// <seealso cref="Context.GetTransitionReference(Hash128)"/>
public readonly struct TransitionReference : IEquatable<TransitionReference>
{
    /// <summary>
    /// Unique identifier of the transition this reference points to.
    /// </summary>
    public Hash128 TransitionID { get; }

    /// <summary>
    /// Visualization context that produced the current transition reference.
    /// </summary>
    public Context Context { get; }

    internal TransitionReference(Context context, Hash128 transitionID)
    {
        Context = context;
        TransitionID = transitionID;
    }

    /// <summary>
    /// The icon override applied in the transition inspector for the referenced transition, or <see langword="null"/> when no icon override is applied.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when you access this method after you call <see cref="Context.Dispose"/> on the context.</exception>
    /// <remarks>
    /// Getting this property returns the override set through this reference, or <see langword="null"/> when none is set. Read <see cref="ITransition.Icon"/> for the icon the transition itself authored.
    /// Setting a new value overwrites any existing icon override for that transition; it does not change the transition's own <see cref="ITransition.Icon"/>. Setting <see langword="null"/> removes the override, so the inspector shows the transition's own icon again.
    /// Reading or setting this property has no effect when the <see cref="TransitionReference"/> has no associated <see cref="Context"/>, such as when it is <c>default</c>.
    /// Throws <see cref="ObjectDisposedException"/> when you access this property after you call <see cref="Context.Dispose"/> on the context.
    ///
    /// Set this to one of <see cref="DebugStyles.TrueIcon"/>, <see cref="DebugStyles.FalseIcon"/>, or
    /// <see cref="DebugStyles.PendingIcon"/> to indicate the transition's evaluation status, or to a custom icon.
    /// </remarks>
    /// <example>
    /// <code>
    /// TransitionReference transition = context.GetTransitionReference(transitionID);
    /// transition.Icon = DebugStyles.PendingIcon;
    /// </code>
    /// </example>
    public Texture2D Icon
    {
        get => Context != null && Context.TransitionVisuals.TryGet(TransitionID, out var data)
            ? data.Icon
            : null;
        set => Context?.TransitionVisuals.UpdateVisualData(TransitionID, data => data.Icon = value);
    }

    /// <summary>
    /// The color the referenced transition's arrow is filled with, or <see langword="null"/> when no fill color override is applied.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when you access this property after you call <see cref="Context.Dispose"/> on the context.</exception>
    /// <remarks>
    /// This property sets the transition arrow's fill color only. Use <see cref="LineColor"/> to color the transition wire and arrow border.
    /// Setting <see langword="null"/> means no fill color is applied, in which case the transition falls back to <see cref="ITransition.FillColor"/>.
    /// Setting a new value overwrites any existing fill color for that transition. Setting the value back to
    /// <see langword="null"/> removes it.
    /// Getting or setting this property has no effect, and getting it returns <see langword="null"/>, when the
    /// <see cref="TransitionReference"/> has no associated <see cref="Context"/>, such as when it is <c>default</c>.
    /// Throws <see cref="ObjectDisposedException"/> when you access this property after you call <see cref="Context.Dispose"/> on the context.
    /// </remarks>
    /// <example>
    /// <code>
    /// TransitionReference transitionRef = context.GetTransitionReference(transitionID);
    /// transitionRef.FillColor = DebugStyles.Pending;
    /// </code>
    /// </example>
    public Color? FillColor
    {
        get => Context != null && Context.TransitionVisuals.TryGet(TransitionID, out var data)
            ? data.FillColor
            : null;
        set => Context?.TransitionVisuals.UpdateVisualData(TransitionID, data => data.FillColor = value);
    }

    /// <summary>
    /// The color the referenced transition's wire and arrow border is drawn with, or <see langword="null"/> when no line color override is applied.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when you access this property after you call <see cref="Context.Dispose"/> on the context.</exception>
    /// <remarks>
    /// This property sets the transition's wire and arrow border color only. Use <see cref="FillColor"/> to set the transition arrow's fill color.
    /// Setting <see langword="null"/> means no line color is applied, in which case the transition falls back to <see cref="ITransition.LineColor"/>.
    /// Setting a new value overwrites any existing line color for that transition. Setting the value back to
    /// <see langword="null"/> removes it.
    /// Getting or setting this property has no effect, and getting it returns <see langword="null"/>, when the
    /// <see cref="TransitionReference"/> has no associated <see cref="Context"/>, such as when it is <c>default</c>.
    /// Throws <see cref="ObjectDisposedException"/> when you access this property after you call <see cref="Context.Dispose"/> on the context.
    /// </remarks>
    /// <example>
    /// <code>
    /// TransitionReference transitionRef = context.GetTransitionReference(transitionID);
    /// transitionRef.LineColor = DebugStyles.Pending;
    /// </code>
    /// </example>
    public Color? LineColor
    {
        get => Context != null && Context.TransitionVisuals.TryGet(TransitionID, out var data)
            ? data.LineColor
            : null;
        set => Context?.TransitionVisuals.UpdateVisualData(TransitionID, data => data.LineColor = value);
    }

    /// <summary>
    /// The line width override applied to the referenced transition.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when you access this property after you call <see cref="Context.Dispose"/> on the context.</exception>
    /// <remarks>
    /// Setting the value to 0 removes the width override on the transition.
    /// Setting a new value overwrites any existing width override for that transition.
    /// On a state-to-state transition, this property sets the width of the transition's wire and of the border
    /// of its arrow. On a self transition, this property sets the width of the transition arrow's border.
    /// Getting or setting this property has no effect, and getting it returns <c>0f</c>, when the
    /// <see cref="TransitionReference"/> has no associated <see cref="Context"/>, such as when it is <c>default</c>.
    /// Throws <see cref="ObjectDisposedException"/> when you access this property after you call <see cref="Context.Dispose"/> on the context.
    /// </remarks>
    /// <example>
    /// Override the line width of a transition so it appears thicker than the default thickness.
    /// <code>
    /// TransitionReference transitionRef = context.GetTransitionReference(transitionID);
    /// transitionRef.WidthOverride = 4f;
    /// </code>
    /// </example>
    public float WidthOverride
    {
        get => Context != null && Context.TransitionVisuals.TryGet(TransitionID, out var data)
            ? data.WidthOverride
            : 0f;
        set => Context?.TransitionVisuals.UpdateVisualData(TransitionID, data => data.WidthOverride = value);
    }

    /// <summary>
    /// The opacity multiplier applied to the referenced transition.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when you access this property after you call <see cref="Context.Dispose"/> on the context.</exception>
    /// <remarks>
    /// The opacity multiplier is clamped to the [0, 1] range when set.
    /// Setting a new value overwrites any existing opacity override for that transition.
    /// On a transition between two states, this property fades the transition wire and leaves the arrow solid.
    /// On a self transition, this property fades the transition arrow's border, leaving the arrow head solid.
    /// Getting or setting this property has no effect, and getting it returns <c>1f</c>, when the
    /// <see cref="TransitionReference"/> has no associated <see cref="Context"/>, such as when it is <c>default</c>.
    /// Throws <see cref="ObjectDisposedException"/> when you access this property after you call <see cref="Context.Dispose"/> on the context.
    /// </remarks>
    /// <example>
    /// Reduce the opacity of a transition to draw it semi-transparently in the state machine canvas, for example to
    /// de-emphasize a transition that was not taken.
    /// <code>
    /// TransitionReference transitionRef = context.GetTransitionReference(transitionID);
    /// transitionRef.Opacity = 0.5f;
    /// </code>
    /// </example>
    public float Opacity
    {
        get => Context != null && Context.TransitionVisuals.TryGet(TransitionID, out var data) ? data.Opacity : 1f;
        set => Context?.TransitionVisuals.UpdateVisualData(TransitionID, data => data.Opacity = Mathf.Clamp01(value));
    }

    /// <summary>
    /// Whether the referenced transition is drawn with a dashed pattern.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when you access this property after you call <see cref="Context.Dispose"/> on the context.</exception>
    /// <remarks>
    /// Setting a new value overwrites any existing dash pattern override for that transition.
    /// The transition keeps the dashed pattern until you set this property back to <c>false</c> or call <see cref="ClearCustomization"/>.
    /// On a state-to-state transition, this property dashes the transition's wire.
    /// On a self transition, this property dashes the transition arrow's border instead.
    /// Getting or setting this property has no effect, and getting it returns <c>false</c>, when the
    /// <see cref="TransitionReference"/> has no associated <see cref="Context"/>, such as when it is <c>default</c>.
    /// Throws <see cref="ObjectDisposedException"/> when you access this property after you call <see cref="Context.Dispose"/> on the context.
    /// </remarks>
    /// <example>
    /// <code>
    /// TransitionReference transitionRef = context.GetTransitionReference(transitionID);
    /// transitionRef.IsDashed = true;
    /// </code>
    /// </example>
    public bool IsDashed
    {
        get => Context != null && Context.TransitionVisuals.TryGet(TransitionID, out var data) && (data.IsDashed ?? false);
        set => Context?.TransitionVisuals.UpdateVisualData(TransitionID, data => data.IsDashed = value);
    }

    /// <summary>
    /// Clears all customization previously applied to the referenced transition.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when you access this method after you call <see cref="Context.Dispose"/> on the context.</exception>
    /// <remarks>
    /// If the current reference does not correspond to any transition in the state machine, any stored visual data for that
    /// transition is removed and the call has no further effect.
    /// Calling this method has no effect when the <see cref="TransitionReference"/> has no associated
    /// <see cref="Context"/>, such as when it is <c>default</c>.
    /// Throws <see cref="ObjectDisposedException"/> when you access this method after you call <see cref="Context.Dispose"/> on the context.
    /// </remarks>
    /// <example>
    /// Remove every visual override previously applied to a transition and restore the default drawing in the
    /// state machine canvas.
    /// <code>
    /// TransitionReference transitionRef = context.GetTransitionReference(transitionID);
    /// transitionRef.ClearCustomization();
    /// </code>
    /// </example>
    public void ClearCustomization()
    {
        Context?.TransitionVisuals.Clear(TransitionID);
    }

    /// <summary>
    /// Indicates whether the current <see cref="TransitionReference"/> is equal to another <see cref="TransitionReference"/>.
    /// </summary>
    /// <param name="other">The other <see cref="TransitionReference"/> to compare with the current instance.</param>
    /// <returns>true if both references share the same <see cref="TransitionID"/> and refer to the same <see cref="Context"/> instance. Otherwise, false.</returns>
    public bool Equals(TransitionReference other)
    {
        return TransitionID == other.TransitionID && ReferenceEquals(Context, other.Context);
    }

    /// <summary>
    /// Indicates whether the current <see cref="TransitionReference"/> is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with the current <see cref="TransitionReference"/>.</param>
    /// <returns>true if <paramref name="obj"/> is a <see cref="TransitionReference"/> equal to the current instance. Otherwise, false.</returns>
    public override bool Equals(object obj)
    {
        return obj is TransitionReference other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for the current <see cref="TransitionReference"/>.
    /// </summary>
    /// <returns>A hash code derived from the <see cref="TransitionID"/> and <see cref="Context"/> of the current reference.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(TransitionID, Context);
    }

    /// <summary>
    /// Compares two <see cref="TransitionReference"/> values for equality.
    /// </summary>
    /// <param name="left">The first <see cref="TransitionReference"/> to compare.</param>
    /// <param name="right">The second <see cref="TransitionReference"/> to compare.</param>
    /// <returns>true if <paramref name="left"/> is equal to <paramref name="right"/>. Otherwise, false.</returns>
    public static bool operator ==(TransitionReference left, TransitionReference right) => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="TransitionReference"/> values for inequality.
    /// </summary>
    /// <param name="left">The first <see cref="TransitionReference"/> to compare.</param>
    /// <param name="right">The second <see cref="TransitionReference"/> to compare.</param>
    /// <returns>true if <paramref name="left"/> isn't equal to <paramref name="right"/>. Otherwise, false.</returns>
    public static bool operator !=(TransitionReference left, TransitionReference right) => !left.Equals(right);
}
