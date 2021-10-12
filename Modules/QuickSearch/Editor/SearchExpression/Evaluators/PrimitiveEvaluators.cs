// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace UnityEditor.Search
{
    static partial class Evaluators
    {
        [Description("Return the first result for each expression."), Category("Primitives")]
        [SearchExpressionEvaluator(SearchExpressionType.Function | SearchExpressionType.Literal)]
        public static IEnumerable<SearchItem> Constant(SearchExpressionContext c)
        {
            if (c.expression.types.HasAny(SearchExpressionType.Function))
            {
                using (c.runtime.Push(c.args[0], c.args.Skip(1)))
                    yield return Constant(c.runtime.current).First();
            }
            else if (c.expression.types.HasAny(SearchExpressionType.Number))
                yield return EvaluatorUtils.CreateItem(c.expression.GetNumberValue(), c.expression.alias.ToString());
            else if (c.expression.types.HasAny(SearchExpressionType.Text | SearchExpressionType.Keyword))
                yield return EvaluatorUtils.CreateItem(c.expression.innerText.ToString(), c.expression.alias.ToString());
            else if (c.expression.types.HasAny(SearchExpressionType.Boolean))
                yield return EvaluatorUtils.CreateItem(c.expression.GetBooleanValue(), c.expression.alias.ToString());
            else
                c.ThrowError($"Invalid constant expression");
        }

        [Description("Returns a set of elements from any expression."), Category("Primitives")]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable | SearchExpressionType.Variadic)]
        public static IEnumerable<SearchItem> Set(SearchExpressionContext c)
        {
            return c.args.SelectMany(e => e.Execute(c));
        }

        [Description("Convert the text of any expression to a literal string."), Category("Primitives")]
        [SearchExpressionEvaluator(SearchExpressionEvaluationHints.DoNotValidateSignature)]
        public static IEnumerable<SearchItem> Text(SearchExpressionContext c)
        {
            if (c.args.Length == 0)
                c.ThrowError("Text needs 1 argument");

            return c.args.Select(e => EvaluatorUtils.CreateItem(e.outerText.ToString()));
        }

        [SearchExpressionEvaluator(SearchExpressionType.Iterable | SearchExpressionType.Variadic)]
        [SearchExpressionEvaluatorSignatureOverload(SearchExpressionType.Number, SearchExpressionType.Iterable | SearchExpressionType.Variadic)]
        [Description("Return the first result for each expression."), Category("Primitives")]
        public static IEnumerable<SearchItem> First(SearchExpressionContext c)
        {
            var argIndex = 0;
            var takeNumber = 1;
            if (c.args[0].types.HasFlag(SearchExpressionType.Number))
            {
                ++argIndex;
                takeNumber = Math.Max((int)(c.args[0].GetNumberValue(takeNumber)), 0);
            }

            var taken = 0;
            return c.ForEachArgument((context, e) =>
            {
                taken = 0;
                if (takeNumber == 0)
                    context.Break();
            }, argIndex).ForEachResult((context, item) =>
                {
                    if (taken >= takeNumber)
                    {
                        context.Break();
                        return null;
                    }

                    ++taken;
                    return item;
                });
        }

        [SearchExpressionEvaluator(SearchExpressionType.Iterable | SearchExpressionType.Variadic)]
        [SearchExpressionEvaluatorSignatureOverload(SearchExpressionType.Number, SearchExpressionType.Iterable | SearchExpressionType.Variadic)]
        [Description("Return the last result for each expression."), Category("Primitives")]
        public static IEnumerable<SearchItem> Last(SearchExpressionContext c)
        {
            var argIndex = 0;
            var takeNumber = 1;
            if (c.args[0].types.HasFlag(SearchExpressionType.Number))
            {
                ++argIndex;
                takeNumber = Math.Max((int)(c.args[0].GetNumberValue(takeNumber)), 0);
            }

            var queue = new Queue<SearchItem>(takeNumber);
            return c.ForEachArgument((context, e) =>
            {
                queue.Clear();
                if (takeNumber == 0)
                    context.Break();
            }, argIndex).AggregateResults(queue, (context, item, aggregator) =>
                {
                    if (queue.Count == takeNumber)
                        queue.Dequeue();
                    queue.Enqueue(item);
                });
        }
    }
}
