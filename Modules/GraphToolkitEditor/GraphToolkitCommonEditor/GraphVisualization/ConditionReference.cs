// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization
{
    /// <summary>
    /// Identifies a condition inside a visualization <see cref="Context"/> so visualization changes can be applied to it.
    /// </summary>
    /// <remarks>
    /// Obtain a <see cref="ConditionReference"/> from <see cref="Context.GetConditionReference"/>, providing the unique identifier of the condition.
    /// The reference is only meaningful for the <see cref="Context"/> that produced it.
    /// Two <see cref="ConditionReference"/> values are equal when they share the same <see cref="ConditionID"/> and refer to the same <see cref="Context"/> instance.
    ///
    /// Create a visualization context for a graph, retrieve a <see cref="ConditionReference"/> for a condition, apply a customization, then clear it.
    /// </remarks>
    /// <example>
    /// <code>
    /// using Context context = Registry.CreateVisualizationContext(graph.ID);
    /// ConditionReference condition = context.GetConditionReference(conditionID);
    /// condition.Icon = DebugStyles.PendingIcon;
    /// condition.ClearCustomization();
    /// </code>
    /// </example>
    /// <seealso cref="Context"/>
    /// <seealso cref="Context.GetConditionReference(Hash128)"/>
    public readonly struct ConditionReference : IEquatable<ConditionReference>
    {
        /// <summary>
        /// Unique identifier of the referenced condition.
        /// </summary>
        public Hash128 ConditionID { get; }

        /// <summary>
        /// Visualization context that produced the current condition reference.
        /// </summary>
        public Context Context { get; }

        internal ConditionReference(Context context, Hash128 conditionID)
        {
            Context = context;
            ConditionID = conditionID;
        }

        /// <summary>
        /// The icon override for the referenced condition, or <see langword="null"/> when no icon override is applied.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when you access this method after you call <see cref="Context.Dispose"/> on the context.</exception>
        /// <remarks>
        /// Getting this property returns the override set through this reference, or <see langword="null"/> when none is set. Read <see cref="ICondition.Icon"/> for the icon the condition itself authored.
        /// Setting a new value overwrites any existing icon override for that condition; it does not change the condition's own <see cref="ICondition.Icon"/>. Setting <see langword="null"/> removes the override, so the condition's row shows the condition's own icon again.
        /// Reading or setting this property has no effect when the <see cref="ConditionReference"/> has no associated <see cref="Context"/>, such as when it is <c>default</c>.
        /// Throws <see cref="ObjectDisposedException"/> when you access this property after you call <see cref="Context.Dispose"/> on the context.
        ///
        /// Set this to one of <see cref="DebugStyles.TrueIcon"/>, <see cref="DebugStyles.FalseIcon"/>, or
        /// <see cref="DebugStyles.PendingIcon"/> to indicate the condition's evaluation status, or to a custom icon.
        /// </remarks>
        /// <example>
        /// <code>
        /// ConditionReference condition = context.GetConditionReference(conditionID);
        /// condition.Icon = DebugStyles.PendingIcon;
        /// </code>
        /// </example>
        public Texture2D Icon
        {
            get => Context != null ? Context.ConditionVisuals.GetIcon(this) : null;
            set => Context?.ConditionVisuals.SetIcon(this, value);
        }

        /// <summary>
        /// Clears the icon override previously applied to the referenced condition.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when you access this method after you call <see cref="Context.Dispose"/> on the context.</exception>
        /// <remarks>
        /// After this call, <see cref="Icon"/> reads the condition's own icon again.
        /// Has no effect when the <see cref="ConditionReference"/> has no associated <see cref="Context"/>, such as when it is <c>default</c>.
        /// Throws <see cref="ObjectDisposedException"/> when you access this method after you call <see cref="Context.Dispose"/> on the context.
        /// </remarks>
        /// <example>
        /// <code>
        /// ConditionReference condition = context.GetConditionReference(conditionID);
        /// condition.ClearCustomization();
        /// </code>
        /// </example>
        public void ClearCustomization()
        {
            Context?.ConditionVisuals.Clear(this);
        }

        /// <summary>
        /// Indicates whether the current <see cref="ConditionReference"/> is equal to another <see cref="ConditionReference"/>.
        /// </summary>
        /// <param name="other">The other <see cref="ConditionReference"/> to compare with the current instance.</param>
        /// <returns>true if both references share the same <see cref="ConditionID"/> and refer to the same <see cref="Context"/> instance; otherwise, false.</returns>
        public bool Equals(ConditionReference other)
        {
            return ConditionID == other.ConditionID && Context == other.Context;
        }

        /// <summary>
        /// Indicates whether the current <see cref="ConditionReference"/> is equal to another object.
        /// </summary>
        /// <param name="obj">The object to compare with the current <see cref="ConditionReference"/>.</param>
        /// <returns>true if <paramref name="obj"/> is a <see cref="ConditionReference"/> equal to the current instance; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is ConditionReference other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code for the current <see cref="ConditionReference"/>.
        /// </summary>
        /// <returns>A hash code derived from the <see cref="ConditionID"/> and <see cref="Context"/> of the current reference.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(ConditionID, Context);
        }

        /// <summary>
        /// Compares two <see cref="ConditionReference"/> values for equality.
        /// </summary>
        /// <param name="left">The first <see cref="ConditionReference"/> to compare.</param>
        /// <param name="right">The second <see cref="ConditionReference"/> to compare.</param>
        /// <returns>true if <paramref name="left"/> is equal to <paramref name="right"/>. Otherwise, false.</returns>
        public static bool operator ==(ConditionReference left, ConditionReference right) => left.Equals(right);

        /// <summary>
        /// Compares two <see cref="ConditionReference"/> values for inequality.
        /// </summary>
        /// <param name="left">The first <see cref="ConditionReference"/> to compare.</param>
        /// <param name="right">The second <see cref="ConditionReference"/> to compare.</param>
        /// <returns>true if <paramref name="left"/> isn't equal to <paramref name="right"/>. Otherwise, false.</returns>
        public static bool operator !=(ConditionReference left, ConditionReference right) => !left.Equals(right);
    }
}
