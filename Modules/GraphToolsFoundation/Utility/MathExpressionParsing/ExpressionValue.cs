// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;

namespace Unity.GraphToolsFoundation
{
    /// <summary>
    /// Holds information on a parsed float value.
    /// </summary>
    readonly struct ExpressionValue : IValue_Internal
    {
        public readonly float Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionValue"/> class.
        /// </summary>
        /// <param name="value">The float value of the ExpressionValue.</param>
        public ExpressionValue(float value)
        {
            Value = value;
        }

        /// <summary>
        /// Returns a string that represents the parsed float value.
        /// </summary>
        /// <returns>A string that represents the parsed float value.</returns>
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }
}
