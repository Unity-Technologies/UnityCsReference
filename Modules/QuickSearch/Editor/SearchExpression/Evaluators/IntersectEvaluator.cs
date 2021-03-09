// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;

namespace UnityEditor.Search
{
    static partial class Evaluators
    {
        // intersect{...sets[, @intersectBy]}
        [Description("Produces the set intersection of two sequences."), Category("Set Manipulation")]
        [SearchExpressionEvaluator(/*SearchExpressionType.Iterable | SearchExpressionType.Variadic, SearchExpressionType.Selector | SearchExpressionType.Optional*/)]
        public static IEnumerable<SearchItem> Intersect(SearchExpressionContext c)
        {
            if (c.args.Length < 2)
                c.ThrowError("Invalid arguments");

            var setExpr = c.args[0];
            if (!setExpr.types.IsIterable())
                c.ThrowError("Primary set is not iterable", setExpr.outerText);

            string selector = null;
            if (c.args[c.args.Length - 1].types.HasFlag(SearchExpressionType.Selector))
                selector = c.args[c.args.Length - 1].innerText.ToString();

            var set = new HashSet<SearchItem>(new ItemComparer(selector));
            foreach (var r in setExpr.Execute(c))
            {
                if (r == null || !set.Add(r))
                    yield return null;
            }

            // Remove from primary set all items from subsequent sets that do not intersect
            foreach (var e in c.args.Skip(1))
            {
                if (e != null && !e.types.HasFlag(SearchExpressionType.Selector))
                {
                    var intersectWith = new HashSet<SearchItem>();
                    foreach (var r in e.Execute(c))
                    {
                        if (r == null || !intersectWith.Add(r))
                            yield return null;
                    }

                    set.IntersectWith(intersectWith);
                }
                else
                    yield return null;
            }

            foreach (var r in set)
                yield return r;
        }

        readonly struct ItemComparer : IEqualityComparer<SearchItem>
        {
            public readonly string selector;

            public ItemComparer(string selector) => this.selector = selector;
            public bool Equals(SearchItem x, SearchItem y) => DefaultComparer.Compare(GetValueToCompare(x), GetValueToCompare(y)) == 0;
            public int GetHashCode(SearchItem obj) => GetValueToCompare(obj).GetHashCode();

            public object GetValueToCompare(SearchItem obj)
            {
                var value = obj.GetValue(selector);
                if (value != null)
                    return value;
                return obj.id;
            }
        }
    }
}
