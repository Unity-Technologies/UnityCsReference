// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System;
using System.ComponentModel;

namespace UnityEditor.Search
{
    static partial class Evaluators
    {
        static readonly Comparer DefaultComparer = Comparer.DefaultInvariant;

        public static IEnumerable<SearchItem> Compare(SearchExpressionContext c, Func<object, object, bool> comparer)
        {
            if (c.args.Length < 2 || c.args.Length > 3)
                c.ThrowError($"Invalid arguments");

            if (c.args.Length == 3 && !IsSelectorLiteral(c.args[1]))
                c.ThrowError($"Invalid selector");

            var setExpr = c.args[0];
            if (!setExpr.types.IsIterable())
                c.ThrowError("Primary set is not iterable", setExpr.outerText);

            string valueSelector = null;
            var compareExprIndex = 1;
            if (c.args.Length == 3)
            {
                valueSelector = c.args[1].innerText.ToString();
                compareExprIndex++;
            }
            var compareExpr = c.args[compareExprIndex];
            var compareValueItr = compareExpr.Execute(c).FirstOrDefault(e => e != null);
            if (compareValueItr == null)
                c.ThrowError("Invalid comparer value", compareExpr.outerText);
            var compareValue = compareValueItr.value;

            foreach (var r in setExpr.Execute(c))
            {
                if (r != null)
                {
                    if (comparer(r.GetValue(valueSelector), compareValue))
                        yield return r;
                }
                else
                    yield return null;
            }
        }

        [Description("Keep search results that have a greater value."), Category("Comparers")]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable, SearchExpressionType.Literal | SearchExpressionType.QueryString)]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable, SearchExpressionType.Selector, SearchExpressionType.Literal | SearchExpressionType.QueryString)]
        static IEnumerable<SearchItem> gt(SearchExpressionContext c) => Compare(c, (a, b) => DefaultComparer.Compare(a, b) > 0);

        [Description("Keep search results that have a greater or equal value."), Category("Comparers")]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable, SearchExpressionType.Literal | SearchExpressionType.QueryString)]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable, SearchExpressionType.Selector, SearchExpressionType.Literal | SearchExpressionType.QueryString)]
        static IEnumerable<SearchItem> gte(SearchExpressionContext c) => Compare(c, (a, b) => DefaultComparer.Compare(a, b) >= 0);

        [Description("Keep search results that have a lower value."), Category("Comparers")]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable, SearchExpressionType.Literal | SearchExpressionType.QueryString)]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable, SearchExpressionType.Selector, SearchExpressionType.Literal | SearchExpressionType.QueryString)]
        static IEnumerable<SearchItem> lw(SearchExpressionContext c) => Compare(c, (a, b) => DefaultComparer.Compare(a, b) < 0);

        [Description("Keep search results that have a lower or equal value."), Category("Comparers")]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable, SearchExpressionType.Literal | SearchExpressionType.QueryString)]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable, SearchExpressionType.Selector, SearchExpressionType.Literal | SearchExpressionType.QueryString)]
        static IEnumerable<SearchItem> lwe(SearchExpressionContext c) => Compare(c, (a, b) => DefaultComparer.Compare(a, b) <= 0);

        [Description("Keep search results that have an equal value."), Category("Comparers")]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable, SearchExpressionType.Literal | SearchExpressionType.QueryString)]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable, SearchExpressionType.Selector, SearchExpressionType.Literal | SearchExpressionType.QueryString)]
        static IEnumerable<SearchItem> eq(SearchExpressionContext c) => Compare(c, (a, b) => DefaultComparer.Compare(a, b) == 0);

        [Description("Keep search results that have a different value."), Category("Comparers")]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable, SearchExpressionType.Literal | SearchExpressionType.QueryString)]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable, SearchExpressionType.Selector, SearchExpressionType.Literal | SearchExpressionType.QueryString)]
        static IEnumerable<SearchItem> neq(SearchExpressionContext c) => Compare(c, (a, b) => DefaultComparer.Compare(a, b) != 0);

        [Description("Exclude search results for which the expression is not valid."), Category("Comparers")]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable, SearchExpressionType.Text | SearchExpressionType.QueryString | SearchExpressionType.Selector)]
        public static IEnumerable<SearchItem> Where(SearchExpressionContext c)
        {
            var setExpr = c.args[0];
            var whereConditionExpr = c.args[1];
            var queryStr = whereConditionExpr.innerText;
            if (whereConditionExpr.types.HasFlag(SearchExpressionType.Selector))
            {
                queryStr = whereConditionExpr.outerText;
            }
            var setResults = setExpr.Execute(c);
            return EvaluatorManager.itemQueryEngine.WhereMainThread(c, setResults, queryStr.ToString());
        }
    }
}
