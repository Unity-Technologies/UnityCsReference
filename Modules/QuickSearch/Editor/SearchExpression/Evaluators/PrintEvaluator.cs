// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.ComponentModel;

namespace UnityEditor.Search
{
    static partial class Evaluators
    {
        [Description("Print expression results in the console."), Category("Utilities")]
        [SearchExpressionEvaluator(SearchExpressionType.Selector | SearchExpressionType.Text, SearchExpressionType.Iterable | SearchExpressionType.Variadic)]
        [SearchExpressionEvaluatorSignatureOverload(SearchExpressionType.Iterable | SearchExpressionType.Variadic)]
        public static IEnumerable<SearchItem> Print(SearchExpressionContext c)
        {
            var skipCount = 0;
            if (EvaluatorUtils.GetFormatString(c.args[0], out var formatStr))
                skipCount++;

            var outputValueFieldName = System.Guid.NewGuid().ToString("N");
            foreach (var expr in c.args.Skip(skipCount))
            {
                if (expr == null)
                {
                    yield return null;
                    continue;
                }

                var str = new List<string>();
                var dataSet = EvaluatorUtils.ProcessValues(expr.Execute(c), outputValueFieldName, item =>
                {
                    var valueStr = EvaluatorUtils.FormatItem(c.search, item, formatStr);
                    str.Add(valueStr);
                    return valueStr;
                });
                foreach (var item in dataSet)
                    yield return item;

                Debug.Log($"[{string.Join(",", str)}]");
            }
        }

        struct RangeDouble
        {
            public double? start;
            public double? end;
            public bool valid => start.HasValue && end.HasValue;
        }

        [Description("Generate a range of expressions."), Category("Utilities")]
        [SearchExpressionEvaluator(SearchExpressionType.AnyValue, SearchExpressionType.AnyValue | SearchExpressionType.Optional)]
        public static IEnumerable<SearchItem> Range(SearchExpressionContext c)
        {
            var range = new RangeDouble();
            var alias = c.ResolveAlias("Range");

            foreach (var sr in c.args[0].Execute(c))
            {
                if (GetRange(sr, ref range))
                    break;
                else
                    yield return null;
            }

            if (!range.valid)
            {
                if (c.args.Length < 2)
                    c.ThrowError("No expression to end range");
                foreach (var sr in c.args[1].Execute(c))
                {
                    if (GetRange(sr, ref range))
                        break;
                    else
                        yield return null;
                }
            }

            if (!range.valid)
                c.ThrowError("Incomplete range");

            for (double d = range.start.Value; d < range.end.Value; d += 1d)
                yield return EvaluatorUtils.CreateItem(d, alias);
        }

        static bool GetRange(SearchItem item, ref RangeDouble range)
        {
            if (item == null)
                return false;
            if (!range.start.HasValue)
            {
                if (Utils.TryGetNumber(item.value, out var rs))
                    range.start = rs;
            }
            else if (!range.end.HasValue)
            {
                if (Utils.TryGetNumber(item.value, out var re))
                    range.end = re;
            }

            return range.valid;
        }
    }
}
