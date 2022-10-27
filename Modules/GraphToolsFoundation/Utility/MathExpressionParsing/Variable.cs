// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation
{
    /// <summary>
    /// Holds information on a parsed variable.
    /// </summary>
    readonly struct Variable : IValue_Internal
    {
        public readonly string Id;

        /// <summary>
        /// Initializes a new instance of the <see cref="Variable"/> class.
        /// </summary>
        /// <param name="id">The name of the variable.</param>
        public Variable(string id)
        {
            Id = id;
        }

        /// <summary>
        /// Returns a string that represents the parsed variable.
        /// </summary>
        /// <returns>A string that represents the parsed variable.</returns>
        public override string ToString() => $"${Id}";
    }
}
