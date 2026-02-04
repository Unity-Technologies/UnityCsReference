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
        static int s_NextGroupId = 0;

        [Description("Group search result by a specified @selector"), Category("Set Manipulation")]
        [SearchExpressionEvaluator(SearchExpressionEvaluationHints.ExpandSupported, SearchExpressionType.Iterable, SearchExpressionType.Selector | SearchExpressionType.Optional)]
        public static IEnumerable<SearchItem> GroupBy(SearchExpressionContext c)
        {
            string selector = null;
            if (c.args.Length > 1)
                selector = c.args[1].innerText.ToString();

            var outputValueFieldName = System.Guid.NewGuid().ToString("N");
            var dataSet = SelectorManager.SelectValues(c.search, c.args[0].Execute(c), selector, outputValueFieldName);

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var _group in dataSet.GroupBy(item => item.GetValue(outputValueFieldName)))
#pragma warning restore UA2001
            {
                var group = _group;
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var groupId = group.Key?.ToString() ?? $"group{++s_NextGroupId}";
#pragma warning restore UA2001

                if (c.HasFlag(SearchExpressionExecutionFlags.Expand))
                {
                    var evaluator = new SearchExpressionEvaluator(groupId, _ => group, SearchExpressionEvaluationHints.Default);
                    var genExpr = new SearchExpression(SearchExpressionType.Group,
                        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        groupId.GetStringView(), groupId.GetStringView(), (group.Key?.ToString() ?? groupId).GetStringView(),
#pragma warning restore UA2001
                        evaluator);

                    yield return SearchExpression.CreateSearchExpressionItem(genExpr);
                }
                else
                {
                    SearchProvider groupProvider = null;
                    foreach (var item in group)
                    {
                        if (groupProvider == null)
                            groupProvider = SearchUtils.CreateGroupProvider(item.provider, groupId, s_NextGroupId);
                        item.provider = groupProvider;
                        yield return item;
                    }
                }
            }
        }
    }
}
