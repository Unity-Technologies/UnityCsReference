// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace UnityEditor.Search
{
    [Flags]
    enum SearchExpressionType
    {
        Nil = 0,

        Optional = 1 << 0,
        Variadic = 1 << 1,

        Boolean = 1 << 2,
        Number = 1 << 4,
        Text = 1 << 5,

        Selector = 1 << 6,
        Keyword = 1 << 7,

        Set = 1 << 8,
        Function = 1 << 10,
        QueryString = 1 << 11,

        Expandable = 1 << 12,
        Group = 1 << 13,

        Literal = Boolean | Number | Text | Keyword,
        Iterable = Set | Function | QueryString,
        AnyValue = Literal | Iterable,
        AnyExpression = Literal | Iterable | Selector
    }

    enum SearchExpressionKeyword
    {
        asc,
        desc,
        any,
        all,
        none
    }

    static class SearchExpressionTypeExtensions
    {
        public static bool HasAny(this SearchExpressionType type, SearchExpressionType flags)
        {
            if (type == SearchExpressionType.Nil && flags == SearchExpressionType.Nil)
                return true;
            return (type & flags) != 0;
        }

        public static bool IsIterable(this SearchExpressionType type)
        {
            return type.HasAny(SearchExpressionType.Iterable);
        }

        public static bool IsLiteral(this SearchExpressionType type)
        {
            return type.HasAny(SearchExpressionType.Literal);
        }

        public static bool IsText(this SearchExpressionType type)
        {
            return type.HasAny(SearchExpressionType.Literal | SearchExpressionType.Selector);
        }
    }

    class SearchExpression
    {
        public string name => evaluator.name ?? outerText.ToString();

        // Expression info
        public readonly SearchExpressionType types;
        public readonly StringView outerText;
        public readonly StringView innerText;
        public readonly StringView alias;

        // Evaluation fields
        public readonly SearchExpressionEvaluator evaluator;
        public readonly SearchExpression[] parameters;

        public SearchExpression(SearchExpressionType types,
                                StringView outerText, StringView innerText, StringView alias,
                                SearchExpressionEvaluator evaluator, SearchExpression[] parameters)
        {
            this.types = types;
            this.outerText = outerText;
            this.innerText = innerText.valid ? innerText : outerText;
            this.alias = alias;
            this.parameters = parameters ?? new SearchExpression[0];
            this.evaluator = evaluator;
        }

        public SearchExpression(SearchExpressionType types, StringView text)
            : this(types, text, StringView.Null, StringView.Null, default, null)
        {
        }

        public SearchExpression(SearchExpressionType types, StringView outerText, StringView innerText)
            : this(types, outerText, innerText, StringView.Null, default, null)
        {
        }

        public SearchExpression(SearchExpressionType types, StringView outerText, StringView innerText, SearchExpressionEvaluator evaluator)
            : this(types, outerText, innerText, StringView.Null, evaluator, null)
        {
        }

        public SearchExpression(SearchExpressionType types, StringView outerText, StringView innerText, SearchExpressionEvaluator evaluator, SearchExpression[] parameters)
            : this(types, outerText, innerText, StringView.Null, evaluator, parameters)
        {
        }

        public SearchExpression(SearchExpressionType types, StringView outerText, StringView innerText, StringView alias, SearchExpressionEvaluator evaluator)
            : this(types, outerText, innerText, alias, evaluator, null)
        {
        }

        public SearchExpression(SearchExpressionType types, StringView text, SearchExpressionEvaluator evaluator)
            : this(types, text, text, StringView.Null, evaluator, null)
        {
        }

        public SearchExpression(SearchExpressionType types, StringView text, SearchExpressionEvaluator evaluator, SearchExpression[] parameters)
            : this(types, text, text, StringView.Null, evaluator, parameters)
        {
        }

        public SearchExpression(SearchExpression ex, SearchExpressionType types, StringView outerText, StringView innerText)
            : this(types, outerText, innerText, ex.alias, ex.evaluator, ex.parameters)
        {
        }

        public SearchExpression(SearchExpression ex, StringView newAlias)
            : this(ex.types, ex.outerText, ex.innerText, newAlias, ex.evaluator, ex.parameters)
        {
        }

        public override string ToString()
        {
            if (alias.IsNullOrEmpty())
                return $"{outerText} ({types})";
            return $"{outerText} | {alias} ({types})";
        }

        public bool GetBooleanValue(bool defaultValue = false)
        {
            if (bool.TryParse(innerText.ToString(), out var b))
                return b;
            if (innerText == "1")
                return true;
            if (innerText == "0")
                return false;
            return defaultValue;
        }

        public double GetNumberValue(double defaultValue = double.NaN)
        {
            if (Utils.TryParse(innerText.ToString(), out double d))
                return d;
            return defaultValue;
        }

        public static SearchExpression Parse(SearchExpressionParserArgs args)
        {
            return ParserManager.Parse(args);
        }

        public static SearchExpression Parse(string text, SearchExpressionParserFlags options = SearchExpressionParserFlags.Default)
        {
            var searchContext = SearchService.CreateContext(text);
            return Parse(searchContext, options);
        }

        public static SearchExpression Parse(SearchContext context, SearchExpressionParserFlags options = SearchExpressionParserFlags.Default)
        {
            return ParserManager.Parse(new SearchExpressionParserArgs(context.searchText.GetStringView(), context, options));
        }

        public IEnumerable<SearchItem> Execute(SearchContext searchContext, SearchExpressionExecutionFlags executionFlags = SearchExpressionExecutionFlags.Root)
        {
            var runtime = new SearchExpressionRuntime(searchContext, executionFlags);
            if (executionFlags.HasFlag(SearchExpressionExecutionFlags.ThreadedEvaluation))
                return TaskEvaluatorManager.Evaluate(runtime.current, this);
            return Execute(runtime.current, executionFlags);
        }

        public IEnumerable<SearchItem> Execute(SearchExpressionContext c, SearchExpressionExecutionFlags flags = SearchExpressionExecutionFlags.None)
        {
            if (!evaluator.valid || evaluator.execute == null)
                c.ThrowError("Invalid expression evaluator");
            try
            {
                if (!evaluator.hints.HasFlag(SearchExpressionEvaluationHints.ThreadNotSupported))
                    return Evaluate(c, flags);

                // We cannot only return the IEnumerable of the evaluator, as the iteration itself needs to be
                // done on the main thread. If we return the IEnumerable itself, we will unroll the items and call the evaluator
                // in the evaluation thread.
                return TaskEvaluatorManager.EvaluateMainThreadUnroll(() => Evaluate(c, flags));
            }
            catch (SearchExpressionEvaluatorException ex)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
                return null; // To stop visual studio complaining about not all code path return a value
            }
        }

        private IEnumerable<SearchItem> Evaluate(SearchExpressionContext c, SearchExpressionExecutionFlags flags)
        {
            var args = new List<SearchExpression>();
            foreach (var p in parameters)
            {
                var evalHints = p.evaluator.hints;
                if (evalHints.HasFlag(SearchExpressionEvaluationHints.AlwaysExpand) ||
                    (p.types.HasFlag(SearchExpressionType.Expandable) && evalHints.HasFlag(SearchExpressionEvaluationHints.ExpandSupported)))
                {
                    foreach (var exprItem in p.Execute(c, SearchExpressionExecutionFlags.Expand))
                    {
                        if (exprItem != null)
                        {
                            if (exprItem.data is SearchExpression expr)
                                args.Add(expr);
                            else
                                c.ThrowError($"cannot expand {p}");
                        }
                        else
                            yield return null;
                    }
                }
                else if (p.types.HasFlag(SearchExpressionType.Expandable))
                {
                    foreach (var exprItem in p.Execute(c))
                    {
                        if (exprItem == null)
                        {
                            yield return null;
                            continue;
                        }

                        if (exprItem.value != null)
                        {
                            if (Utils.TryGetNumber(exprItem.value, out var d))
                                args.Add(new SearchExpression(SearchExpressionType.Number, d.ToString().GetStringView(), Parsers.ConstantEvaluator));
                            else if (exprItem.value is string s)
                                args.Add(new SearchExpression(SearchExpressionType.Text, s.GetStringView(), Parsers.ConstantEvaluator));
                            else
                                c.ThrowError("Cannot expand parameters");
                        }
                        else
                            c.ThrowError("Cannot expand null value");
                    }
                }
                else
                {
                    args.Add(p);
                }
            }

            using (c.runtime.Push(this, args, flags))
            {
                var skipNull = c.HasFlag(SearchExpressionExecutionFlags.ThreadedEvaluation) && !c.HasFlag(SearchExpressionExecutionFlags.PassNull);
                var executeContext = c.runtime.current;
                var timeoutWatch = new System.Diagnostics.Stopwatch();
                timeoutWatch.Start();
                foreach (var r in evaluator.execute(executeContext))
                {
                    if (r != null)
                    {
                        timeoutWatch.Restart();
                        yield return r;
                    }
                    else if (!skipNull)
                    {
                        if (timeoutWatch.Elapsed.TotalSeconds > 3.0d)
                            c.ThrowError("Timeout");
                        yield return null;
                    }
                }
            }
        }

        public SearchExpression Apply(string functionName, params SearchExpression[] args)
        {
            var see = EvaluatorManager.GetEvaluatorByNameDuringParsing(functionName, innerText);
            var seeArgs = new List<SearchExpression> { this };
            seeArgs.AddRange(args);
            return new SearchExpression(SearchExpressionType.Function, StringView.Null, innerText, alias, see, seeArgs.ToArray());
        }

        public bool IsKeyword(SearchExpressionKeyword keyword)
        {
            return innerText.Equals(keyword.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
