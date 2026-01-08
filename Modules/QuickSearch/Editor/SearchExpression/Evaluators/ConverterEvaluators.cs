// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;

namespace UnityEditor.Search
{
    static partial class ConverterEvaluators
    {
        [Description("Convert arguments to a number."), Category("Converters")]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable | SearchExpressionType.Literal | SearchExpressionType.Variadic)]
        public static IEnumerable<SearchItem> ToNumber(SearchExpressionContext c)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return c.args.SelectMany(e => e.Execute(c)).Select(item =>
#pragma warning restore RS0030
            {
                SearchExpression.TryConvertToDouble(item, out var value);
                return SearchExpression.CreateItem(value);
            });
        }

        [Description("Convert arguments to a string allowing you to format the result."), Category("Converters")]
        [SearchExpressionEvaluator(SearchExpressionType.Selector | SearchExpressionType.Text, SearchExpressionType.Iterable | SearchExpressionType.Literal | SearchExpressionType.Variadic)]
        [SearchExpressionEvaluatorSignatureOverload(SearchExpressionType.Iterable | SearchExpressionType.Literal | SearchExpressionType.Variadic)]
        public static IEnumerable<SearchItem> Format(SearchExpressionContext c)
        {
            var skipCount = 0;
            if (SearchExpression.GetFormatString(c.args[0], out var formatStr))
                skipCount++;
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var items = c.args.Skip(skipCount).SelectMany(e => e.Execute(c));
#pragma warning restore RS0030
            var dataSet = SearchExpression.ProcessValues(items, null, item => SearchExpression.FormatItem(c.search, item, formatStr));
            return dataSet;
        }

        [Description("Convert arguments to booleans."), Category("Converters")]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable | SearchExpressionType.Literal | SearchExpressionType.Variadic)]
        public static IEnumerable<SearchItem> ToBoolean(SearchExpressionContext c)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return c.args.SelectMany(e => e.Execute(c)).Select(item => SearchExpression.CreateItem(SearchExpression.IsTrue(item)));
#pragma warning restore RS0030
        }
    }
}
