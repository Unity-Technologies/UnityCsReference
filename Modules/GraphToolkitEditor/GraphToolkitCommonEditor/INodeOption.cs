// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for a node option.
    /// </summary>
    /// <remarks>
    /// Node options are typed properties that appear under the node header and in the inspector when a node is selected.
    /// Unlike ports, they cannot receive values from wires. They allow the tool developers to change the behavior or topology of the node at edit time.
    /// For example, you can use node options to change the number of available ports on a node.
    /// <br/>
    /// <br/>
    /// Each option has a unique <see cref="Name"/> (which must be unique per node for identification), a <see cref="DisplayName"/> (for the UI),
    /// and a <see cref="DataType"/> that defines the type of its value. Use <see cref="TryGetValue{T}"/> to retrieve the option's value.
    /// </remarks>
    public interface INodeOption
    {
        /// <summary>
        /// The data type of the node option.
        /// </summary>
        Type DataType { get; }

        /// <summary>
        /// The unique identifier of the node option.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The display name of the node option shown in the UI.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Tries to retrieve the value of the node option using the specified type.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="value">The variable to assign the value to, if retrieval succeeds.</param>
        /// <returns><c>true</c> if the option exists and the type matches; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// If the value was never explicitly set, this method still returns <c>true</c>, and <paramref name="value"/> will contain the default
        /// value for type <typeparamref name="T"/>.
        /// </remarks>
        bool TryGetValue<T>(out T value);
    }
}
