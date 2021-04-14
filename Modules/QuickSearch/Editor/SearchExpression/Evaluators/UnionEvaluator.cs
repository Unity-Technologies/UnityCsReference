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
        [SearchExpressionEvaluator(SearchExpressionType.AnyExpression | SearchExpressionType.Variadic)]
        [Description("Produces the unique set union of multiple expression."), Category("Set Manipulation")]
        public static IEnumerable<SearchItem> Union(SearchExpressionContext c)
        {
            if (c.args == null || c.args.Length == 0)
                c.ThrowError("Nothing to merge");

            var set = new HashSet<int>();
            foreach (var e in c.args)
            {
                foreach (var item in e.Execute(c))
                {
                    if (item == null)
                        yield return null;
                    else if (set.Add(item.value.GetHashCode()))
                        yield return item;
                }
            }
        }

        [SearchExpressionEvaluator(SearchExpressionType.AnyExpression | SearchExpressionType.Variadic)]
        [Description("Produces the unique set union of multiple expression."), Category("Set Manipulation")]
        public static IEnumerable<SearchItem> Distinct(SearchExpressionContext c)
        {
            return Union(c);
        }
    }
}
