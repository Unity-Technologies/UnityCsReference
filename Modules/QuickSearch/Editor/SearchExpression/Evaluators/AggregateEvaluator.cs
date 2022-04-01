// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.ComponentModel;

namespace UnityEditor.Search
{
    static partial class Evaluators
    {
        // aggregate{set, template[, iteration_count, field_name, KEEP | SORTED]}
        [Description("Aggregate multiple iterations of a templated expression."), Category("Transformers")]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable, SearchExpressionType.AnyValue,
            SearchExpressionType.Number | SearchExpressionType.Optional,
            SearchExpressionType.QueryString | SearchExpressionType.Text | SearchExpressionType.Optional,
            SearchExpressionType.Keyword | SearchExpressionType.Optional | SearchExpressionType.Variadic)]
        public static IEnumerable<SearchItem> Aggregate(SearchExpressionContext c)
        {
            var initialSet = c.args[0].Execute(c);
            var templateQuery = c.args[1];
            var iterationCount = 1;
            var fieldName = "";
            var setField = false;
            var keep = false;
            var sorted = false;
            if (c.args.Length > 2)
            {
                iterationCount = (int)c.args[2].GetNumberValue(iterationCount);
            }
            if (c.args.Length > 3)
            {
                if (c.args[3].types.HasAny(SearchExpressionType.Keyword))
                {
                    keep |= c.args[3].IsKeyword(SearchExpressionKeyword.Keep);
                    sorted |= c.args[3].IsKeyword(SearchExpressionKeyword.Sort);
                }
                else
                {
                    fieldName = c.args[3].innerText.ToString();
                    setField = !string.IsNullOrEmpty(fieldName);
                }
            }
            if (c.args.Length > 4)
            {
                keep |= c.args[4].IsKeyword(SearchExpressionKeyword.Keep);
                sorted |= c.args[4].IsKeyword(SearchExpressionKeyword.Sort);
            }
            if (c.args.Length > 5)
            {
                keep |= c.args[5].IsKeyword(SearchExpressionKeyword.Keep);
                sorted |= c.args[5].IsKeyword(SearchExpressionKeyword.Sort);
            }

            // Evaluate initial set and decide if we yield it or not (keep)
            var yieldedItems = new HashSet<SearchItem>();
            var currentIteration = 0;
            var itemScore = 0;
            foreach (var item in initialSet)
            {
                if (yieldedItems.Add(item) && keep)
                {
                    if (setField)
                        item.SetField(fieldName, currentIteration);
                    if (sorted)
                        item.score = itemScore++;
                    yield return item;
                }
            }

            // For each iteration resolve templateQuery for each item are previous iteration.
            var toProcessItems = new List<SearchItem>(yieldedItems);
            currentIteration++;
            while (currentIteration <= iterationCount && toProcessItems.Count > 0)
            {
                var currentBatchOfItems = new List<SearchItem>();
                foreach (var sourceItem in toProcessItems)
                {
                    using (c.runtime.Push(sourceItem))
                    {
                        foreach (var itemInBatch in templateQuery.Execute(c))
                        {
                            if (!yieldedItems.Add(itemInBatch))
                                continue;
                            currentBatchOfItems.Add(itemInBatch);
                            if (setField)
                                itemInBatch.SetField(fieldName, currentIteration);

                            // Sort items by batch by multiplying their score per batch number.
                            if (sorted)
                            {
                                itemInBatch.score = itemScore++;
                            }
                            yield return itemInBatch;
                        }
                    }
                }
                toProcessItems = currentBatchOfItems;
                currentIteration++;
            }
        }
    }
}
