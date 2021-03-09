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
        [Description("Count the number of results in an expression."), Category("Math")]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable | SearchExpressionType.Variadic)]
        public static IEnumerable<SearchItem> Count(SearchExpressionContext c)
        {
            foreach (var arg in c.args)
                yield return EvaluatorUtils.CreateItem(arg.Execute(c).Count(), c.ResolveAlias(arg, "Count"));
        }

        [Description("Find the minimal value for each expression."), Category("Math")]
        [SearchExpressionEvaluator(SearchExpressionType.Selector, SearchExpressionType.Iterable | SearchExpressionType.Variadic)]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable | SearchExpressionType.Variadic)]
        public static IEnumerable<SearchItem> Min(SearchExpressionContext c)
        {
            var skipCount = 0;
            string selector = null;
            if (c.args[0].types.HasFlag(SearchExpressionType.Selector))
            {
                skipCount++;
                selector = c.args[0].innerText.ToString();
            }

            return c.ForEachArgument(skipCount).AggregateResults(double.MaxValue,
                (context, item, min) => Aggregate(item, selector, min, (d, _min) => d < _min),
                (context, min) => EvaluatorUtils.CreateItem(min, context.ResolveAlias("Min")));
        }

        [Description("Find the maximum value for each expression."), Category("Math")]
        [SearchExpressionEvaluator(SearchExpressionType.Selector, SearchExpressionType.Iterable | SearchExpressionType.Variadic)]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable | SearchExpressionType.Variadic)]
        public static IEnumerable<SearchItem> Max(SearchExpressionContext c)
        {
            var skipCount = 0;
            string selector = null;
            if (c.args[0].types.HasFlag(SearchExpressionType.Selector))
            {
                skipCount++;
                selector = c.args[0].innerText.ToString();
            }
            return c.ForEachArgument(skipCount).AggregateResults(double.MinValue,
                (context, item, max) => Aggregate(item, selector, max, (d, _max) => d > _max),
                (context, max) => EvaluatorUtils.CreateItem(max, context.ResolveAlias("Max")));
        }

        [Description("Find the average value for each expression."), Category("Math")]
        [SearchExpressionEvaluator(SearchExpressionType.Selector, SearchExpressionType.Iterable | SearchExpressionType.Variadic)]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable | SearchExpressionType.Variadic)]
        public static IEnumerable<SearchItem> Avg(SearchExpressionContext c)
        {
            var skipCount = 0;
            string selector = null;
            if (c.args[0].types.HasFlag(SearchExpressionType.Selector))
            {
                skipCount++;
                selector = c.args[0].innerText.ToString();
            }
            return c.ForEachArgument(skipCount).AggregateResults(Average.Zero,
                (context, item, avg) => Aggregate(item, selector, avg, (d, _avg) => _avg.Add(d)),
                (context, avg) => EvaluatorUtils.CreateItem(avg.result, context.ResolveAlias("Average")));
        }

        [Description("Compute the sum value for each expression."), Category("Math")]
        [SearchExpressionEvaluator(SearchExpressionType.Selector, SearchExpressionType.Iterable | SearchExpressionType.Variadic)]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable | SearchExpressionType.Variadic)]
        public static IEnumerable<SearchItem> Sum(SearchExpressionContext c)
        {
            var skipCount = 0;
            string selector = null;
            if (c.args[0].types.HasFlag(SearchExpressionType.Selector))
            {
                skipCount++;
                selector = c.args[0].innerText.ToString();
            }

            return c.ForEachArgument(skipCount).AggregateResults(0d,
                (context, item, sum) => Aggregate(item, selector, sum, (d, _sum) => _sum + d),
                (context, sum) => EvaluatorUtils.CreateItem(sum, context.ResolveAlias("Sum")));
        }

        static double Aggregate(SearchItem item, string selector, double agg, Func<double, double, bool> comparer) => Aggregate(item, selector, agg, comparer, (d, _) => d);
        static T Aggregate<T>(SearchItem item, string selector, T agg, Func<double, T, T> aggregator) where T : struct => Aggregate(item, selector, agg, (d, v) => true, aggregator);
        static T Aggregate<T>(SearchItem item, string selector, T agg, Func<double, T, bool> comparer, Func<double, T, T> aggregator) where T : struct
        {
            if (EvaluatorUtils.TryConvertToDouble(item, out var d, selector) && comparer(d, agg))
                return aggregator(d, agg);
            return agg;
        }

        struct Average
        {
            public int count;
            public double sum;
            public double result => count != 0 ? sum / count : 0d;

            public static Average Zero => new Average();

            public Average Add(double d)
            {
                ++count;
                sum += d;
                return this;
            }
        }
    }
}
