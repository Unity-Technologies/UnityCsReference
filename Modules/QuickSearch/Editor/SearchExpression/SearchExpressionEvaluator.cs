// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Text.RegularExpressions;

namespace UnityEditor.Search
{
    delegate IEnumerable<SearchItem> SearchExpressionEvaluatorHandler(SearchExpressionContext context);

    [Flags]
    enum SearchExpressionEvaluationHints
    {
        ThreadSupported = 1 << 0,
        ThreadNotSupported = 1 << 1,
        ExpandSupported = 1 << 2,
        AlwaysExpand = 1 << 3,
        DoNotValidateSignature = 1 << 4,
        DoNotValidateArgsSignature = 1 << 5,
        ImplicitArgsLiterals = 1 << 6,

        Default = ThreadSupported
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    class SearchExpressionEvaluatorAttribute : Attribute
    {
        public SearchExpressionEvaluatorAttribute(params SearchExpressionType[] signatureArgumentTypes) : this(null, SearchExpressionEvaluationHints.Default, new SearchExpressionValidator.Signature(signatureArgumentTypes)) {}
        public SearchExpressionEvaluatorAttribute(string name, params SearchExpressionType[] signatureArgumentTypes) : this(name, SearchExpressionEvaluationHints.Default, new SearchExpressionValidator.Signature(signatureArgumentTypes)) {}

        public SearchExpressionEvaluatorAttribute(SearchExpressionEvaluationHints hints, params SearchExpressionType[] signatureArgumentTypes) : this(null, hints, new SearchExpressionValidator.Signature(signatureArgumentTypes)) {}
        public SearchExpressionEvaluatorAttribute(string name, SearchExpressionEvaluationHints hints, params SearchExpressionType[] signatureArgumentTypes) : this(name, hints, new SearchExpressionValidator.Signature(signatureArgumentTypes)) {}

        private SearchExpressionEvaluatorAttribute(string name, SearchExpressionEvaluationHints hints, SearchExpressionValidator.Signature signature)
        {
            this.name = name;
            this.signature = signature;
            this.hints = hints;
        }

        public string name { get; private set; }
        public SearchExpressionValidator.Signature signature { get; private set; }
        public SearchExpressionEvaluationHints hints { get; private set; }
    }

    static class EvaluatorManager
    {
        public static List<SearchExpressionEvaluator> evaluators { get; private set; }
        public static SearchItemQueryEngine itemQueryEngine;
        public static Dictionary<string, List<SearchExpressionValidator.Signature>> evaluatorSignatures = new Dictionary<string, List<SearchExpressionValidator.Signature>>();

        static EvaluatorManager()
        {
            try
            {
                RefreshEvaluators();
            }
            catch (Exception)
            {
                Debug.LogError("Error while loading evaluators");
            }

            itemQueryEngine = new SearchItemQueryEngine();
        }

        public static SearchExpressionEvaluator GetEvaluatorByNameDuringEvaluation(string name, StringView errorView, SearchExpressionContext context)
        {
            return GetEvaluatorByName(name, errorView, false, context);
        }

        public static SearchExpressionEvaluator GetEvaluatorByNameDuringParsing(string name, StringView errorView)
        {
            return GetEvaluatorByName(name, errorView, true);
        }

        private static SearchExpressionEvaluator GetEvaluatorByName(string name, StringView errorView, bool duringParsing, SearchExpressionContext context = default)
        {
            var evaluator = FindEvaluatorByName(name);
            if (!evaluator.valid)
            {
                if (duringParsing)
                    throw new SearchExpressionParseException(GetEvaluatorNameExceptionMessage(name), errorView.startIndex, errorView.Length);
                else
                    context.ThrowError(GetEvaluatorNameExceptionMessage(name), errorView);
            }
            return evaluator;
        }

        public static SearchExpressionEvaluator GetConstantEvaluatorByName(string name)
        {
            var evaluator = FindEvaluatorByName(name);
            if (!evaluator.valid)
                throw new ArgumentException(GetEvaluatorNameExceptionMessage(name), nameof(name));
            return evaluator;
        }

        private static string GetEvaluatorNameExceptionMessage(string name)
        {
            return $"Search expression evaluator {name} does not exist.";
        }

        public static IEnumerable<SearchExpressionValidator.Signature> GetSignaturesByName(string name)
        {
            name = name.ToLowerInvariant();
            if (evaluatorSignatures.TryGetValue(name.ToLowerInvariant(), out var signatures))
            {
                return signatures.OrderByDescending(s => s.mandatoryArgumentNumber);
            }
            return null;
        }

        public static void AddSignature(string name, SearchExpressionValidator.Signature signature)
        {
            name = name.ToLowerInvariant();
            string error = "";
            if (!SearchExpressionValidator.ValidateSignature(name, signature, ref error))
            {
                Debug.LogError($"Invalid signature for {name}({signature}) : {error}");
                return;
            }

            if (!evaluatorSignatures.TryGetValue(name.ToLowerInvariant(), out var signatures))
            {
                signatures = new List<SearchExpressionValidator.Signature>();
                evaluatorSignatures.Add(name, signatures);
            }
            evaluatorSignatures[name].Add(signature);
        }

        public static SearchExpressionEvaluator FindEvaluatorByName(string name)
        {
            foreach (var e in evaluators)
                if (string.Equals(e.name, name, StringComparison.OrdinalIgnoreCase))
                    return e;
            return default;
        }

        public static void RefreshEvaluators()
        {
            var supportedSignature = MethodSignature.FromDelegate<SearchExpressionEvaluatorHandler>();
            evaluators = ReflectionUtils.LoadAllMethodsWithAttribute<SearchExpressionEvaluatorAttribute, SearchExpressionEvaluator>((mi, attribute, handler) =>
            {
                var descAttr = mi.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
                var description = descAttr != null ? descAttr.Description : null;
                var catAttr = mi.GetCustomAttribute<System.ComponentModel.CategoryAttribute>();
                var category = catAttr != null ? catAttr.Category : "General";
                var name = attribute.name ?? mi.Name;
                if (handler is SearchExpressionEvaluatorHandler _handler)
                {
                    if (attribute.signature != null && attribute.signature.argumentCount > 0)
                        AddSignature(name, attribute.signature);
                    return new SearchExpressionEvaluator(name, description, category, _handler, attribute.hints);
                }

                Debug.LogWarning($"Invalid evaluator handler: \"{attribute.name}\" using: \"{mi.DeclaringType.FullName}.{mi.Name}\"");
                return new SearchExpressionEvaluator();
            }, supportedSignature, ReflectionUtils.AttributeLoaderBehavior.DoNotThrowOnValidation).Distinct().Where(evaluator => evaluator.valid).ToList();
        }
    }

    class SearchExpressionEvaluatorException : Exception
    {
        public SearchExpressionContext evaluationContext { get; private set; }
        public StringView errorView { get; private set; }
        public SearchExpressionEvaluatorException(SearchExpressionContext c, Exception innerException = null)
            : this(innerException != null ? innerException.Message : "Failed", GetErrorStringView(c, innerException), c, innerException)
        {
        }

        public SearchExpressionEvaluatorException(string message, StringView errorPosition, SearchExpressionContext c, Exception innerException = null)
            : base(FormatDefaultEvaluationExceptionMessage(c, message), innerException)
        {
            if (errorPosition.IsNullOrEmpty())
            {
                errorView = new StringView(c.search.searchText);
            }
            else
            {
                errorView = errorPosition;
            }
            evaluationContext = c;
        }

        private static string FormatDefaultEvaluationExceptionMessage(SearchExpressionContext c, string prefixMessage = "Failed")
        {
            if (!c.valid)
                return prefixMessage;
            var e = c.expression;
            var eval = e.evaluator;
            var m = eval.execute.Method;
            return $"{prefixMessage} to evaluate expression `{e.outerText}` using {m.DeclaringType.Name}.{m.Name}";
        }

        private static StringView GetErrorStringView(SearchExpressionContext c, Exception innerException)
        {
            if (innerException is SearchExpressionEvaluatorException runtimeEx)
                return runtimeEx.errorView;

            return c.expression?.outerText ?? c.search.searchQuery.GetStringView();
        }
    }

    readonly struct SearchExpressionEvaluator : IEqualityComparer<SearchExpressionEvaluator>
    {
        public readonly string name;
        public readonly string description;
        public readonly string category;
        public readonly SearchExpressionEvaluatorHandler execute;
        public readonly SearchExpressionEvaluationHints hints;
        public bool valid => execute != null;
        public readonly SearchExpressionParserFlags with;
        public readonly SearchExpressionParserFlags without;

        public SearchExpressionEvaluator(string name, string description, string category, SearchExpressionEvaluatorHandler execute, SearchExpressionEvaluationHints hints)
            : this(name, description, category, execute, hints, SearchExpressionParserFlags.None, SearchExpressionParserFlags.None)
        {
        }

        public SearchExpressionEvaluator(string name, SearchExpressionEvaluatorHandler execute, SearchExpressionEvaluationHints hints)
            : this(name, description : null , category : "General", execute, hints, SearchExpressionParserFlags.None, SearchExpressionParserFlags.None)
        {
        }

        public SearchExpressionEvaluator(string name, string description, string category, SearchExpressionEvaluatorHandler execute, SearchExpressionEvaluationHints hints, SearchExpressionParserFlags with, SearchExpressionParserFlags without)
        {
            this.name = name;
            this.description = description;
            this.category = category;
            this.execute = execute;
            this.hints = hints;
            this.with = with;
            this.without = without;
        }

        public override string ToString()
        {
            return $"{name} | {execute.Method.DeclaringType.Name}.{execute.Method.Name}";
        }

        public bool Equals(SearchExpressionEvaluator x, SearchExpressionEvaluator y)
        {
            return x.name == y.name;
        }

        public int GetHashCode(SearchExpressionEvaluator obj)
        {
            return obj.name.GetHashCode();
        }
    }

    static class EvaluatorUtils
    {
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
            return SearchExpressionProvider.CreateItem(CreateId(), label, description, value);
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

        internal static bool IsTrue(SearchItem item)
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

        internal static bool Check(SearchExpression e, SearchExpressionContext c)
        {
            var result = e.Execute(c);
            var count = result.Count();
            if (count == 0)
                return false;
            if (count == 1)
                return IsTrue(result.First());
            return true;
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

        public static SearchExpression CreateStreamExpression(SearchExpression currentStream, string name = "<streamexpr>")
        {
            var exp = new SearchExpression(SearchExpressionType.Function, name.GetStringView(),
                new SearchExpressionEvaluator("StreamEvaluator", context => currentStream.Execute(context), SearchExpressionEvaluationHints.Default));
            return exp;
        }

        public static SearchItem CreateSearchExpressionItem(string queryString, string alias = null)
        {
            var queryView = queryString.GetStringView();
            return CreateSearchExpressionItem(new SearchExpression(
                SearchExpressionType.QueryString, queryView, queryView, alias.GetStringView(),
                EvaluatorManager.GetConstantEvaluatorByName("query")));
        }

        public static SearchItem CreateSearchExpressionItem(SearchExpression expr)
        {
            return new SearchItem("") { data = expr };
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
    }
}
