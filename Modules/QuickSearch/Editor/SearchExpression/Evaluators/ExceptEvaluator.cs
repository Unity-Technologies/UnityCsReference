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
        // except{set, ...sets}
        [Description("Produces the set difference of two sequences."), Category("Set Manipulation")]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable, SearchExpressionType.Iterable | SearchExpressionType.Variadic)]
        public static IEnumerable<SearchItem> Except(SearchExpressionContext c)
        {
            if (c.args.Length < 2)
                c.ThrowError("Invalid arguments");

            var setExpr = c.args[0];
            if (!setExpr.types.IsIterable())
                c.ThrowError("Primary set is not iterable", setExpr.outerText);

            var exceptSet = new HashSet<SearchItem>();
            foreach (var r in setExpr.Execute(c))
            {
                if (r == null || !exceptSet.Add(r))
                    yield return null;
            }

            // Remove from primary set all items from subsequent sets.
            foreach (var e in c.args.Skip(1))
            {
                if (e != null)
                {
                    foreach (var r in e.Execute(c))
                    {
                        if (r != null)
                            exceptSet.Remove(r);
                        yield return null;
                    }
                }
                else
                    yield return null;
            }

            // Return excepted set.
            foreach (var r in exceptSet)
                yield return r;
        }
    }
}
