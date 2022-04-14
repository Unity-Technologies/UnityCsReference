// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;

namespace UnityEditor.Search
{
    static partial class Parsers
    {
        static readonly SearchExpressionEvaluator SetEvaluator = EvaluatorManager.GetConstantEvaluatorByName("set");

        [SearchExpressionParser("fixedset", BuiltinParserPriority.Set)]
        internal static SearchExpression FixedSetParser(SearchExpressionParserArgs args)
        {
            var outerText = args.text;
            var text = ParserUtils.SimplifyExpression(outerText);
            if (text.length < 2 || text[0] != '[' || text[text.length - 1] != ']')
                return null;
            var expressions = ParserUtils.GetExpressionsStartAndLength(text, out _);
            if (expressions.Length != 1 || expressions[0].startIndex != text.startIndex || expressions[0].length != text.length)
                return null;

            var parameters = ParserUtils.ExtractArguments(text)
                .Select(paramText => ParserManager.Parse(args.With(paramText).With(SearchExpressionParserFlags.ImplicitLiterals)))
                .ToArray();

            var innerText = ParserUtils.SimplifyExpression(text.Substring(1, text.length - 2));
            return new SearchExpression(SearchExpressionType.Set, outerText, innerText, SetEvaluator, parameters);
        }

        [SearchExpressionParser("set", BuiltinParserPriority.Set)]
        internal static SearchExpression ExpressionSetParser(SearchExpressionParserArgs args)
        {
            var outerText = args.text;
            if (outerText.length < 2)
                return null;
            var innerText = ParserUtils.SimplifyExpression(outerText, false);
            if (outerText.length == innerText.length)
                return null;
            var text = outerText.Substring(innerText.startIndex - outerText.startIndex - 1, innerText.length + 2);
            if (text[0] != '{' || text[text.length - 1] != '}')
                return null;

            var expressions = ParserUtils.GetExpressionsStartAndLength(innerText, out var rootHasParameters);
            if (!rootHasParameters)
                return null;

            var parameters = ParserUtils.ExtractArguments(text)
                .Select(paramText => ParserManager.Parse(args.With(paramText).Without(SearchExpressionParserFlags.ImplicitLiterals)))
                .ToArray();

            return new SearchExpression(SearchExpressionType.Set, outerText, innerText.Trim(), SetEvaluator, parameters);
        }
    }
}
