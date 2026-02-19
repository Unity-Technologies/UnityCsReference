// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using UnityEngine;

namespace UnityEditor.Animations.AnimationWindow.TimelineFoundation
{
    /// <summary>
    /// TimeTransform is used to keep the transformation between Local and Global contexts.
    /// </summary>
    readonly struct TimeTransform : IEquatable<TimeTransform>
    {
        /// <summary>
        /// Time offset relative to parent scope.
        /// </summary>
        public readonly DiscreteTime offset;
        /// <summary>
        /// Time multiplier relative to parent scope.
        /// </summary>
        public readonly double multiplier;

        /// <summary>
        /// Represents the Identity for a TimeTransform multiplication.
        /// </summary>
        public static readonly TimeTransform Identity = new(new DiscreteTime(0), 1);

        /// <summary>
        /// Creates a TimeTransform
        /// </summary>
        /// <param name="offset">Offset time from parent scope.</param>
        /// <param name="multiplier">Time multiplier.</param>
        /// <exception cref="ArgumentException">Multiplier must be non-zero.</exception>
        public TimeTransform(DiscreteTime offset, double multiplier)
        {
            if (Math.Abs(multiplier) == 0d)
                throw new ArgumentException("multiplier must be non-zero");

            this.offset = offset;
            this.multiplier = multiplier;
        }

        /// <summary>
        /// Combines two TimeTransforms together.
        /// </summary>
        /// <remarks>This operation is not associative; the order of the arguments is important.</remarks>
        /// <param name="left">TimeTransform argument to the left of the operator.</param>
        /// <param name="right">TimeTransform argument to the right of the operator.</param>
        /// <returns>The combined TimeTransform.</returns>
        /// <exception cref="ArgumentException">Both arguments must valid TimeTranforms.</exception>
        public static TimeTransform operator *(TimeTransform left, TimeTransform right)
        {
            if (!left.IsValid())
                throw new ArgumentException($"{nameof(left)} must be a valid TimeTransform");
            if (!right.IsValid())
                throw new ArgumentException($"{nameof(right)} must be a valid TimeTransform");

            return new TimeTransform(left.offset + (right.offset / left.multiplier), left.multiplier * right.multiplier);
        }

        /// <summary>
        /// Compares two TimeTransforms to see if they are the same.
        /// </summary>
        /// <param name="a">first TimeTransform object to compare.</param>
        /// <param name="b">second TimeTransform object to compare against the first.</param>
        /// <returns>Whether two TimeTransform are equal.</returns>
        public static bool operator ==(TimeTransform a, TimeTransform b)
        {
            return a.offset == b.offset && Math.Abs(a.multiplier - b.multiplier) < Mathf.Epsilon;
        }

        /// <summary>
        /// Compares two TimeTransforms to see if they are different.
        /// </summary>
        /// <param name="a">first TimeTransform object to compare.</param>
        /// <param name="b">second TimeTransform object to compare against the first.</param>
        /// <returns>Whether two TimeTransform are not equal.</returns>
        public static bool operator !=(TimeTransform a, TimeTransform b) => !(a == b);

        /// <summary>
        /// Reinterprets a time from global context to local context.
        /// </summary>
        /// <param name="global">A time in the global context.</param>
        /// <returns>The time as interpreted in the local context.</returns>
        /// <exception cref="InvalidOperationException">TimeTransform must be valid</exception>
        public DiscreteTime InverseTransform(DiscreteTime global)
        {
            if (!IsValid())
                throw new InvalidOperationException($"{nameof(TimeTransform)} must be valid.");

            return (global - offset) * multiplier;
        }

        /// <summary>
        /// Returns the Inverse of this TimeTransform.
        /// </summary>
        public TimeTransform GetInverse()
        {
            if (!IsValid())
                throw new InvalidOperationException($"{nameof(TimeTransform)} must be valid.");

            return new TimeTransform(InverseTransform(DiscreteTime.Zero), 1 / multiplier);
        }

        /// <summary>
        /// Reinterprets a time from local context to global context.
        /// </summary>
        /// <param name="local">A time in the local context.</param>
        /// <returns>The time as interpreted in the global context.</returns>
        public DiscreteTime Transform(DiscreteTime local)
        {
            if (!IsValid())
                throw new InvalidOperationException($"{nameof(TimeTransform)} must be valid.");

            return (local / multiplier) + offset;
        }

        /// <summary>
        /// Returns true if this TimeTransform is equal to a given TimeTransform, false otherwise.
        /// </summary>
        public bool Equals(TimeTransform other) => this == other;
        /// <summary>
        /// Returns true if this TimeTransform is equal to a given TimeTransform, false otherwise.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj != null && Equals((TimeTransform)obj);
        }
        /// <summary>
        /// Returns the HashCode for this TimeTransform
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(offset.GetHashCode(), multiplier.GetHashCode());
        }
        /// <summary>
        /// Returns the string for this TimeTransform
        /// </summary>
        public override string ToString()
        {
            return $"TimeTransform(offset: {offset}, multiplier: {multiplier})";
        }
        /// <summary>
        /// Returns whether this TimeTransform is valid.
        /// </summary>
        public bool IsValid() => (Math.Abs(multiplier) != 0d);
    }
}
