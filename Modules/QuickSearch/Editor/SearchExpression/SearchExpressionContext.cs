// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Search
{
    [Flags]
    enum SearchExpressionExecutionFlags
    {
        None = 0,

        Root = 1 << 0,

        // Evaluation will be mapping some variables
        Mapping = 1 << 1,

        CacheResults = 1 << 2,

        // Expand query in multiple expressions
        Expand = 1 << 3,

        // Pass null to run evaluator function (used for async requests)
        PassNull = 1 << 4,

        // Run the evaluation in a separate thread
        ThreadedEvaluation = 1 << 5,

        TransferedFlags = ThreadedEvaluation
    }

    readonly struct SearchExpressionContext
    {
        public readonly SearchExpressionRuntime runtime;
        public readonly SearchExpression expression;
        public readonly SearchExpression[] args;
        public readonly SearchExpressionExecutionFlags flags;

        public bool valid => runtime.valid && expression != null;
        public SearchContext search => runtime.search;
        public ExecutionState state => runtime.state;
        public IReadOnlyCollection<SearchItem> items => runtime.items;

        public SearchExpressionContext(SearchExpressionRuntime runtime, SearchExpression expression, SearchExpression[] args, SearchExpressionExecutionFlags flags)
        {
            this.runtime = runtime;
            this.expression = expression;
            this.flags = flags;
            this.args = args;
        }

        public override string ToString()
        {
            return $"{expression}[{args.Length}]({flags})";
        }

        public void ThrowError(string message)
        {
            ThrowError(message, StringView.Null);
        }

        public void ThrowError(string message, StringView errorPosition)
        {
            if (!errorPosition.IsNullOrEmpty())
                throw new SearchExpressionEvaluatorException(message, errorPosition, this);
            else
                throw new SearchExpressionEvaluatorException(message, expression.outerText, this);
        }

        public ArgumentEnumerable ForEachArgument(OnArgument onArgumentCallback, int argumentSkipCount = 0)
        {
            return ArgumentEnumerable.ForEachArgument(this, onArgumentCallback, argumentSkipCount);
        }

        public ArgumentEnumerable ForEachArgument(int argumentSkipCount = 0)
        {
            return ArgumentEnumerable.ForEachArgument(this, argumentSkipCount);
        }

        public void Break()
        {
            state.Break();
        }

        public void Continue()
        {
            state.Continue();
        }

        public bool IsBreaking()
        {
            return state.IsBreaking();
        }

        public bool IsContinuing()
        {
            return state.IsContinuing();
        }

        public void ResetIterationControl()
        {
            state.ResetIterationControl();
        }

        public bool HasFlag(SearchExpressionExecutionFlags checkFlag)
        {
            return flags.HasFlag(checkFlag);
        }

        public string ResolveAlias(string defaultLabel = null)
        {
            return ResolveAlias(null, defaultLabel);
        }

        public string ResolveAlias(SearchExpression expr, string defaultLabel = null)
        {
            if (expr != null && !expr.alias.IsNullOrEmpty())
                return expr.alias.ToString();
            foreach (var f in runtime.frames)
            {
                if (!f.valid)
                    continue;
                if (!f.expression.alias.IsNullOrEmpty())
                    return f.expression.alias.ToString();
            }
            return defaultLabel;
        }
    }
}
