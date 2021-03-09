// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text.RegularExpressions;

namespace UnityEditor.Search
{
    static partial class Parsers
    {
        readonly static Regex s_AliasWithQuotesRx = new Regex(@"\sas\s+[""'](?<alias>[^""']+)[""']$");
        readonly static Regex s_AliasWithoutQuotesRx = new Regex(@"\sas\s+(?<alias>[^\s{}\[\]'""]+)$");

        [SearchExpressionParser("alias", BuiltinParserPriority.Alias)]
        internal static SearchExpression AliasParser(SearchExpressionParserArgs args)
        {
            // Make sure the expression contains at least the keyword `as`
            if (!args.text.Contains("as"))
                return null;

            var m = s_AliasWithQuotesRx.Match(args.text.ToString());
            if (!m.Success)
                m = s_AliasWithoutQuotesRx.Match(args.text.ToString());

            if (!m.Success || m.Groups.Count != 2)
                return null;

            var aliasGroup = m.Groups["alias"];
            var alias = args.text.Substring(aliasGroup.Index, aliasGroup.Length).Trim();
            var aliasExpression = ParserManager.Parse(args.With(alias).With(SearchExpressionParserFlags.ImplicitLiterals));

            var exprText = args.text.Substring(0, m.Index);
            var expression = ParserManager.Parse(args.With(exprText));
            var isDynamicAlias = !aliasExpression.types.HasFlag(SearchExpressionType.Text) || alias.Contains('@') || alias.Contains('$');

            if (isDynamicAlias && !string.Equals(expression.name, "alias", System.StringComparison.OrdinalIgnoreCase))
                return expression.Apply("alias", aliasExpression);

            return new SearchExpression(expression.types, args.text, expression.innerText, alias, expression.evaluator, expression.parameters);
        }
    }
}
