// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace UnityEditor.Search
{
    static partial class Evaluators
    {
        [Description("Returns a selector from the text of an expression"), Category("Primitives")]
        [SearchExpressionEvaluator(SearchExpressionType.Literal | SearchExpressionType.Selector)]
        public static IEnumerable<SearchItem> Selector(SearchExpressionContext c)
        {
            if (!c.expression.types.IsText())
                c.ThrowError($"Invalid selector");

            yield return EvaluatorUtils.CreateItem(c.ResolveAlias("Selector"), c.expression.innerText, c.expression.innerText.ToString());
        }

        [SearchExpressionEvaluator(SearchExpressionEvaluationHints.DoNotValidateSignature)]
        [Description("Create new results by selecting which value and property to take."), Category("Transformers")]
        public static IEnumerable<SearchItem> Select(SearchExpressionContext c)
        {
            if (c.args.Length < 2)
                c.ThrowError($"Invalid arguments");

            // Select dataset
            var dataset = c.args[0].Execute(c);
            const int batchSize = 100;
            foreach (var batch in dataset.Batch(batchSize))
            {
                var results = batch;
                var sIt = c.args.Skip(1).GetEnumerator();
                while (sIt.MoveNext())
                {
                    var selector = sIt.Current;
                    if (IsSelectorLiteral(selector))
                    {
                        var selectorName = selector.innerText.ToString();
                        var selectorAlias = c.ResolveAlias(selector);
                        results = TaskEvaluatorManager.EvaluateMainThread(results, item =>
                        {
                            var selectedValue = SelectorManager.SelectValue(item, c.search, selectorName, out string suggestedSelectorName);
                            AddSelectedValue(item, selector.innerText.ToString(), selectorAlias ?? suggestedSelectorName, selectedValue);
                            return item;
                        }, batchSize);
                    }
                    else
                    {
                        results = ProcessIterableSelector(c, results, selector);
                    }
                }

                foreach (var r in results)
                    yield return r;
            }
        }

        static IEnumerable<SearchItem> ProcessIterableSelector(SearchExpressionContext c, IEnumerable<SearchItem> results, SearchExpression selector)
        {
            var selectorName = c.ResolveAlias(selector, selector.name);
            foreach (var r in results)
            {
                if (r == null)
                {
                    yield return null;
                    continue;
                }

                using (c.runtime.Push(r))
                {
                    foreach (var sv in selector.Execute(c))
                    {
                        if (sv == null)
                            yield return null;
                        else
                        {
                            AddSelectedValue(r, selectorName, null, sv.value);
                            yield return r;
                            break;
                        }
                    }
                }
            }
        }

        private static void AddSelectedValue(SearchItem item, string name, string alias, object value)
        {
            item.SetField(name, alias, value);
            if (value == null)
                return;
            item.value = value;
            if (item.label == null)
                item.label = value.ToString();
            else if (item.description == null)
                item.description = value.ToString();
        }

        public static bool IsSelectorLiteral(SearchExpression selector)
        {
            if (selector.types.HasAny(SearchExpressionType.Text | SearchExpressionType.Selector))
                return true;
            if (selector.types.HasFlag(SearchExpressionType.QueryString) && selector.parameters.Length == 0)
                return true;
            return false;
        }
    }
}
