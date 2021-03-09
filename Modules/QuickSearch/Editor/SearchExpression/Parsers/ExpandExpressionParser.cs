// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Search
{
    static partial class Parsers
    {
        [SearchExpressionParser("expand", BuiltinParserPriority.Expand)]
        internal static SearchExpression ExpandParser(SearchExpressionParserArgs args)
        {
            var outerText = args.text;
            var text = ParserUtils.SimplifyExpression(outerText);
            if (!text.StartsWith("...", System.StringComparison.Ordinal))
                return null;

            var expression = ParserManager.Parse(args.With(text.Substring(3)));
            return new SearchExpression(expression, expression.types | SearchExpressionType.Expandable, outerText, expression.innerText);
        }
    }
}
