// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace UnityEditor.Search
{
    static partial class Evaluators
    {
        // random{...set}
        [Description("Randomly pick a result for each expression."), Category("Set Manipulation")]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable | SearchExpressionType.Variadic)]
        public static IEnumerable<SearchItem> Random(SearchExpressionContext c)
        {
            if (c.args.Length == 0)
                c.ThrowError("No arguments");

            foreach (var e in c.args)
            {
                if (e != null)
                    foreach (var r in Random(c, e))
                        yield return r;
                else
                    yield return null;
            }
        }

        static IEnumerable<SearchItem> Random(SearchExpressionContext c, SearchExpression e)
        {
            var set = new List<SearchItem>();
            foreach (var item in e.Execute(c))
            {
                if (item != null)
                    set.Add(item);
                yield return null; // Wait until we have all results.
            }

            var randomItem = Random(set);
            // Rename random item label if an alias is defined.
            if (c.ResolveAlias(e) is string alias)
                randomItem.label = alias;
            yield return randomItem;
        }

        static SearchItem Random(IList<SearchItem> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            var r = new Random();
            return list.Count == 0 ? null : list[r.Next(0, list.Count)];
        }
    }
}
