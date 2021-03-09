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
        // if{condition, trueExpr[, falseExpr]}
        [Description("Return a different set of results based on the condition value."), Category("Filters")]
        [SearchExpressionEvaluator(SearchExpressionType.AnyValue, SearchExpressionType.AnyValue, SearchExpressionType.AnyValue | SearchExpressionType.Optional)]
        public static IEnumerable<SearchItem> If(SearchExpressionContext c)
        {
            if (c.args.Length < 2)
                c.ThrowError("Not enough parameters for if");

            bool cond = false;
            foreach (var item in c.args[0].Execute(c))
            {
                if (item == null)
                    yield return null;
                else
                {
                    cond |= EvaluatorUtils.IsTrue(item);
                    if (!cond)
                        break;
                }
            }
            if (cond)
            {
                foreach (var item in c.args[1].Execute(c))
                    yield return item;
            }
            else if (c.args.Length == 2)
            {
                // Nothing to do.
            }
            else
            {
                foreach (var item in c.args[2].Execute(c))
                    yield return item;
            }
        }

        [Description("Return true for each expression that is empty."), Category("Filters")]
        [SearchExpressionEvaluator(SearchExpressionType.AnyValue | SearchExpressionType.Variadic)]
        public static IEnumerable<SearchItem> Empty(SearchExpressionContext c)
        {
            return c.args.Select(e => EvaluatorUtils.CreateItem(!e.Execute(c).Any()));
        }

        [Description("Return true if each elements evaluates has being true."), Category("Filters")]
        [SearchExpressionEvaluator(SearchExpressionType.AnyValue | SearchExpressionType.Variadic)]
        public static IEnumerable<SearchItem> IsTrue(SearchExpressionContext c)
        {
            foreach (var e in c.args)
            {
                bool isTrue = false;
                foreach (var item in e.Execute(c))
                {
                    if (item == null)
                        yield return null;
                    else
                    {
                        isTrue |= EvaluatorUtils.IsTrue(item);
                        if (!isTrue)
                            break;
                    }
                }

                yield return EvaluatorUtils.CreateItem(isTrue, c.ResolveAlias(e, "IsTrue"));
            }
        }

        [Description("Create a chain of transformation for search results."), Category("Transformers")]
        [SearchExpressionEvaluator(
            SearchExpressionEvaluationHints.ThreadSupported | SearchExpressionEvaluationHints.DoNotValidateArgsSignature | SearchExpressionEvaluationHints.ImplicitArgsLiterals,
            SearchExpressionType.Iterable, SearchExpressionType.Function | SearchExpressionType.Text | SearchExpressionType.Variadic)]
        public static IEnumerable<SearchItem> Apply(SearchExpressionContext c)
        {
            if (c.args.Length == 0)
                c.ThrowError("Invalid Arguments");

            var resultStreamExpr = c.args[0];
            if (!resultStreamExpr.types.IsIterable())
                c.ThrowError($"No iterators", resultStreamExpr.outerText);

            var exprs = new List<SearchExpression>(c.expression.parameters.Length);
            for (var i = 1; i < c.expression.parameters.Length; ++i)
            {
                var templateExpr = c.expression.parameters[i];
                var currentStreamName = $"apply@stream#{i}";
                var currentStream = EvaluatorUtils.CreateStreamExpression(resultStreamExpr, currentStreamName);
                var args = new[] { currentStream }.Concat(templateExpr.parameters).ToArray();
                var innerText = templateExpr.innerText;
                var evaluator = templateExpr.evaluator;

                if (templateExpr.types.HasFlag(SearchExpressionType.Function))
                {
                    // Back patch arguments
                    var restOfArguments = templateExpr.innerText.Substring(templateExpr.evaluator.name.Length + 1);
                    innerText = $"{templateExpr.evaluator.name}{{{currentStreamName}, {restOfArguments}}}".GetStringView();
                }
                else if (templateExpr.types.HasFlag(SearchExpressionType.Text))
                {
                    // String literal used as an evaluator name:
                    var filterFunction = EvaluatorManager.GetEvaluatorByNameDuringEvaluation(templateExpr.innerText.ToString(), templateExpr.innerText, c);
                    innerText = $"{templateExpr.innerText}{{{currentStreamName}}}".GetStringView();
                    evaluator = filterFunction;
                }
                else
                {
                    c.ThrowError($"Bad argument type", templateExpr.outerText);
                }

                var patchedExpr = new SearchExpression(templateExpr.types, templateExpr.outerText, innerText, evaluator, args);
                exprs.Add(patchedExpr);
                resultStreamExpr = patchedExpr;
            }

            // Pull on the last element to start the evaluation chain
            return exprs.Last().Execute(c);
        }
    }
}
