// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation
{
    /// <summary>
    /// Holds information on a parsed unary operation.
    /// </summary>
    readonly struct UnaryOperation : IOperation_Internal
    {
        public readonly OperationType Type;
        public readonly IExpression Operand;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnaryOperation"/> class.
        /// </summary>
        /// <param name="type">Type of the unary operation.</param>
        /// <param name="operand">Operand of the unary operation.</param>
        public UnaryOperation(OperationType type, IExpression operand)
        {
            Type = type;
            Operand = operand;
        }

        /// <summary>
        /// Returns a string that represents the parsed unary operation.
        /// </summary>
        /// <returns>A string that represents the parsed unary operation.</returns>
        public override string ToString() => $"{MathExpressionParser.Ops_Internal[Type].Str}{Operand}";
    }
}
