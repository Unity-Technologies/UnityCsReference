// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Unity.GraphToolsFoundation
{
    /// <summary>
    /// The type of a parsed operation.
    /// </summary>
    enum OperationType
    {
        Add,
        Sub,
        Mul,
        Div,
        LeftParens,
        Plus,
        Minus,
        Coma,
        Mod
    }

    /// <summary>
    /// The parsed expression.
    /// </summary>
    interface IExpression
    {
    }

    interface IOperation_Internal : IExpression
    {
    }

    interface IValue_Internal : IExpression
    {
    }

    enum Associativity_Internal
    {
        None,
        Left,
        Right
    }

    /// <summary>
    /// Tokens for parsing the mathematical expression.
    /// </summary>
    [Flags]
    enum Token
    {
        None = 0,
        Op = 1,
        Number = 2,
        Identifier = 4,
        LeftParens = 8,
        RightParens = 16,
        Coma = 32,
    }

    /// <summary>
    /// Utility class to parse a mathematical expression.
    /// </summary>
    static class MathExpressionParser
    {
        /// <summary>
        /// Parses a mathematical expression.
        /// </summary>
        /// <param name="expressionStr">The expression in string to be parsed.</param>
        /// <param name="error">The error message if an exception occured.</param>
        /// <returns>An IExpression that contains the parsed expression.</returns>
        public static IExpression Parse(string expressionStr, out string error)
        {
            var output = new Stack<IExpression>();
            var opStack = new Stack<Operator_Internal>();

            var r = new MathExpressionReader_Internal(expressionStr);

            try
            {
                r.ReadToken();
                error = null;
                return ParseUntil(r, opStack, output, Token.None, 0);
            }
            catch (Exception e)
            {
                error = e.Message;
                return null;
            }
        }

        internal static readonly Dictionary<OperationType, Operator_Internal> Ops_Internal = new Dictionary<OperationType, Operator_Internal>
        {
            {OperationType.Add, new Operator_Internal(OperationType.Add, "+", 2, Associativity_Internal.Left)},
            {OperationType.Sub, new Operator_Internal(OperationType.Sub, "-", 2, Associativity_Internal.Left)},

            {OperationType.Mul, new Operator_Internal(OperationType.Mul, "*", 3, Associativity_Internal.Left)},
            {OperationType.Div, new Operator_Internal(OperationType.Div, "/", 3, Associativity_Internal.Left)},
            {OperationType.Mod, new Operator_Internal(OperationType.Mod, "%", 3, Associativity_Internal.Left)},

            {OperationType.LeftParens, new Operator_Internal(OperationType.LeftParens, "(", 5)},
            {OperationType.Minus, new Operator_Internal(OperationType.Minus, "-", 2000, Associativity_Internal.Right, unary: true)},
        };

        static Operator_Internal ReadOperator(string input, bool unary)
        {
            return Ops_Internal.Single(o => o.Value.Str == input && o.Value.Unary == unary).Value;
        }

        static IExpression ParseUntil(MathExpressionReader_Internal r, Stack<Operator_Internal> opStack, Stack<IExpression> output, Token readUntilToken,
            int startOpStackSize)
        {
            do
            {
                switch (r.CurrentTokenType)
                {
                    case Token.LeftParens:
                    {
                        opStack.Push(Ops_Internal[OperationType.LeftParens]);
                        r.ReadToken();
                        var arg = ParseUntil(r, opStack, output, Token.Coma | Token.RightParens,
                            opStack.Count);
                        if (r.CurrentTokenType == Token.Coma)
                            throw new InvalidDataException("Tuples not supported");
                        if (r.CurrentTokenType != Token.RightParens)
                            throw new InvalidDataException("Mismatched parens, missing a closing parens");
                        output.Push(arg);

                        while (opStack.TryPeek(out var stackOp) && stackOp.Type != OperationType.LeftParens)
                        {
                            opStack.Pop();
                            PopOpOperandsAndPushNode(stackOp);
                        }

                        if (opStack.TryPeek(out var leftParens) && leftParens.Type == OperationType.LeftParens)
                            opStack.Pop();
                        else
                            throw new InvalidDataException("Mismatched parens");
                        r.ReadToken();
                        break;
                    }
                    case Token.RightParens:
                        throw new InvalidDataException("Mismatched parens");
                    case Token.Op:
                    {
                        var unary = r.PrevTokenType == Token.Op ||
                            r.PrevTokenType == Token.LeftParens ||
                            r.PrevTokenType == Token.None;
                        var readBinOp = ReadOperator(r.CurrentToken, unary);

                        while (opStack.TryPeek(out var stackOp) &&
                               // the operator at the top of the operator stack is not a left parenthesis or coma
                               stackOp.Type != OperationType.LeftParens && stackOp.Type != OperationType.Coma &&
                               // there is an operator at the top of the operator stack with greater precedence
                               (stackOp.Precedence > readBinOp.Precedence ||
                                // or the operator at the top of the operator stack has equal precedence and the token is left associative
                                stackOp.Precedence == readBinOp.Precedence &&
                                readBinOp.Associativity == Associativity_Internal.Left))
                        {
                            opStack.Pop();
                            PopOpOperandsAndPushNode(stackOp);
                        }

                        opStack.Push(readBinOp);
                        r.ReadToken();
                        break;
                    }
                    case Token.Number:
                        output.Push(new ExpressionValue(float.Parse(r.CurrentToken, CultureInfo.InvariantCulture)));
                        r.ReadToken();
                        break;
                    case Token.Identifier:
                        var id = r.CurrentToken;
                        r.ReadToken();
                        if (r.CurrentTokenType != Token.LeftParens) // variable
                        {
                            output.Push(new Variable(id));
                            break;
                        }
                        else // function call
                        {
                            r.ReadToken(); // skip (
                            var args = new List<IExpression>();

                            while (true)
                            {
                                var arg = ParseUntil(r, opStack, output, Token.Coma | Token.RightParens,
                                    opStack.Count);
                                args.Add(arg);
                                if (r.CurrentTokenType == Token.RightParens)
                                {
                                    break;
                                }
                                r.ReadToken();
                            }

                            r.ReadToken(); // skip )

                            output.Push(new FunctionCall(id.ToLower(), args));
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException(r.CurrentTokenType.ToString());
                }
            }
            while (!readUntilToken.HasFlag(r.CurrentTokenType));

            while (opStack.Count > startOpStackSize)
            {
                var readBinOp = opStack.Pop();
                if (readBinOp.Type == OperationType.LeftParens)
                    break;
                PopOpOperandsAndPushNode(readBinOp);
            }

            return output.Pop();

            void PopOpOperandsAndPushNode(Operator_Internal readBinOp)
            {
                var b = output.Pop();
                if (readBinOp.Unary)
                {
                    output.Push(new UnaryOperation(readBinOp.Type, b));
                }
                else
                {
                    if (output.Count == 0)
                        throw new InvalidDataException($"Missing operand for the {readBinOp.Str} operator in the expression");
                    var a = output.Pop();
                    output.Push(new BinaryOperation(readBinOp.Type, a, b));
                }
            }
        }
    }
}
