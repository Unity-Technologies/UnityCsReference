// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolsFoundation
{
    /// <summary>
    /// Holds information on a parsed function call.
    /// </summary>
    readonly struct FunctionCall : IOperation_Internal
    {
        public readonly string Id;
        public readonly List<IExpression> Arguments;

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionCall"/> class.
        /// </summary>
        /// <param name="id">The name of the function call.</param>
        /// <param name="arguments">The arguments provided to the function call.</param>
        public FunctionCall(string id, List<IExpression> arguments)
        {
            Id = id;
            Arguments = arguments;
        }

        /// <summary>
        /// Returns a string that represents the parsed function call.
        /// </summary>
        /// <returns>A string that represents the parsed function call.</returns>
        public override string ToString() => $"#{Id}({string.Join(", ", Arguments)})";
    }
}
