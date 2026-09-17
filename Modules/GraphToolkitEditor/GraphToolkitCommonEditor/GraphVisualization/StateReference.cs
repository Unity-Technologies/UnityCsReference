// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Identifies a state inside a visualization <see cref="Context"/> so visualization changes can be applied to it.
/// </summary>
/// <remarks>
/// Obtain a <see cref="StateReference"/> from <see cref="Context.GetStateReference"/>. The reference is only meaningful for the <see cref="Context"/> that produced it.
/// You can use its properties to set, retrieve, or clear customization for that specific state in the state machine canvas.
/// Two <see cref="StateReference"/> values are equal when they share the same <see cref="StateID"/> and refer to the same <see cref="Context"/> instance.
/// </remarks>
/// <example>
/// <code>
/// // Obtain a reference to a state by its ID, then drive its visual state.
/// using Context context = Registry.CreateVisualizationContext(stateMachineID);
/// StateReference stateRef = context.GetStateReference(stateID);
///
/// // Show a half-filled accent bar on the state.
/// stateRef.FillAmount = 50f;
///
/// // Replace the static fill with a looping progress animation.
/// context.Motion.Play(stateRef, animationSpeed: 1f);
///
/// // Stop the animation. The bar remains visible at the previously set fill amount.
/// context.Motion.Stop(stateRef);
/// </code>
/// </example>
/// <seealso cref="Context"/>
/// <seealso cref="Context.GetStateReference(Hash128)"/>
public readonly struct StateReference : IEquatable<StateReference>
{
    /// <summary>
    /// Unique identifier of the state this reference points to.
    /// </summary>
    public Hash128 StateID { get; }

    /// <summary>
    /// Visualization context that produced the current state reference.
    /// </summary>
    public Context Context { get; }

    internal StateReference(Context context, Hash128 stateID)
    {
        Context = context;
        StateID = stateID;
    }

    /// <summary>
    /// The progress fill amount displayed on the referenced state's accent bar, expressed as a percentage.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when you access this method after you call <see cref="Context.Dispose"/> on the context.</exception>
    /// <remarks>
    /// Accepted values range from -100f to 100f. A positive value fills the bar from left to right.
    /// A negative value fills it from right to left. A value of <c>0</c> hides the bar.
    /// This field is an override applied on top of <see cref="IState.FillAmount"/>. The default value is <c>0</c>, but it has no effect.
    /// Call the setter to start applying the override. Call <see cref="StateReference.ClearCustomization"/> to remove the override.
    /// Setting this property has no effect when the <see cref="StateReference"/> has no associated <see cref="Context"/>, such as when it is <c>default</c>.
    /// Throws <see cref="ObjectDisposedException"/> when you access this method after you call <see cref="Context.Dispose"/> on the context.
    /// </remarks>
    public float FillAmount
    {
        set => Context?.StateAccent.SetFillAmount(StateID, value);
        get => Context != null && Context.StateAccent.TryGetFillAmount(StateID, out var amount) ? amount : 0f;
    }

    /// <summary>
    /// Clears all customization previously applied to the referenced state, removes any fill amount, and stops any looping animation.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when you access this method after you call <see cref="Context.Dispose"/> on the context.</exception>
    /// <remarks>
    /// Resets the referenced state's accent bar to its inactive state.
    /// This call removes any fill amount previously set through <see cref="FillAmount"/> and stops any looping animation started through <see cref="GraphMotion.Play"/>.
    /// If the current reference does not correspond to any state in the state machine, any stored visual data for that state is removed and the call has no further effect.
    /// Throws <see cref="ObjectDisposedException"/> when you access this method after you call <see cref="Context.Dispose"/> on the context.
    /// </remarks>
    /// <example>
    /// Remove every visual override previously applied to a state and resets the referenced state's accent bar to its inactive state.
    /// <code>
    /// StateReference state = context.GetStateReference(stateID);
    /// state.ClearCustomization();
    /// </code>
    /// </example>
    public void ClearCustomization()
    {
        Context?.StateAccent.ClearAccent(StateID);
    }

    /// <summary>
    /// Indicates whether the current <see cref="StateReference"/> is equal to another <see cref="StateReference"/>.
    /// </summary>
    /// <param name="other">The other <see cref="StateReference"/> to compare with the current instance.</param>
    /// <returns>true if both references share the same <see cref="StateID"/> and refer to the same <see cref="Context"/> instance. Otherwise, false.</returns>
    public bool Equals(StateReference other)
    {
        return StateID == other.StateID && ReferenceEquals(Context, other.Context);
    }

    /// <summary>
    /// Indicates whether the current <see cref="StateReference"/> is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with the current <see cref="StateReference"/>.</param>
    /// <returns>true if <paramref name="obj"/> is a <see cref="StateReference"/> equal to the current instance. Otherwise, false.</returns>
    public override bool Equals(object obj)
    {
        return obj is StateReference other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for the current <see cref="StateReference"/>.
    /// </summary>
    /// <returns>A hash code derived from the <see cref="StateID"/> and <see cref="Context"/> of the current reference.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(StateID, Context);
    }

    /// <summary>
    /// Compares two <see cref="StateReference"/> values for equality.
    /// </summary>
    /// <param name="left">The first <see cref="StateReference"/> to compare.</param>
    /// <param name="right">The second <see cref="StateReference"/> to compare.</param>
    /// <returns>true if <paramref name="left"/> is equal to <paramref name="right"/>. Otherwise, false.</returns>
    public static bool operator ==(StateReference left, StateReference right) => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="StateReference"/> values for inequality.
    /// </summary>
    /// <param name="left">The first <see cref="StateReference"/> to compare.</param>
    /// <param name="right">The second <see cref="StateReference"/> to compare.</param>
    /// <returns>true if <paramref name="left"/> isn't equal to <paramref name="right"/>. Otherwise, false.</returns>
    public static bool operator !=(StateReference left, StateReference right) => !left.Equals(right);
}
