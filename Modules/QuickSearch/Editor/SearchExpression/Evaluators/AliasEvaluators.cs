// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace UnityEditor.Search
{
    static partial class Evaluators
    {
        [Description("Set expression alias"), Category("Primitives")]
        [SearchExpressionEvaluator(SearchExpressionEvaluationHints.AlwaysExpand, SearchExpressionType.Iterable, SearchExpressionType.Text | SearchExpressionType.Selector | SearchExpressionType.Iterable)]
        public static IEnumerable<SearchItem> Alias(SearchExpressionContext c)
        {
            var aliasSelector = c.args.Last();
            if (c.HasFlag(SearchExpressionExecutionFlags.Expand))
                yield return EvaluatorUtils.CreateSearchExpressionItem(new SearchExpression(c.args[0], newAlias: aliasSelector.innerText));
            else
            {
                foreach (var r in c.args[0].Execute(c))
                {
                    if (r == null)
                    {
                        yield return null;
                        continue;
                    }

                    var hasFormatString = EvaluatorUtils.GetFormatString(aliasSelector, out var formatStr);
                    if (hasFormatString && aliasSelector.types.HasAny(SearchExpressionType.Text))
                    {
                        r.label = EvaluatorUtils.FormatItem(c.search, r, formatStr);
                    }
                    else if (aliasSelector.types.HasAny(SearchExpressionType.Selector))
                    {
                        r.label = SelectorManager.SelectValue(r, c.search, aliasSelector.innerText.ToString()).ToString();
                        yield return r;
                    }
                    else if (aliasSelector.types.HasAny(SearchExpressionType.Iterable))
                    {
                        bool valueSelected = false;
                        using (c.runtime.Push(r))
                        {
                            foreach (var s in aliasSelector.Execute(c))
                            {
                                if (s != null)
                                {
                                    r.label = s.value.ToString();
                                    valueSelected = true;
                                    break;
                                }
                                else
                                    yield return null;
                            }
                        }

                        if (!valueSelected)
                            r.label = r.label = EvaluatorUtils.FormatItem(c.search, r, aliasSelector.innerText.ToString());
                    }
                    else
                        c.ThrowError($"Alias selector `{aliasSelector.outerText}` not supported", aliasSelector.outerText);

                    yield return r;
                }
            }
        }
    }
}
