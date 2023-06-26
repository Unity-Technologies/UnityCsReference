// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.Search
{
    static partial class Parsers
    {
        [SearchExpressionParser("named", BuiltinParserPriority.Named)]
        internal static SearchExpression NamedParser(SearchExpressionParserArgs args)
        {
            var text = ParserUtils.SimplifyExpression(args.text);
            if (text.IsNullOrEmpty())
                return null;

            var match = ParserUtils.namedExpressionStartRegex.Match(text.ToString());
            if (!match.Success || match.Index != 0 || match.Groups["name"].Length == 0)
                return null;

            var expressionsStartAndLength = ParserUtils.GetExpressionsStartAndLength(text, out _, out _);
            if (expressionsStartAndLength.Length != 1)
                return null;

            var expressionName = match.Groups["name"].Value;
            if ((expressionName.Length + expressionsStartAndLength[0].length) != text.length)
                return null;

            var evaluator = EvaluatorManager.GetEvaluatorByNameDuringParsing(expressionName, text.Substring(0, expressionName.Length));
            var parametersText = text.Substring(expressionName.Length, text.length - expressionName.Length);
            var parametersPositions = ParserUtils.ExtractArguments(parametersText, expressionName);
            var parameters = new List<SearchExpression>();

            var argsWith = SearchExpressionParserFlags.None;
            var argsWithout = SearchExpressionParserFlags.ImplicitLiterals;
            ApplyEvaluatorHints(evaluator.hints, ref argsWith, ref argsWithout);
            foreach (var paramText in parametersPositions)
            {
                parameters.Add(ParserManager.Parse(args.With(paramText, argsWith).Without(argsWithout)));
            }

            if (!evaluator.hints.HasFlag(SearchExpressionEvaluationHints.DoNotValidateSignature) &&
                args.HasOption(SearchExpressionParserFlags.ValidateSignature))
            {
                var signatures = EvaluatorManager.GetSignaturesByName(expressionName);
                if (signatures != null)
                    SearchExpressionValidator.ValidateExpressionArguments(evaluator, parameters.ToArray(), signatures, text);
            }

            var expressionText = ParserUtils.SimplifyExpression(expressionsStartAndLength[0].Substring(1, expressionsStartAndLength[0].length - 2));
            return new SearchExpression(SearchExpressionType.Function, args.text, expressionText, evaluator, parameters.ToArray());
        }

        static void ApplyEvaluatorHints(SearchExpressionEvaluationHints hints, ref SearchExpressionParserFlags with, ref SearchExpressionParserFlags without)
        {
            if (hints.HasFlag(SearchExpressionEvaluationHints.ImplicitArgsLiterals))
            {
                with |= SearchExpressionParserFlags.ImplicitLiterals;
                without &= ~SearchExpressionParserFlags.ImplicitLiterals;
            }
            if (hints.HasFlag(SearchExpressionEvaluationHints.DoNotValidateArgsSignature))
            {
                without |= SearchExpressionParserFlags.ValidateSignature;
            }
        }
    }
}
