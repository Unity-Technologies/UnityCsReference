// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.ComponentModel;

namespace UnityEditor.Search
{
    static partial class Evaluators
    {
        static Regex QueryVariableRx = new Regex(@"([\$\@])([\#\w][\w\d\.\\/]*)");
        [Description("Returns a Search Query from a string"), Category("Primitives")]
        [SearchExpressionEvaluator(SearchExpressionEvaluationHints.ExpandSupported | SearchExpressionEvaluationHints.DoNotValidateSignature)]
        public static IEnumerable<SearchItem> Query(SearchExpressionContext c)
        {
            if (c.expression.types.HasFlag(SearchExpressionType.Function))
            {
                using (c.runtime.Push(c.args[0], c.args.Skip(1)))
                    return Query(c.runtime.current);
            }

            // Resolve variables
            var queryText = UnEscapeExpressions(c.expression);
            if (c.items.Count > 0 && (queryText.Contains('$') || queryText.Contains('@')))
            {
                foreach (Match m in QueryVariableRx.Matches(queryText))
                {
                    for (int i = 2; i < m.Groups.Count; i++)
                        queryText = ResolveVariable(queryText, m.Groups[1].Value, m.Groups[i].Value, c);
                }
            }

            // Spread nested expressions
            if (c.args?.Length > 0)
                return SpreadExpression(queryText, c);

            return RunQuery(c, queryText);
        }

        public static IEnumerable<SearchItem> RunQuery(SearchExpressionContext c, string queryText)
        {
            using (var context = new SearchContext(c.search.GetProviders(), queryText, c.search.options | SearchFlags.QueryString))
            using (var results = SearchService.Request(context))
                foreach (var r in results)
                    yield return r;
        }

        readonly struct SpreadContext
        {
            public readonly string query;
            public readonly string alias;

            public SpreadContext(string query, string alias = null)
            {
                this.query = query;
                this.alias = alias;
            }
        }

        private static IEnumerable<SearchItem> SpreadExpression(string query, SearchExpressionContext c)
        {
            var spreaded = new List<SpreadContext>();
            var toSpread = new List<SpreadContext>() { new SpreadContext(query.ToString(), c.ResolveAlias()) };
            foreach (var e in c.args)
            {
                spreaded = new List<SpreadContext>();
                foreach (var r in e.Execute(c))
                {
                    if (r != null)
                    {
                        foreach (var q in toSpread)
                        {
                            if (r.value == null)
                                continue;
                            var replacement = r.value is UnityEngine.Object ? TaskEvaluatorManager.EvaluateMainThread(() => r.value.ToString()): r.value.ToString();
                            if (replacement.LastIndexOf(' ') != -1)
                                replacement = '"' + replacement + '"';
                            var pattern = @"[\[\{]?" + Regex.Escape(e.outerText.ToString()) + @"[\}\]]?";
                            spreaded.Add(new SpreadContext(Regex.Replace(q.query, pattern, replacement), alias: c.ResolveAlias(e, replacement)));
                        }
                    }
                    else
                        yield return null;
                }

                toSpread = spreaded;
            }

            foreach (var s in spreaded)
            {
                if (c.flags.HasFlag(SearchExpressionExecutionFlags.Expand))
                {
                    yield return SearchExpression.CreateSearchExpressionItem(s.query, s.alias);
                }
                else
                {
                    foreach (var r in RunQuery(c, s.query))
                        yield return r;
                }
            }
        }

        static string ResolveVariable(string query, string token, string varName, SearchExpressionContext c)
        {
            var v = SelectorManager.SelectValue(c, varName, out var _);
            if (v == null)
                c.ThrowError($"Cannot resolve variable {token}{varName}");

            return query.Replace(token + varName, v.ToString());
        }

        static string UnEscapeExpressions(SearchExpression expression)
        {
            if (!expression.hasEscapedNestedExpressions)
                return expression.innerText.ToString();

            return ParserUtils.UnEscapeExpressions(expression.innerText);
        }
    }
}
