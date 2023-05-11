// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine
{
    // Evaluates simple expressions.
    // - supports data types: float, int, double, long.
    // - operations: + - * / % ^ ( )
    // - functions: sqrt, sin, cos, tan, floor, ceil, round, L, R
    //     L(a,b) results in a linear ramp between a and b, based on index & count.
    //     R(a,b) results in a random value between a and b.
    //     Both L and R function presence is treated as if the variable was present and an
    //       Instance needs to be evaluated.
    // - variables: 'x', 'v' or 'f' can be treated as input for later evaluation via Instance
    // - expressions starting with +=, -=, *=, /= are treated the same as if they were "x+(...)" etc.
    [MovedFrom(true, "UnityEditor", "UnityEditor")]
    public class ExpressionEvaluator
    {
        internal class Expression
        {
            internal Expression(string expression)
            {
                expression = PreFormatExpression(expression);
                var infixTokens = ExpressionToTokens(expression, out hasVariables);
                infixTokens = FixUnaryOperators(infixTokens);
                rpnTokens = InfixToRPN(infixTokens);
            }

            public bool Evaluate<T>(ref T value, int index = 0, int count = 1)
            {
                return EvaluateTokens(rpnTokens, ref value, index, count);
            }

            internal readonly string[] rpnTokens;
            internal readonly bool hasVariables;
        }

        public static bool Evaluate<T>(string expression, out T value)
        {
            return Evaluate(expression, out value, out _);
        }

        internal static bool Evaluate<T>(string expression, out T value, out Expression delayed)
        {
            value = default;
            delayed = null;
            if (TryParse(expression, out value))
                return true;
            var expr = new Expression(expression);
            if (expr.hasVariables)
            {
                value = default;
                delayed = expr;
                return false;
            }
            return EvaluateTokens(expr.rpnTokens, ref value, 0, 1);
        }

        // Simple PCG (https://www.pcg-random.org/) based random number generator
        struct PcgRandom
        {
            readonly ulong increment;
            ulong state;

            public PcgRandom(ulong state = 0, ulong sequence = 0)
            {
                this.increment = (sequence << 1) | 1u;
                this.state = 0;
                Step();
                this.state += state;
                Step();
            }

            public uint GetUInt()
            {
                var prevState = state;
                Step();
                return XshRr(prevState);
            }

            static uint RotateRight(uint v, int rot) => (v >> rot) | (v << (-rot & 31));
            static uint XshRr(ulong s) => RotateRight((uint)(((s >> 18) ^ s) >> 27), (int)(s >> 59));
            const ulong Multiplier64 = 6364136223846793005ul;
            void Step() { state = unchecked(state * Multiplier64 + increment); }
        }

        static PcgRandom s_Random = new PcgRandom(0);

        static Dictionary<string, Operator> s_Operators = new Dictionary<string, Operator>
        {
            {"-", new Operator(Op.Sub, 2, 2, Associativity.Left)},
            {"+", new Operator(Op.Add, 2, 2, Associativity.Left)},
            {"/", new Operator(Op.Div, 3, 2, Associativity.Left)},
            {"*", new Operator(Op.Mul, 3, 2, Associativity.Left)},
            {"%", new Operator(Op.Mod, 3, 2, Associativity.Left)},
            {"^", new Operator(Op.Pow, 5, 2, Associativity.Right)},
            // unary minus trick. For example we convert 2/-7+(-9*8)*2^-9-5 to 2/_7+(_9*8)*2^_9-5 before evaluation
            {"_", new Operator(Op.Neg, 5, 1, Associativity.Left)},
            {"sqrt", new Operator(Op.Sqrt, 4, 1, Associativity.Left)},
            {"cos", new Operator(Op.Cos, 4, 1, Associativity.Left)},
            {"sin", new Operator(Op.Sin, 4, 1, Associativity.Left)},
            {"tan", new Operator(Op.Tan, 4, 1, Associativity.Left)},
            {"floor", new Operator(Op.Floor, 4, 1, Associativity.Left)},
            {"ceil", new Operator(Op.Ceil, 4, 1, Associativity.Left)},
            {"round", new Operator(Op.Round, 4, 1, Associativity.Left)},
            {"R", new Operator(Op.Rand, 4, 2, Associativity.Left)},
            {"L", new Operator(Op.Linear, 4, 2, Associativity.Left)},
        };

        enum Op
        {
            Add, Sub, Mul, Div, Mod,
            Neg,
            Pow, Sqrt,
            Sin, Cos, Tan,
            Floor, Ceil, Round,
            Rand, Linear
        }

        enum Associativity { Left, Right }

        class Operator
        {
            public readonly Op op;
            public readonly int precedence;
            public readonly Associativity associativity;
            public readonly int inputs;

            public Operator(Op op, int precedence, int inputs, Associativity associativity)
            {
                this.op = op;
                this.precedence = precedence;
                this.inputs = inputs;
                this.associativity = associativity;
            }
        }

        internal static void SetRandomState(uint state)
        {
            s_Random = new PcgRandom(state);
        }

        // Evaluate RPN tokens (https://en.wikipedia.org/wiki/Reverse_Polish_notation)
        static bool EvaluateTokens<T>(string[] tokens, ref T value, int index, int count)
        {
            var res = false;
            if (typeof(T) == typeof(float))
            {
                var v = (double) UnsafeUtility.As<T, float>(ref value);
                res = EvaluateDouble(tokens, ref v, index, count);
                var outValue = (float)v;
                value = UnsafeUtility.As<float, T>(ref outValue);
            }
            else if (typeof(T) == typeof(int))
            {
                var v = (double) UnsafeUtility.As<T, int>(ref value);
                res = EvaluateDouble(tokens, ref v, index, count);
                var outValue = (int)v;
                value = UnsafeUtility.As<int, T>(ref outValue);
            }
            else if (typeof(T) == typeof(long))
            {
                var v = (double) UnsafeUtility.As<T, long>(ref value);
                res = EvaluateDouble(tokens, ref v, index, count);
                var outValue = (long)v;
                value = UnsafeUtility.As<long, T>(ref outValue);
            }
            else if (typeof(T) == typeof(ulong))
            {
                var v = (double) UnsafeUtility.As<T, ulong>(ref value);
                res = EvaluateDouble(tokens, ref v, index, count);
                if (v < 0d)
                {
                    v = 0d;
                }
                var outValue = (ulong)v;
                value = UnsafeUtility.As<ulong, T>(ref outValue);
            }
            else if (typeof(T) == typeof(double))
            {
                var v = UnsafeUtility.As<T, double>(ref value);
                res = EvaluateDouble(tokens, ref v, index, count);
                value = UnsafeUtility.As<double, T>(ref v);
            }
            return res;
        }

        static bool EvaluateDouble(string[] tokens, ref double value, int index, int count)
        {
            var stack = new Stack<string>();

            foreach (var token in tokens)
            {
                if (IsOperator(token))
                {
                    Operator oper = TokenToOperator(token);
                    var values = new List<double>();
                    var parsed = true;

                    while (stack.Count > 0 && !IsCommand(stack.Peek()) && values.Count < oper.inputs)
                    {
                        parsed &= TryParse<double>(stack.Pop(), out var newValue);
                        values.Add(newValue);
                    }

                    values.Reverse();

                    if (parsed && values.Count == oper.inputs)
                        stack.Push(EvaluateOp(values.ToArray(), oper.op, index, count).ToString(CultureInfo.InvariantCulture));
                    else // Can't parse values or too few values for the operator -> exit
                    {
                        return false;
                    }
                }
                else if (IsVariable(token))
                {
                    stack.Push(token == "#" ? index.ToString() : value.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    stack.Push(token);
                }
            }

            if (stack.Count == 1)
            {
                if (TryParse(stack.Pop(), out value))
                    return true;
            }

            return false;
        }

        // Translate tokens from infix into RPN (https://en.wikipedia.org/wiki/Shunting-yard_algorithm)
        static string[] InfixToRPN(string[] tokens)
        {
            var operatorStack = new Stack<string>();
            var outputQueue = new Queue<string>();

            foreach (string token in tokens)
            {
                if (IsCommand(token))
                {
                    char command = token[0];

                    if (command == '(') // Bracket open
                    {
                        operatorStack.Push(token);
                    }
                    else if (command == ')') // Bracket close
                    {
                        while (operatorStack.Count > 0 && operatorStack.Peek() != "(")
                            outputQueue.Enqueue(operatorStack.Pop());
                        if (operatorStack.Count > 0)
                            operatorStack.Pop();
                        if (operatorStack.Count > 0 && IsDelayedFunction(operatorStack.Peek()))
                            outputQueue.Enqueue(operatorStack.Pop());
                    }
                    else if (command == ',') // Function argument separator
                    {
                        while (operatorStack.Count > 0 && operatorStack.Peek() != "(")
                            outputQueue.Enqueue(operatorStack.Pop());
                    }
                    else // All the other operators
                    {
                        Operator o = TokenToOperator(token);

                        while (NeedToPop(operatorStack, o))
                            outputQueue.Enqueue(operatorStack.Pop());

                        operatorStack.Push(token);
                    }
                }
                else if (IsDelayedFunction(token))
                {
                    operatorStack.Push(token);
                }
                else // Not a command, just a regular number
                {
                    outputQueue.Enqueue(token);
                }
            }
            while (operatorStack.Count > 0)
                outputQueue.Enqueue(operatorStack.Pop());

            return outputQueue.ToArray();
        }

        // While there is an operator (topOfStack) at the top of the operators stack and
        // either (newOperator) is left-associative and its precedence is less or equal to that of (topOfStack), or
        // (newOperator) is right-associative and its precedence is less than (topOfStack)
        static bool NeedToPop(Stack<string> operatorStack, Operator newOperator)
        {
            if (operatorStack.Count > 0 && newOperator != null)
            {
                Operator topOfStack = TokenToOperator(operatorStack.Peek());
                if (topOfStack != null)
                {
                    if (newOperator.associativity == Associativity.Left && newOperator.precedence <= topOfStack.precedence ||
                        newOperator.associativity == Associativity.Right && newOperator.precedence < topOfStack.precedence)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // Splits expression to meaningful tokens
        static string[] ExpressionToTokens(string expression, out bool hasVariables)
        {
            hasVariables = false;
            var result = new List<string>();
            var currentString = "";

            foreach (var currentChar in expression)
            {
                if (IsCommand(currentChar.ToString()))
                {
                    if (currentString.Length > 0)
                        result.Add(currentString);

                    result.Add(currentChar.ToString());
                    currentString = "";
                }
                else
                {
                    if (currentChar != ' ')
                        currentString += currentChar;
                    else
                    {
                        if (currentString.Length > 0)
                            result.Add(currentString);
                        currentString = "";
                    }
                }
            }

            if (currentString.Length > 0)
                result.Add(currentString);

            hasVariables = result.Any(f => IsVariable(f) || IsDelayedFunction(f));
            return result.ToArray();
        }

        static bool IsCommand(string token)
        {
            if (token.Length == 1)
            {
                char c = token[0];
                if (c == '(' || c == ')' || c == ',')
                    return true;
            }
            return IsOperator(token);
        }

        static bool IsVariable(string token)
        {
            if (token.Length == 1)
            {
                char c = token[0];
                return c == 'x' || c == 'v' || c == 'f' || c == '#';
            }
            return false;
        }

        static bool IsDelayedFunction(string token)
        {
            var op = TokenToOperator(token);
            if (op != null)
            {
                if (op.op == Op.Rand || op.op == Op.Linear)
                    return true;
            }
            return false;
        }

        static bool IsOperator(string token)
        {
            return s_Operators.ContainsKey(token);
        }

        static Operator TokenToOperator(string token)
        {
            return s_Operators.TryGetValue(token, out var op) ? op : null;
        }

        // Clean up the expression before any parsing
        static string PreFormatExpression(string expression)
        {
            var result = expression;
            result = result.Trim();

            if (result.Length == 0)
                return result;

            var lastChar = result[result.Length - 1];

            // remove trailing operator for niceness (user is middle of typing, and we don't want to evaluate to zero)
            if (IsOperator(lastChar.ToString()))
                result = result.TrimEnd(lastChar);

            // turn +=, -=, *=, /= into variable forms
            if (result.Length >= 2 && result[1] == '=')
            {
                char op = result[0];
                string expr = result.Substring(2);
                if (op == '+') result = $"x+({expr})";
                if (op == '-') result = $"x-({expr})";
                if (op == '*') result = $"x*({expr})";
                if (op == '/') result = $"x/({expr})";
            }

            return result;
        }

        // Turn unary minus into an operator. For example: - ( 1 - 2 ) * - 3 becomes: _ ( 1 - 2 ) * _ 3
        static string[] FixUnaryOperators(string[] tokens)
        {
            if (tokens.Length == 0)
                return tokens;

            if (tokens[0] == "-")
                tokens[0] = "_";

            for (int i = 1; i < tokens.Length - 1; i++)
            {
                string token = tokens[i];
                string previousToken = tokens[i - 1];
                if (token == "-" && IsCommand(previousToken) && previousToken != ")")
                    tokens[i] = "_";
            }
            return tokens;
        }

        static double EvaluateOp(double[] values, Op op, int index, int count)
        {
            var a = values.Length >= 1 ? values[0] : 0;
            var b = values.Length >= 2 ? values[1] : 0;
            switch (op)
            {
                case Op.Neg: return -a;
                case Op.Add: return a + b;
                case Op.Sub: return a - b;
                case Op.Mul: return a * b;
                case Op.Div: return a / b;
                case Op.Mod: return a % b;
                case Op.Pow: return Math.Pow(a, b);
                case Op.Sqrt: return a <= 0 ? 0 : Math.Sqrt(a);
                case Op.Floor: return Math.Floor(a);
                case Op.Ceil: return Math.Ceiling(a);
                case Op.Round: return Math.Round(a);
                case Op.Cos: return Math.Cos(a);
                case Op.Sin: return Math.Sin(a);
                case Op.Tan: return Math.Tan(a);
                case Op.Rand:
                {
                    var r = s_Random.GetUInt() & 0xFFFFFF;
                    var f = r / (double)0xFFFFFF;
                    return a + f * (b - a);
                }
                case Op.Linear:
                {
                    if (count < 1)
                        count = 1;
                    var f = count < 2 ? 0.5 : index / (double)(count - 1);
                    return a + f * (b - a);
                }
            }
            return 0;
        }

        static bool TryParse<T>(string expression, out T result)
        {
            expression = expression.Replace(',', '.'); // any actual reason for that? CultureInfo.InvariantCulture.NumberFormat used below
            var expressionLowerCase = expression.ToLowerInvariant();
            if (expressionLowerCase.Length > 1 && Char.IsDigit(expressionLowerCase[expressionLowerCase.Length-2]))
            {
                char[] numberDesignator = {'f','d','l'};
                expressionLowerCase = expressionLowerCase.TrimEnd(numberDesignator);
            }

            bool success = false;
            result = default;
            if (expressionLowerCase.Length == 0)
                return true;

            if (typeof(T) == typeof(float))
            {
                if (expressionLowerCase == "pi")
                {
                    success = true;
                    result = (T)(object)(float)Math.PI;
                }
                else
                {
                    success = float.TryParse(expressionLowerCase, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out var temp);
                    result = (T)(object)temp;
                }
            }
            else if (typeof(T) == typeof(int))
            {
                success = int.TryParse(expressionLowerCase, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var temp);
                result = (T)(object)temp;
            }
            else if (typeof(T) == typeof(double))
            {
                if (expressionLowerCase == "pi")
                {
                    success = true;
                    result = (T)(object)Math.PI;
                }
                else
                {
                    success = double.TryParse(expressionLowerCase, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out var temp);
                    result = (T)(object)temp;
                }
            }
            else if (typeof(T) == typeof(long))
            {
                success = long.TryParse(expressionLowerCase, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var temp);
                result = (T)(object)temp;
            }
            else if (typeof(T) == typeof(ulong))
            {
                success = ulong.TryParse(expressionLowerCase, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var temp);
                result = (T)(object)temp;
            }
            return success;
        }
    }
}
