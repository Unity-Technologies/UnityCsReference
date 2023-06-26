// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace UnityEditor.Search
{
    [Flags]
    public enum SearchExpressionType
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

    public enum SearchExpressionKeyword
    {
        None,
        Asc,
        Desc,
        Any,
        All,
        Keep,
        Sort
    }

    static class SearchExpressionTypeExtensions
    {
        public static bool HasAny(this SearchExpressionType type, SearchExpressionType flags)
        {
            if (type == SearchExpressionType.Nil && flags == SearchExpressionType.Nil)
                return true;
            return (type & flags) != 0;
        }

        public static bool HasNone(this SearchExpressionType type, SearchExpressionType flags)
        {
            if (type == SearchExpressionType.Nil && flags == SearchExpressionType.Nil)
                return false; // None of Nil means there should be something.
            return (type & flags) == 0;
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

    public class SearchExpression
    {
        public string name => evaluator.name ?? outerText.ToString();

        // Expression info
        public readonly SearchExpressionType types;
        public readonly StringView outerText;
        public readonly StringView innerText;
        public readonly StringView alias;
        internal bool hasEscapedNestedExpressions;

        // Evaluation fields
        internal readonly SearchExpressionEvaluator evaluator;
        public readonly SearchExpression[] parameters;

        internal SearchExpression(SearchExpressionType types,
                                StringView outerText, StringView innerText, StringView alias,
                                SearchExpressionEvaluator evaluator, SearchExpression[] parameters, bool hasEscapedNestedExpressions)
        {
            this.types = types;
            this.outerText = outerText;
            this.innerText = innerText.valid ? innerText : outerText;
            this.alias = alias;
            this.parameters = parameters ?? new SearchExpression[0];
            this.evaluator = evaluator;
            this.hasEscapedNestedExpressions = hasEscapedNestedExpressions;
        }

        internal SearchExpression(SearchExpressionType types,
            StringView outerText, StringView innerText, StringView alias,
            SearchExpressionEvaluator evaluator, SearchExpression[] parameters)
            : this(types, outerText, innerText, alias, evaluator, parameters, false)
        {}

        internal SearchExpression(SearchExpressionType types, StringView text)
            : this(types, text, StringView.nil, StringView.nil, default, null, false)
        {
        }

        internal SearchExpression(SearchExpressionType types, StringView outerText, StringView innerText)
            : this(types, outerText, innerText, StringView.nil, default, null, false)
        {
        }

        internal SearchExpression(SearchExpressionType types, StringView outerText, StringView innerText, SearchExpressionEvaluator evaluator)
            : this(types, outerText, innerText, StringView.nil, evaluator, null, false)
        {
        }

        internal SearchExpression(SearchExpressionType types, StringView outerText, StringView innerText, SearchExpressionEvaluator evaluator, SearchExpression[] parameters)
            : this(types, outerText, innerText, StringView.nil, evaluator, parameters, false)
        {
        }

        internal SearchExpression(SearchExpressionType types, StringView outerText, StringView innerText, SearchExpressionEvaluator evaluator, SearchExpression[] parameters, bool hasEscapedNestedExpressions)
            : this(types, outerText, innerText, StringView.nil, evaluator, parameters, hasEscapedNestedExpressions)
        {
        }

        internal SearchExpression(SearchExpressionType types, StringView outerText, StringView innerText, StringView alias, SearchExpressionEvaluator evaluator)
            : this(types, outerText, innerText, alias, evaluator, null, false)
        {
        }

        internal SearchExpression(SearchExpressionType types, StringView text, SearchExpressionEvaluator evaluator)
            : this(types, text, text, StringView.nil, evaluator, null, false)
        {
        }

        internal SearchExpression(SearchExpressionType types, StringView text, SearchExpressionEvaluator evaluator, SearchExpression[] parameters)
            : this(types, text, text, StringView.nil, evaluator, parameters, false)
        {
        }

        internal SearchExpression(SearchExpression ex, SearchExpressionType types, StringView outerText, StringView innerText)
            : this(types, outerText, innerText, ex.alias, ex.evaluator, ex.parameters, false)
        {
        }

        internal SearchExpression(SearchExpression ex, StringView newAlias)
            : this(ex.types, ex.outerText, ex.innerText, newAlias, ex.evaluator, ex.parameters, false)
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

        public bool IsKeyword(SearchExpressionKeyword keyword)
        {
            return innerText.Equals(keyword.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public IEnumerable<SearchItem> Execute(SearchContext searchContext)
        {
            return Execute(searchContext, SearchExpressionExecutionFlags.Root);
        }

        public IEnumerable<SearchItem> Execute(SearchExpressionContext c)
        {
            return Execute(c, SearchExpressionExecutionFlags.None);
        }

        internal IEnumerable<SearchItem> Execute(SearchContext searchContext, SearchExpressionExecutionFlags executionFlags)
        {
            var runtime = new SearchExpressionRuntime(searchContext, executionFlags);
            if (executionFlags.HasFlag(SearchExpressionExecutionFlags.ThreadedEvaluation))
                return TaskEvaluatorManager.Evaluate(runtime.current, this);
            return Execute(runtime.current, executionFlags);
        }

        internal IEnumerable<SearchItem> Execute(SearchExpressionContext c, SearchExpressionExecutionFlags executionFlags)
        {
            if (!evaluator.valid || evaluator.execute == null)
                c.ThrowError("Invalid expression evaluator");
            try
            {
                if (!evaluator.hints.HasFlag(SearchExpressionEvaluationHints.ThreadNotSupported))
                    return Evaluate(c, executionFlags);

                // We cannot only return the IEnumerable of the evaluator, as the iteration itself needs to be
                // done on the main thread. If we return the IEnumerable itself, we will unroll the items and call the evaluator
                // in the evaluation thread.
                return TaskEvaluatorManager.EvaluateMainThreadUnroll(() => Evaluate(c, executionFlags));
            }
            catch (SearchExpressionEvaluatorException ex)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
                return null; // To stop visual studio complaining about not all code path return a value
            }
        }

        internal static SearchExpression Parse(SearchExpressionParserArgs args)
        {
            return ParserManager.Parse(args);
        }

        internal static SearchExpression Parse(string text, SearchExpressionParserFlags options = SearchExpressionParserFlags.Default)
        {
            var searchContext = SearchService.CreateContext(text);
            return Parse(searchContext, options);
        }

        internal static SearchExpression Parse(SearchContext context, SearchExpressionParserFlags options = SearchExpressionParserFlags.Default)
        {
            return ParserManager.Parse(new SearchExpressionParserArgs(context.searchText.GetStringView(), context, options));
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

        internal SearchExpression Apply(string functionName, params SearchExpression[] args)
        {
            var see = EvaluatorManager.GetEvaluatorByNameDuringParsing(functionName, innerText);
            var seeArgs = new List<SearchExpression> { this };
            seeArgs.AddRange(args);
            return new SearchExpression(SearchExpressionType.Function, StringView.nil, innerText, alias, see, seeArgs.ToArray(), hasEscapedNestedExpressions);
        }

        public static bool TryConvertToDouble(SearchItem item, out double value, string selector = null)
        {
            value = double.NaN;
            object itemValue = SelectorManager.SelectValue(item, null, selector);
            if (itemValue == null)
                return false;
            return Utils.TryGetNumber(itemValue, out value);
        }

        public static string CreateId()
        {
            return Guid.NewGuid().ToString("N");
        }

        public static SearchItem CreateItem(string label, object value, string description)
        {
            return SearchServiceProvider.CreateItem(CreateId(), label, description, value);
        }

        public static SearchItem CreateItem(string value, string label = null)
        {
            return CreateItem(label, value, value.ToString());
        }

        public static SearchItem CreateItem(double value, string label = null)
        {
            return CreateItem(label, value, null);
        }

        public static SearchItem CreateItem(int value, string label = null)
        {
            return CreateItem((double)value, label);
        }

        internal static SearchItem CreateItem(bool booleanValue, string label = null)
        {
            return CreateItem(label, booleanValue, booleanValue.ToString().ToLowerInvariant());
        }

        public static bool GetFormatString(SearchExpression expr, out string formatStr)
        {
            formatStr = null;
            if (expr.types.HasFlag(SearchExpressionType.Text))
            {
                formatStr = expr.innerText.ToString();
                return true;
            }

            if (expr.types.HasFlag(SearchExpressionType.Selector))
            {
                formatStr = expr.outerText.ToString();
                return true;
            }

            return false;
        }

        public static string FormatItem(SearchContext ctx, SearchItem item, string formatString)
        {
            if (formatString == null)
            {
                var value = SelectorManager.SelectValue(item, ctx, null);
                return value == null ? "null" : value.ToString();
            }
            return ParserUtils.ReplaceSelectorInExpr(formatString, (selector, cleanedSelector) =>
            {
                var value = SelectorManager.SelectValue(item, ctx, cleanedSelector);
                return value == null ? "<no value>" : value.ToString();
            });
        }

        public static IEnumerable<SearchItem> ProcessValues<T>(IEnumerable<SearchItem> items, string outputValueFieldName, Func<SearchItem, T> processHandler)
        {
            var selectedItems = new List<SearchItem>(items);
            return TaskEvaluatorManager.EvaluateMainThread<SearchItem>((yielder) =>
            {
                foreach (var item in selectedItems)
                {
                    var selectedValue = processHandler(item);
                    if (selectedValue == null)
                        continue;

                    if (outputValueFieldName == null)
                        item.value = selectedValue;
                    else
                        item.SetField(outputValueFieldName, selectedValue);

                    yielder(item);
                }
            });
        }

        public static bool IsTrue(SearchItem item)
        {
            var value = false;
            if (item.value != null)
            {
                if (item.value is string argValue)
                {
                    value = argValue != "";
                }
                else if (item.value is bool itemValue)
                {
                    value = itemValue;
                }
                else if (item.value is int i)
                {
                    value = i != 0;
                }
                else if (item.value is double d)
                {
                    value = d != 0.0;
                }
                else if (item.value is float f)
                {
                    value = f != 0.0f;
                }
            }
            return value;
        }

        public static bool Check(SearchExpression e, SearchExpressionContext c)
        {
            var result = e.Execute(c);
            var count = result.Count();
            if (count == 0)
                return false;
            if (count == 1)
                return IsTrue(result.First());
            return true;
        }

        internal static SearchExpression CreateStreamExpression(SearchExpression currentStream, string name = "<streamexpr>")
        {
            var exp = new SearchExpression(SearchExpressionType.Function, name.GetStringView(),
                new SearchExpressionEvaluator("StreamEvaluator", context => currentStream.Execute(context), SearchExpressionEvaluationHints.Default));
            return exp;
        }

        internal static SearchItem CreateSearchExpressionItem(string queryString, string alias = null)
        {
            var queryView = queryString.GetStringView();
            return CreateSearchExpressionItem(new SearchExpression(
                SearchExpressionType.QueryString, queryView, queryView, alias.GetStringView(),
                EvaluatorManager.GetConstantEvaluatorByName("query")));
        }

        internal static SearchItem CreateSearchExpressionItem(SearchExpression expr)
        {
            return new SearchItem("") { data = expr };
        }

    }
}
