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
            return c.args.SelectMany(e => e.Execute(c)).Select(item =>
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
            var items = c.args.Skip(skipCount).SelectMany(e => e.Execute(c));
            var dataSet = SearchExpression.ProcessValues(items, null, item => SearchExpression.FormatItem(c.search, item, formatStr));
            return dataSet;
        }

        [Description("Convert arguments to booleans."), Category("Converters")]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable | SearchExpressionType.Literal | SearchExpressionType.Variadic)]
        public static IEnumerable<SearchItem> ToBoolean(SearchExpressionContext c)
        {
            return c.args.SelectMany(e => e.Execute(c)).Select(item => SearchExpression.CreateItem(SearchExpression.IsTrue(item)));
        }
    }
}
