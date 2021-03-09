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

        public static IEnumerable<SearchItem> Select(SearchExpressionContext c, bool append)
        {
            if (c.args.Length < 2)
                c.ThrowError($"Invalid arguments");

            // Select dataset
            var dataset = c.args[0].Execute(c);
            var results = append ? dataset : dataset.Select(r =>
            {
                if (r == null)
                    return r;
                return SearchExpressionProvider.CreateItem(c.search, r.id, r.score, null, null, r.thumbnail, r);
            });

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
                        var selectedValue = SelectorManager.SelectValue(append ? item : (SearchItem)item.data,
                            c.search, selectorName, out string suggestedSelectorName);
                        if (selectedValue != null)
                            AddSelectedValue(item, selectorAlias ?? suggestedSelectorName, selectedValue, append);
                        return item;
                    });
                }
                else
                {
                    results = ProcessIterableSelector(c, results, selector, append);
                }
            }

            return results;
        }

        static IEnumerable<SearchItem> ProcessIterableSelector(SearchExpressionContext c, IEnumerable<SearchItem> results, SearchExpression selector, bool append)
        {
            var selectorName = c.ResolveAlias(selector, selector.name);
            foreach (var r in results)
            {
                if (r == null)
                {
                    yield return null;
                    continue;
                }

                using (c.runtime.Push(append ? r : (SearchItem)r.data))
                {
                    foreach (var sv in selector.Execute(c))
                    {
                        if (sv == null)
                            yield return null;
                        else
                        {
                            AddSelectedValue(r, selectorName, sv.value, append);
                            yield return r;
                            break;
                        }
                    }
                }
            }
        }

        private static void AddSelectedValue(SearchItem item, string name, object value, bool append)
        {
            item.SetField(name, value);
            if (value == null)
                return;
            item.value = value;
            if (!append)
            {
                if (item.label == null)
                    item.label = value.ToString();
                else if (item.description == null)
                    item.description = value.ToString();
            }
        }

        [SearchExpressionEvaluator]
        [Description("Create new results by selecting which value and property to take."), Category("Transformers")]
        public static IEnumerable<SearchItem> Select(SearchExpressionContext c)
        {
            return Select(c, append: false);
        }

        [SearchExpressionEvaluator]
        [Description("Add selected values to expression results."), Category("Transformers")]
        public static IEnumerable<SearchItem> Append(SearchExpressionContext c)
        {
            return Select(c, append: true);
        }

        static bool IsSelectorLiteral(SearchExpression selector)
        {
            if (selector.types.HasAny(SearchExpressionType.Text | SearchExpressionType.Selector))
                return true;
            if (selector.types.HasFlag(SearchExpressionType.QueryString) && selector.parameters.Length == 0)
                return true;
            return false;
        }
    }
}
