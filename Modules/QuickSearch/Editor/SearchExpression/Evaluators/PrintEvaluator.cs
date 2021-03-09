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
        [SearchExpressionEvaluator(SearchExpressionType.Iterable | SearchExpressionType.Variadic)]
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
    }
}
