// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Search
{
    static partial class Parsers
    {
        public static readonly SearchExpressionEvaluator ConstantEvaluator = EvaluatorManager.GetConstantEvaluatorByName("constant");

        [SearchExpressionParser("bool", BuiltinParserPriority.Bool)]
        internal static SearchExpression BooleanParser(StringView text)
        {
            var trimmedText = ParserUtils.SimplifyExpression(text);
            if (!bool.TryParse(trimmedText.ToString(), out _))
                return null;
            return new SearchExpression(SearchExpressionType.Boolean, text, trimmedText, ConstantEvaluator);
        }

        [SearchExpressionParser("number", BuiltinParserPriority.Number)]
        internal static SearchExpression NumberParser(StringView outerText)
        {
            var trimmedText = ParserUtils.SimplifyExpression(outerText);
            if (!Utils.TryParse(trimmedText.ToString(), out double _))
                return null;

            SearchExpressionType types = SearchExpressionType.Number;
            if (trimmedText == "0" || trimmedText == "1")
                types |= SearchExpressionType.Boolean;
            return new SearchExpression(types, outerText, trimmedText, ConstantEvaluator);
        }

        [SearchExpressionParser("implicit_string", BuiltinParserPriority.ImplicitStringLiteral)]
        internal static SearchExpression ImplicitStringParser(SearchExpressionParserArgs args)
        {
            if (!args.HasOption(SearchExpressionParserFlags.ImplicitLiterals))
                return null;

            var text = args.text;
            if (text.Length > 2)
            {
                if (ParserUtils.IsQuote(text[0]))
                {
                    if (text[0] == text[text.Length - 1])
                        return null;
                }
                if (ParserUtils.IsOpener(text[0]) && ParserUtils.IsCloser(text[text.Length - 1]))
                    return null;
            }

            return new SearchExpression(SearchExpressionType.Text, text, text, ConstantEvaluator);
        }

        [SearchExpressionParser("string", BuiltinParserPriority.String)]
        internal static SearchExpression ExplicitStringParser(SearchExpressionParserArgs args)
        {
            var outerText = args.text;
            var text = ParserUtils.SimplifyExpression(outerText);
            if (text.Length < 2 || !ParserUtils.HasQuotes(text))
                return null;

            // Check for any string, since enclosed strings are not allowed, if we find a string token that means there are multiple strings in the text
            for (int i = 1; i < text.Length - 2; ++i)
                if (ParserUtils.IsQuote(text[i]))
                    return null;
            return new SearchExpression(SearchExpressionType.Text, outerText, text.Substring(1, text.Length - 2), ConstantEvaluator);
        }

        [SearchExpressionParser("keyword", BuiltinParserPriority.Keyword)]
        internal static SearchExpression KeywordParser(SearchExpressionParserArgs args)
        {
            if (args.text.Equals(nameof(SearchExpressionKeyword.asc), System.StringComparison.OrdinalIgnoreCase) ||
                args.text.Equals(nameof(SearchExpressionKeyword.desc), System.StringComparison.OrdinalIgnoreCase))
            {
                return new SearchExpression(SearchExpressionType.Keyword, args.text, ConstantEvaluator);
            }

            return null;
        }
    }
}
