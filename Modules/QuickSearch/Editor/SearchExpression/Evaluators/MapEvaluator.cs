// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.ComponentModel;

namespace UnityEditor.Search
{
    static partial class Evaluators
    {
        [Description("Map an expression results to form a new expression using variables."), Category("Transformers")]
        [SearchExpressionEvaluator(SearchExpressionType.Iterable, SearchExpressionType.AnyValue)]
        public static IEnumerable<SearchItem> Map(SearchExpressionContext c)
        {
            var mapSet = c.args[0].Execute(c);
            var mapQuery = c.args[1];
            foreach (var m in mapSet)
            {
                if (m == null)
                    yield return null;
                else
                {
                    using (c.runtime.Push(m))
                    {
                        foreach (var e in mapQuery.Execute(c))
                            yield return e;
                    }
                }
            }
        }
    }
}
