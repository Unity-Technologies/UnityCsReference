// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization
{
    /// <summary>
    /// Identifies a wire inside a visualization <see cref="Context"/> so visualization changes can be applied to it.
    /// </summary>
    /// <remarks>
    /// Obtain a <see cref="WireReference"/> from <see cref="Context.GetWireReference"/>, providing the unique identifiers of the output and input ports that define the connection.
    /// The reference is only meaningful for the <see cref="Context"/> that produced it.
    /// You can use its properties to set, retrieve, or clear customization for that specific wire in the graph canvas.
    /// Two <see cref="WireReference"/> values are equal when they share the same <see cref="OutputPortID"/> and <see cref="InputPortID"/>, and refer to the same <see cref="Context"/> instance.
    /// </remarks>
    /// <example>
    /// Create a visualization context for a graph, retrieve a <see cref="WireReference"/> for a connected pair of ports, apply customizations, then clear them.
    /// <code>
    /// using Context context = Registry.CreateVisualizationContext(graph.UID);
    /// WireReference wire = context.GetWireReference(outputPortID, inputPortID);
    /// wire.IsDashed = true;
    /// wire.WidthOverride = 4f;
    /// wire.Opacity = 0.5f;
    /// wire.ClearCustomization();
    /// </code>
    /// </example>
    /// <seealso cref="Context"/>
    /// <seealso cref="Context.GetWireReference(Hash128, Hash128)"/>
    public readonly struct WireReference : IEquatable<WireReference>
    {
        /// <summary>
        /// Unique identifier of the output port at the start of the connection.
        /// </summary>
        public Hash128 OutputPortID { get; }

        /// <summary>
        /// Unique identifier of the input port at the end of the connection.
        /// </summary>
        public Hash128 InputPortID { get; }

        /// <summary>
        /// Visualization context that produced the current wire reference.
        /// </summary>
        public Context Context { get; }

        internal WireReference(Context context, Hash128 outputPortID, Hash128 inputPortID)
        {
            Context = context;
            OutputPortID = outputPortID;
            InputPortID = inputPortID;
        }

        /// <summary>
        /// Whether the reference wire is drawn with a dashed pattern.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when you access this method after you call <see cref="Context.Dispose"/> on the context.</exception>
        /// <remarks>
        /// Setting a new value overwrites any existing dash pattern overrides for that wire. The wire keeps the dashed pattern until you set this property back to <c>false</c> or call <see cref="ClearCustomization"/>.
        /// Setting this property has no effect when the <see cref="WireReference"/> has no associated <see cref="Context"/>, such as when it is <c>default</c>.
        /// Throws <see cref="ObjectDisposedException"/> when you access this method after you call <see cref="Context.Dispose"/> on the context.
        /// </remarks>
        /// <example>
        /// Apply a dashed style to a wire by retrieving its <see cref="WireReference"/> from a visualization <see cref="Context"/> and setting this property.
        /// <code>
        /// WireReference wire = context.GetWireReference(outputPortID, inputPortID);
        /// wire.IsDashed = true;
        /// </code>
        /// </example>
        public bool IsDashed
        {
            get => Context.WireVisuals.TryGet(this, out var data) && data.IsDashed;
            set => Context.WireVisuals.UpdateVisualData(this, data => data.IsDashed = value);
        }

        /// <summary>
        /// The width override applied to the referenced wire.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when you access this method after you call <see cref="Context.Dispose"/> on the context.</exception>
        /// <remarks>
        /// Setting the value to 0 removes the width override on the wire.
        /// Setting a new value overwrites any existing width override for that wire.
        /// Setting this property has no effect when the <see cref="WireReference"/> has no associated <see cref="Context"/>, such as when it is <c>default</c>.
        /// Throws <see cref="ObjectDisposedException"/> when you access this method after you call <see cref="Context.Dispose"/> on the context.
        /// </remarks>
        /// <example>
        /// Override the line width of a wire so it appears thicker than the default thickness in the graph canvas.
        /// <code>
        /// WireReference wire = context.GetWireReference(outputPortID, inputPortID);
        /// wire.WidthOverride = 4f;
        /// </code>
        /// </example>
        public float WidthOverride
        {
            get => Context.WireVisuals.TryGet(this, out var data) ? data.WidthOverride : 0f;
            set => Context.WireVisuals.UpdateVisualData(this, data => data.WidthOverride = value);
        }

        /// <summary>
        /// The opacity multiplier applied to the referenced wire.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when you access this method after you call <see cref="Context.Dispose"/> on the context.</exception>
        /// <remarks>
        /// The opacity multiplier is clamped to the [0, 1] range when set.
        /// Setting a new value overwrites any existing opacity override for that wire.
        /// Setting this property has no effect when the <see cref="WireReference"/> has no associated <see cref="Context"/>, such as when it is <c>default</c>.
        /// Throws <see cref="ObjectDisposedException"/> when you access this method after you call <see cref="Context.Dispose"/> on the context.
        /// </remarks>
        /// <example>
        /// Reduce the opacity of a wire to draw it semi-transparently in the graph canvas, for example to de-emphasize inactive connections.
        /// <code>
        /// WireReference wire = context.GetWireReference(outputPortID, inputPortID);
        /// wire.Opacity = 0.5f;
        /// </code>
        /// </example>
        public float Opacity
        {
            get => Context.WireVisuals.TryGet(this, out var data) ? data.Opacity : 1f;
            set => Context.WireVisuals.UpdateVisualData(this, data => data.Opacity = Mathf.Clamp01(value));
        }

        /// <summary>
        /// Clears all customization previously applied to the referenced wire.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when you access this method after you call <see cref="Context.Dispose"/> on the context.</exception>
        /// <remarks>
        /// If the wire does not correspond to any connection in the graph, any stored visual data for that wire is removed and the call has no further effect.
        /// Throws <see cref="ObjectDisposedException"/> when you access this method after you call <see cref="Context.Dispose"/> on the context.
        /// </remarks>
        /// <example>
        /// Remove every visual override previously applied to a wire and restore the default drawing in the graph canvas.
        /// <code>
        /// WireReference wire = context.GetWireReference(outputPortID, inputPortID);
        /// wire.ClearCustomization();
        /// </code>
        /// </example>
        public void ClearCustomization()
        {
            Context.WireVisuals.Clear(this);
        }

        /// <summary>
        /// Indicates whether the current <see cref="WireReference"/> is equal to another <see cref="WireReference"/>.
        /// </summary>
        /// <param name="other">The other <see cref="WireReference"/> to compare with the current instance.</param>
        /// <returns>true if both references share the same <see cref="OutputPortID"/>, <see cref="InputPortID"/>, and refer to the same <see cref="Context"/> instance; otherwise, false.</returns>
        public bool Equals(WireReference other)
        {
            return OutputPortID == other.OutputPortID && InputPortID == other.InputPortID && Context == other.Context;
        }

        /// <summary>
        /// Indicates whether the current <see cref="WireReference"/> is equal to another object.
        /// </summary>
        /// <param name="obj">The object to compare with the current <see cref="WireReference"/>.</param>
        /// <returns>true if <paramref name="obj"/> is a <see cref="WireReference"/> equal to the current instance; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is WireReference other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code for the current <see cref="WireReference"/>.
        /// </summary>
        /// <returns>A hash code derived from the <see cref="OutputPortID"/>, <see cref="InputPortID"/>, and <see cref="Context"/> of the current reference.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(OutputPortID, InputPortID, Context);
        }

        /// <summary>
        /// Compares two <see cref="WireReference"/> values for equality.
        /// </summary>
        /// <param name="left">The first <see cref="WireReference"/> to compare.</param>
        /// <param name="right">The second <see cref="WireReference"/> to compare.</param>
        /// <returns>true if <paramref name="left"/> is equal to <paramref name="right"/>. Otherwise, false.</returns>
        public static bool operator ==(WireReference left, WireReference right) => left.Equals(right);

        /// <summary>
        /// Compares two <see cref="WireReference"/> values for inequality.
        /// </summary>
        /// <param name="left">The first <see cref="WireReference"/> to compare.</param>
        /// <param name="right">The second <see cref="WireReference"/> to compare.</param>
        /// <returns>true if <paramref name="left"/> isn't equal to <paramref name="right"/>. Otherwise, false.</returns>
        public static bool operator !=(WireReference left, WireReference right) => !left.Equals(right);
    }
}
