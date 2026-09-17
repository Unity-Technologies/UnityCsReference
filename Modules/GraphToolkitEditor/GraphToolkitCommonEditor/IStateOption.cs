// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for a state option.
    /// </summary>
    /// <remarks>
    /// State options are typed properties that appear in the graph inspector when a state is selected. They allow the tool
    /// developers to change the behavior of the state at edit time. Unlike the options of a <see cref="Node"/>, they are
    /// never drawn on the state in the state machine canvas.
    /// <br/>
    /// <br/>
    /// Each option has a unique <see cref="Name"/> (which must be unique per state for identification), a <see cref="DisplayName"/> (for the UI),
    /// and a <see cref="DataType"/> that defines the type of its value. Use <see cref="TryGetValue{T}"/> to retrieve the option's value.
    ///
    /// See also:
    ///
    ///- <see cref="State.OnDefineOptions"/> for how to declare the options of a state
    ///- <see cref="IStateOptionBuilder"/> for how to configure an option as it is declared
    ///
    /// Unity implements this interface. Do not implement it in your own types.
    /// </remarks>
    public interface IStateOption
    {
        /// <summary>
        /// The data type of the state option.
        /// </summary>
        Type DataType { get; }

        /// <summary>
        /// The unique identifier of the state option.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The display name of the state option shown in the UI.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// The text displayed when hovering over the state option's display name.
        /// </summary>
        string Tooltip { get; }

        /// <summary>
        /// Tries to retrieve the value of the state option using the specified type.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="value">The variable to assign the value to, if retrieval succeeds.</param>
        /// <returns><c>true</c> if the option exists and the type matches; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// If the value was never explicitly set, this method still returns <c>true</c>, and <paramref name="value"/> will contain the default
        /// value for type <typeparamref name="T"/>.
        /// </remarks>
        bool TryGetValue<T>(out T value);

        /// <summary>
        /// Sets a new value for the state option.
        /// </summary>
        /// <typeparam name="T">The type of the value to set that matches the option's data type.</typeparam>
        /// <param name="value">The value to assign to the option’s UI field.</param>
        /// <returns><c>true</c> if the value was successfully set; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method allows for editor-time modification of a state option's value.
        /// It performs a type check and conversion internally. If the value cannot be cast to <see cref="DataType"/>, the method returns <c>false</c>.
        /// This method triggers the <see cref="State.DefineState"/> method to update the state.
        /// </remarks>
        bool TrySetValue<T>(T value);
    }
}
