// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Identifies a port inside a visualization <see cref="Context"/> so visualization changes can be applied to it.
/// </summary>
/// <remarks>
/// Obtain a <see cref="PortReference"/> from <see cref="Context.GetPortReference"/>, providing the unique identifier of the port.
/// The reference is only meaningful for the <see cref="Context"/> that produced it.
/// You can use its methods to set, retrieve, or clear the port preview for that specific port in the graph canvas.
/// Port preview is a visualization feature that displays a label next to a port in the graph canvas. This label provides additional information about the port's value or state.
/// Two <see cref="PortReference"/> values are equal when they share the same <see cref="PortID"/> and refer to the same <see cref="Context"/> instance.
/// </remarks>
/// <example>
/// <code>
/// using var context = Registry.CreateVisualizationContext(graph.ID);
/// context.GetPortReference(portID).SetPreview("42.0");
/// if (context.GetPortReference(portID).TryGetPreview(out var displayValue))
///     Debug.Log(displayValue);
/// context.GetPortReference(portID).ClearPreview();
/// </code>
/// </example>
/// <seealso cref="Context"/>
/// <seealso cref="Context.GetPortReference(Hash128)"/>
public readonly struct PortReference : IEquatable<PortReference>
{
    /// <summary>
    /// Unique identifier of the port the reference points to.
    /// </summary>
    public Hash128 PortID { get; }

    /// <summary>
    /// Visualization context that produced the current port reference.
    /// </summary>
    public Context Context { get; }

    internal PortReference(Context context, Hash128 portID)
    {
        PortID = portID;
        Context = context;
    }

    /// <summary>
    /// Sets the string value to display in a port preview for a given port.
    /// </summary>
    /// <param name="displayValue">String to display in the port preview.</param>
    /// <exception cref="ObjectDisposedException">Thrown when you access this method after you call <see cref="Context.Dispose"/> on the context.</exception>
    /// <remarks>
    /// The string is displayed with default styling in the port preview next to the port in the graph.
    /// Setting a new value overwrites any existing value for that port preview.
    /// Setting a null value clears the port preview for the given port, hiding it from the graph canvas until a new value is set for that port.
    /// This is equivalent to calling <see cref="ClearPreview()"/> for the given port.
    /// If a graph canvas is attached to the <see cref="Context"/> and the port ID does not correspond to any port in the graph, any stored preview data for that port is removed and the call has no further effect.
    /// If no graph canvas is attached yet, the value is stored and applied when a graph canvas becomes available.
    /// Throws <see cref="ObjectDisposedException"/> when you access this method after you call <see cref="Context.Dispose"/> on the context.
    /// </remarks>
    /// <example>
    /// <code>
    /// context.GetPortReference(portID).SetPreview("42.0");
    /// </code>
    /// </example>
    public void SetPreview(string displayValue)
    {
        Context.PortPreview.Set(PortID, displayValue);
    }

    /// <summary>
    /// Retrieves the current string value assigned to the port preview for a given port.
    /// </summary>
    /// <param name="displayValue">When this method returns true, contains the string value of the port preview; otherwise, <see langword="null"/>.</param>
    /// <returns>true if the port preview value was successfully retrieved; otherwise, false.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when you access this method after you call <see cref="Context.Dispose"/> on the context.</exception>
    /// <remarks>
    /// If no port preview is currently set for the given port, this method returns false and <paramref name="displayValue"/> is <see langword="null"/>.
    /// Call <see cref="SetPreview(string)"/> to assign a display value to a port preview before querying it.
    /// Throws <see cref="ObjectDisposedException"/> when you access this method after you call <see cref="Context.Dispose"/> on the context.
    /// </remarks>
    /// <example>
    /// <code>
    /// if (context.GetPortReference(portID).TryGetPreview(out var displayValue))
    ///     Debug.Log(displayValue);
    /// </code>
    /// </example>
    public bool TryGetPreview(out string displayValue)
    {
        return Context.PortPreview.TryGet(PortID, out displayValue);
    }

    /// <summary>
    /// Clears the port preview for the specified port.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when you access this method after you call <see cref="Context.Dispose"/> on the context.</exception>
    /// <remarks>
    /// The port preview is hidden from the graph canvas until a new value is set using <see cref="SetPreview(string)"/>.
    /// If the provided port ID does not correspond to any port in the graph, any stored preview data for that port is removed and the call has no further effect.
    /// Throws <see cref="ObjectDisposedException"/> when you access this method after you call <see cref="Context.Dispose"/> on the context.
    /// </remarks>
    /// <example>
    /// <code>
    /// context.GetPortReference(portID).ClearPreview();
    /// </code>
    /// </example>
    public void ClearPreview()
    {
        Context.PortPreview.Clear(PortID);
    }

    /// <summary>
    /// Indicates whether the current <see cref="PortReference"/> is equal to another <see cref="PortReference"/>.
    /// </summary>
    /// <param name="other">The other <see cref="PortReference"/> to compare with the current instance.</param>
    /// <returns>Returns true when both references share the same <see cref="PortID"/> and refer to the same <see cref="Context"/> instance. Otherwise, returns false.</returns>
    public bool Equals(PortReference other)
    {
        return PortID == other.PortID && ReferenceEquals(Context, other.Context);
    }

    /// <summary>
    /// Indicates whether the current <see cref="PortReference"/> is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with the current <see cref="PortReference"/>.</param>
    /// <returns>Returns true when <paramref name="obj"/> is a <see cref="PortReference"/> equal to the current instance. Otherwise, returns false.</returns>
    public override bool Equals(object obj)
    {
        return obj is PortReference other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for the current <see cref="PortReference"/>.
    /// </summary>
    /// <returns>A hash code derived from the <see cref="PortID"/> and <see cref="Context"/> of the current reference.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(PortID, Context);
    }

    /// <summary>
    /// Compares two <see cref="PortReference"/> values for equality.
    /// </summary>
    /// <param name="left">The first <see cref="PortReference"/> to compare.</param>
    /// <param name="right">The second <see cref="PortReference"/> to compare.</param>
    /// <returns>Returns true when <paramref name="left"/> is equal to <paramref name="right"/>. Otherwise, returns false.</returns>
    public static bool operator ==(PortReference left, PortReference right) => left.Equals(right);

    /// <summary>
    /// Compares two <see cref="PortReference"/> values for inequality.
    /// </summary>
    /// <param name="left">The first <see cref="PortReference"/> to compare.</param>
    /// <param name="right">The second <see cref="PortReference"/> to compare.</param>
    /// <returns>Returns true when <paramref name="left"/> is not equal to <paramref name="right"/>. Otherwise, returns false.</returns>
    public static bool operator !=(PortReference left, PortReference right) => !left.Equals(right);
}
