// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;

namespace UnityEditor.Search
{
    static partial class Evaluators
    {
        // sort{set, @selector[, #sortAscending]}
        [SearchExpressionEvaluator(SearchExpressionType.Iterable, SearchExpressionType.Selector, SearchExpressionType.Keyword | SearchExpressionType.Number | SearchExpressionType.Optional)]
        [Description("Sort expression results based on a criteria."), Category("Transformers")]
        public static IEnumerable<SearchItem> Sort(SearchExpressionContext c)
        {
            var dataSet = c.args[0];

            bool sortAscend = true;
            if (c.args.Length == 3)
            {
                var sortAscendExpr = c.args[2];
                if (sortAscendExpr.IsKeyword(SearchExpressionKeyword.desc))
                    sortAscend = false;
                else
                    sortAscend = sortAscendExpr.GetBooleanValue(sortAscend);
            }
            var sortedSet = new SortedSet<SearchItem>(new SortItemComparer(c.args[1].innerText.ToString(), sortAscend));
            var selectExpression = dataSet.Apply(nameof(Select), c.args[1]);
            foreach (var r in selectExpression.Execute(c))
            {
                if (r != null)
                    sortedSet.Add(r);
                else
                    yield return null;
            }

            int score = 0;
            foreach (var r in sortedSet)
            {
                r.score = score++;
                yield return r;
            }
        }

        readonly struct SortItemComparer : IComparer<SearchItem>
        {
            public readonly string sortBy;
            public readonly bool sortAscend;

            public SortItemComparer(string sortBy, bool sortAscend)
            {
                this.sortBy = sortBy;
                this.sortAscend = sortAscend;
            }

            public int Compare(SearchItem x, SearchItem y)
            {
                return DefaultComparer(x.GetValue(sortBy), y.GetValue(sortBy)) * (sortAscend ? 1 : -1);
            }
        }
    }
}
