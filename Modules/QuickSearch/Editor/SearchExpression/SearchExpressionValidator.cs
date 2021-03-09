// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Search
{
    static class SearchExpressionValidator
    {
        public class Signature
        {
            public class Argument
            {
                public SearchExpressionType types;
                public bool variadic { get; }
                public bool optional { get; }

                public Argument(SearchExpressionType types, bool variadic = false, bool optional = false)
                {
                    this.types = types;
                    this.variadic = variadic;
                    this.optional = optional || types.HasFlag(SearchExpressionType.Optional);
                }
            }

            public SearchExpressionType[] argTypes;
            public List<Argument> arguments = new List<Argument>();
            public int mandatoryArgumentNumber;
            public int argumentCount => arguments.Count;

            public Signature()
            {
            }

            public Signature(params SearchExpressionType[] argTypes)
            {
                foreach (var arg in argTypes)
                    AddArgument(arg);
            }

            public Signature AddArgument(Argument arg)
            {
                if (!arg.optional)
                    ++mandatoryArgumentNumber;
                arguments.Add(arg);
                return this;
            }

            public Signature AddArgument(SearchExpressionType types, bool variadic, bool optional)
            {
                return AddArgument(new Argument(types, variadic, optional));
            }

            public Signature AddArgument(SearchExpressionType types)
            {
                return AddArgument(types, types.HasFlag(SearchExpressionType.Variadic), types.HasFlag(SearchExpressionType.Optional));
            }

            public override string ToString()
            {
                return string.Join(", ", arguments.Select(a => a.types));
            }
        }

        public static bool ValidateSignature(string name, Signature signature, ref string errorMsg)
        {
            // Single variadic + Variadic must be last argument
            for (var i = 0; i < signature.argumentCount; ++i)
            {
                if (signature.arguments[i].variadic && i != signature.argumentCount - 1)
                {
                    errorMsg = $"{name}: arg #{i} is a variadic and is not the last argument.";
                    return false;
                }
            }

            // Optional must be last(s) argument(s)
            var isOptional = false;
            for (var i = 0; i < signature.argumentCount; ++i)
            {
                if (isOptional)
                {
                    if (!signature.arguments[i].optional)
                    {
                        errorMsg = $"{name}: arg #{i} is not optional after an optional argument.";
                        return false;
                    }
                }
                else if (signature.arguments[i].optional)
                {
                    isOptional = true;
                }
            }

            // Check that each param is an actual value:
            for (var i = 0; i < signature.argumentCount; ++i)
            {
                if (!signature.arguments[i].types.HasAny(SearchExpressionType.AnyExpression))
                {
                    errorMsg = $"{name}: arg #{i} is not a valid argument: {signature.arguments[i].types}.";
                    return false;
                }
            }

            return true;
        }

        public static void ValidateExpressionArguments(SearchExpressionContext c, IEnumerable<Signature> signatures)
        {
            // First pass to get all valid argument number signatures (must do a ToList to separate the 2 passes)
            // Second pass to validate the argument types. The last error is kept (lowest number of arguments if no signature matches the number of argument, wrong type if there is at least one)
            var lastError = "";
            var errorPosition = StringView.Null;
            if (signatures.Where(s => ValidateExpressionArgumentsCount(c.expression.evaluator.name, c.args, s, (msg, errorPos) => { lastError = msg; errorPosition = errorPos; })).ToList()
                .Any(s => ValidateExpressionArguments(c.expression.evaluator.name, c.args, s, (msg, errorPos) => { lastError = msg; errorPosition = errorPos; })))
                return;

            if (!errorPosition.valid)
                errorPosition = c.expression.innerText;
            c.ThrowError($"Error while evaluating arguments for {c.expression.evaluator.name}. {lastError}", errorPosition);
        }

        public static void ValidateExpressionArguments(SearchExpressionEvaluator evaluator, SearchExpression[] args, IEnumerable<Signature> signatures, StringView expressionInnerText)
        {
            // First pass to get all valid argument number signatures (must do a ToList to separate the 2 passes)
            // Second pass to validate the argument types. The last error is kept (lowest number of arguments if no signature matches the number of argument, wrong type if there is at least one)
            var lastError = "";
            var errorPosition = StringView.Null;
            if (signatures.Where(s => ValidateExpressionArgumentsCount(evaluator.name, args, s, (msg, errorPos) => { lastError = msg; errorPosition = errorPos; })).ToList()
                .Any(s => ValidateExpressionArguments(evaluator.name, args, s, (msg, errorPos) => { lastError = msg; errorPosition = errorPos; })))
                return;

            if (!errorPosition.valid)
                errorPosition = expressionInnerText;
            throw new SearchExpressionParseException($"Error while validating signature with arguments for {evaluator.name}. {lastError}", errorPosition.startIndex, errorPosition.Length);
        }

        public static bool ValidateExpressionArgumentsCount(string name, SearchExpression[] args, Signature signature, Action<string, StringView> errorHandler)
        {
            var actualArgsCount = args.Length;
            var expectedArgsCount = signature.argumentCount;
            var mandatoryArgumentNumber = signature.mandatoryArgumentNumber;
            if (actualArgsCount == 0 && mandatoryArgumentNumber != 0)
            {
                errorHandler($"{name} takes a minimum of {mandatoryArgumentNumber} arguments and was passed: {0}.", StringView.Null);
                return false;
            }
            if (actualArgsCount > expectedArgsCount && !signature.arguments.Last().variadic)
            {
                errorHandler($"{name} takes a maximum of {expectedArgsCount} was passed: {actualArgsCount}.", StringView.Null);
                return false;
            }
            if (actualArgsCount < mandatoryArgumentNumber)
            {
                errorHandler($"{name} takes a minimum of {mandatoryArgumentNumber} arguments and was passed: {actualArgsCount}.", StringView.Null);
                return false;
            }
            return true;
        }

        public static bool ValidateExpressionArguments(string name, SearchExpression[] args, Signature signature, Action<string, StringView> errorHandler)
        {
            for (var i = 0; i < args.Length; ++i)
            {
                var expectedArgIndex = Math.Min(i, signature.argumentCount - 1);
                if (!args[i].types.HasAny(signature.arguments[expectedArgIndex].types))
                {
                    errorHandler($"{name} Argument #{i} expects: [{signature.arguments[expectedArgIndex].types}] got passed [{args[i].types}] (\"{args[i].innerText}\")", args[i].outerText);
                    return false;
                }
            }

            return true;
        }
    }
}
