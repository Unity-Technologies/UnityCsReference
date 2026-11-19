// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for a specialized node that outputs a fixed value of a specific data type.
    /// </summary>
    /// <remarks>
    /// Use constant nodes to represent a static, predefined value in the graph. This value remains unchanged and is typically used to feed constant input into computations.
    /// To retrieve the value, use <see cref="TryGetValue{T}"/>. This method is type-safe and provides access to the node’s value if the type matches.
    /// The <see cref="DataType"/> property identifies the constant's type.
    /// </remarks>
    public interface IConstantNode : INode
    {
        /// <summary>
        /// The data type of the constant node's value.
        /// </summary>
        /// <remarks>
        /// The type returned by this property indicates the kind of value the constant node holds,
        /// such as <c>float</c>, <c>int</c>, <c>string</c>, or a custom type.
        /// </remarks>
        public Type DataType { get; }

        /// <summary>
        /// Retrieves the value of the constant node as the specified type.
        /// </summary>
        /// <typeparam name="T">The type to retrieve the value as.</typeparam>
        /// <param name="value">The output parameter that holds the value if the conversion is successful.</param>
        /// <returns><c>true</c> if the value was successfully retrieved and cast to <typeparamref name="T"/>; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method provides type-safe access to the constant's stored value. It performs a type check
        /// and conversion internally. If the value cannot be cast to <typeparamref name="T"/>, the method returns <c>false</c>
        /// and <paramref name="value"/> is set to the default value of <typeparamref name="T"/>.
        /// </remarks>
        public bool TryGetValue<T>(out T value);

        /// <summary>
        /// Sets the value of the constant node to the specified value of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the value to set.</typeparam>
        /// <param name="value">The value to set.</param>
        /// <returns><c>true</c> if the value was successfully set; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method provides type-safe access to the constant's stored value. It performs a type check
        /// and conversion internally. If the value cannot be cast to <see cref="DataType"/>, the method returns <c>false</c>.
        /// </remarks>
        public bool TrySetValue<T>(T value);
    }
}
