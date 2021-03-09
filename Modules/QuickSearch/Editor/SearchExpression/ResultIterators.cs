// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Search
{
    delegate void OnArgument(SearchExpressionContext c, SearchExpression e);
    delegate SearchItem OnArgumentResult(SearchExpressionContext c, SearchItem i);
    delegate void OnAggregateStart<in TAggregator>(SearchExpressionContext c, TAggregator aggregator);
    delegate void OnAggregateResult<in TAggregator>(SearchExpressionContext c, SearchItem i, TAggregator aggregator);
    delegate TAggregator OnAggregatePrimitiveStart<TAggregator>(SearchExpressionContext c, TAggregator aggregator);
    delegate TAggregator OnAggregatePrimitiveResult<TAggregator>(SearchExpressionContext c, SearchItem i, TAggregator aggregator);
    delegate IEnumerable<SearchItem> OnAggregateToSearchItems<in TAggregator>(SearchExpressionContext c, TAggregator aggregator);
    delegate SearchItem OnAggregateToSearchItem<in TAggregator>(SearchExpressionContext c, TAggregator aggregator);

    class ArgumentEnumerable : IEnumerable<SearchExpression>
    {
        int m_ArgumentSkipCount = 0;
        OnArgument m_OnArgument;

        public SearchExpressionContext context;

        ArgumentEnumerable(SearchExpressionContext c)
        {
            context = c;
        }

        public static ArgumentEnumerable ForEachArgument(SearchExpressionContext c, OnArgument onArgumentCallback, int argumentSkipCount = 0)
        {
            return new ArgumentEnumerable(c) { m_OnArgument = onArgumentCallback, m_ArgumentSkipCount = argumentSkipCount };
        }

        public static ArgumentEnumerable ForEachArgument(SearchExpressionContext c, int argumentSkipCount = 0)
        {
            return new ArgumentEnumerable(c) { m_OnArgument = DefaultArgumentIteration, m_ArgumentSkipCount = argumentSkipCount };
        }

        static void DefaultArgumentIteration(SearchExpressionContext c, SearchExpression e)
        {
        }

        public ResultEnumeration ForEachResult(OnArgumentResult onArgumentResult, bool skipNull = true)
        {
            return new ResultEnumeration(this, onArgumentResult, skipNull);
        }

        public ResultAggregation<TAggregator> AggregateResults<TAggregator>(TAggregator aggregator, OnAggregateResult<TAggregator> onAggregateResult, OnAggregateToSearchItems<TAggregator> onAggregateToSearchItems, bool skipNull = true)
            where TAggregator : class
        {
            return new ResultAggregation<TAggregator>(this, onAggregateResult, aggregator, onAggregateToSearchItems, skipNull);
        }

        public ResultAggregation<TAggregator> AggregateResults<TAggregator>(TAggregator aggregator, OnAggregateResult<TAggregator> onAggregateResult, OnAggregateToSearchItem<TAggregator> onAggregateToSearchItem, bool skipNull = true)
            where TAggregator : class
        {
            return new ResultAggregation<TAggregator>(this, onAggregateResult, aggregator, onAggregateToSearchItem, skipNull);
        }

        public ResultAggregation<TAggregator> AggregateResults<TAggregator>(TAggregator aggregator, OnAggregateResult<TAggregator> onAggregateResult, bool skipNull = true)
            where TAggregator : class
        {
            return new ResultAggregation<TAggregator>(this, onAggregateResult, aggregator, skipNull);
        }

        public ResultAggregation<TAggregator> AggregateResults<TAggregator>(TAggregator aggregator, OnAggregateStart<TAggregator> onAggregateStart, OnAggregateResult<TAggregator> onAggregateResult, OnAggregateToSearchItems<TAggregator> onAggregateToSearchItems, bool skipNull = true)
            where TAggregator : class
        {
            return new ResultAggregation<TAggregator>(this, onAggregateStart, onAggregateResult, aggregator, onAggregateToSearchItems, skipNull);
        }

        public ResultAggregation<TAggregator> AggregateResults<TAggregator>(TAggregator aggregator, OnAggregateStart<TAggregator> onAggregateStart, OnAggregateResult<TAggregator> onAggregateResult, OnAggregateToSearchItem<TAggregator> onAggregateToSearchItem, bool skipNull = true)
            where TAggregator : class
        {
            return new ResultAggregation<TAggregator>(this, onAggregateStart, onAggregateResult, aggregator, onAggregateToSearchItem, skipNull);
        }

        public ResultAggregation<TAggregator> AggregateResults<TAggregator>(TAggregator aggregator, OnAggregateStart<TAggregator> onAggregateStart, OnAggregateResult<TAggregator> onAggregateResult, bool skipNull = true)
            where TAggregator : class
        {
            return new ResultAggregation<TAggregator>(this, onAggregateStart, onAggregateResult, aggregator, skipNull);
        }

        public ResultPrimitiveAggregation<TAggregator> AggregateResults<TAggregator>(TAggregator aggregator, OnAggregatePrimitiveResult<TAggregator> onAggregateResult, OnAggregateToSearchItems<TAggregator> onAggregateToSearchItems, bool skipNull = true)
            where TAggregator : struct
        {
            return new ResultPrimitiveAggregation<TAggregator>(this, onAggregateResult, aggregator, onAggregateToSearchItems, skipNull);
        }

        public ResultPrimitiveAggregation<TAggregator> AggregateResults<TAggregator>(TAggregator aggregator, OnAggregatePrimitiveResult<TAggregator> onAggregateResult, OnAggregateToSearchItem<TAggregator> onAggregateToSearchItem, bool skipNull = true)
            where TAggregator : struct
        {
            return new ResultPrimitiveAggregation<TAggregator>(this, onAggregateResult, aggregator, onAggregateToSearchItem, skipNull);
        }

        public ResultPrimitiveAggregation<TAggregator> AggregateResults<TAggregator>(TAggregator aggregator, OnAggregatePrimitiveStart<TAggregator> onAggregateStart, OnAggregatePrimitiveResult<TAggregator> onAggregateResult, OnAggregateToSearchItems<TAggregator> onAggregateToSearchItems, bool skipNull = true)
            where TAggregator : struct
        {
            return new ResultPrimitiveAggregation<TAggregator>(this, onAggregateStart, onAggregateResult, aggregator, onAggregateToSearchItems, skipNull);
        }

        public ResultPrimitiveAggregation<TAggregator> AggregateResults<TAggregator>(TAggregator aggregator, OnAggregatePrimitiveStart<TAggregator> onAggregateStart, OnAggregatePrimitiveResult<TAggregator> onAggregateResult, OnAggregateToSearchItem<TAggregator> onAggregateToSearchItem, bool skipNull = true)
            where TAggregator : struct
        {
            return new ResultPrimitiveAggregation<TAggregator>(this, onAggregateStart, onAggregateResult, aggregator, onAggregateToSearchItem, skipNull);
        }

        public IEnumerator<SearchExpression> GetEnumerator()
        {
            foreach (var argument in context.args.Skip(m_ArgumentSkipCount))
            {
                m_OnArgument(context.runtime.current, argument);
                if (context.IsBreaking())
                {
                    context.ResetIterationControl();
                    yield break;
                }
                if (context.IsContinuing())
                {
                    context.ResetIterationControl();
                    continue;
                }

                yield return argument;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    class BaseResultEnumerable : IEnumerable<SearchItem>
    {
        protected ArgumentEnumerable m_ArgumentEnumerable;
        protected bool m_SkipNullResult;

        protected SearchExpressionContext context => m_ArgumentEnumerable.context;

        public BaseResultEnumerable(ArgumentEnumerable argumentEnumerable, bool skipNull = true)
        {
            m_ArgumentEnumerable = argumentEnumerable;
            m_SkipNullResult = skipNull;
        }

        public virtual IEnumerator<SearchItem> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    class ResultEnumeration : BaseResultEnumerable
    {
        OnArgumentResult m_OnArgumentResult;

        public ResultEnumeration(ArgumentEnumerable argumentEnumerable, OnArgumentResult onArgumentResult, bool skipNull = true)
            : base(argumentEnumerable, skipNull)
        {
            m_OnArgumentResult = onArgumentResult;
        }

        public override IEnumerator<SearchItem> GetEnumerator()
        {
            foreach (var argument in m_ArgumentEnumerable)
            {
                var results = argument.Execute(context);
                if (results == null)
                    yield break;

                foreach (var searchItem in results)
                {
                    if (m_SkipNullResult && searchItem == null)
                    {
                        yield return null;
                        continue;
                    }

                    var resultingItem = m_OnArgumentResult(context.runtime.current, searchItem);
                    if (context.IsBreaking())
                    {
                        context.ResetIterationControl();
                        yield return null;
                        break;
                    }
                    if (context.IsContinuing())
                    {
                        context.ResetIterationControl();
                        yield return null;
                        continue;
                    }

                    yield return resultingItem;
                }
            }
        }
    }

    abstract class BaseResultAggregation<TAggregator> : BaseResultEnumerable
    {
        OnAggregateToSearchItems<TAggregator> m_OnAggregateToSearchItems;
        OnAggregateToSearchItem<TAggregator> m_OnAggregateToSearchItem;

        protected TAggregator m_Aggregator;

        protected BaseResultAggregation(ArgumentEnumerable argumentEnumerable, TAggregator aggregator, OnAggregateToSearchItems<TAggregator> onAggregateToSearchItems, bool skipNull = true)
            : base(argumentEnumerable, skipNull)
        {
            m_Aggregator = aggregator;
            m_OnAggregateToSearchItems = onAggregateToSearchItems;
            m_OnAggregateToSearchItem = null;
        }

        protected BaseResultAggregation(ArgumentEnumerable argumentEnumerable, TAggregator aggregator, OnAggregateToSearchItem<TAggregator> onAggregateToSearchItem, bool skipNull = true)
            : base(argumentEnumerable, skipNull)
        {
            m_Aggregator = aggregator;
            m_OnAggregateToSearchItems = null;
            m_OnAggregateToSearchItem = onAggregateToSearchItem;
        }

        protected BaseResultAggregation(ArgumentEnumerable argumentEnumerable, TAggregator aggregator, bool skipNull = true)
            : base(argumentEnumerable, skipNull)
        {
            m_Aggregator = aggregator;
            m_OnAggregateToSearchItems = null;
            m_OnAggregateToSearchItem = null;
        }

        protected abstract void OnAggregateStart(SearchExpressionContext currentContext);
        protected abstract void OnAggregateResult(SearchExpressionContext currentContext, SearchItem currentItem);

        public override IEnumerator<SearchItem> GetEnumerator()
        {
            foreach (var argument in m_ArgumentEnumerable)
            {
                var results = argument.Execute(context);
                if (results == null)
                    yield break;

                bool started = false;
                SearchExpressionContext argContext = default;
                foreach (var searchItem in results)
                {
                    if (m_SkipNullResult && searchItem == null)
                    {
                        yield return null;
                        continue;
                    }

                    if (!started)
                    {
                        // We need to start yielding result in order for the evaluation context to be valid.
                        if (!context.valid)
                            context.ThrowError("Invalid argument context");
                        argContext = context.runtime.current;
                        OnAggregateStart(argContext);
                        started = true;
                    }

                    OnAggregateResult(argContext, searchItem);
                    yield return null;
                }

                if (m_OnAggregateToSearchItem != null)
                {
                    yield return m_OnAggregateToSearchItem(argContext, m_Aggregator);
                    continue;
                }

                if (m_OnAggregateToSearchItems != null)
                {
                    foreach (var searchItem in m_OnAggregateToSearchItems(argContext, m_Aggregator))
                    {
                        yield return searchItem;
                    }
                    continue;
                }

                if (m_Aggregator is IEnumerable<SearchItem> searchItemAggregator)
                {
                    foreach (var searchItem in searchItemAggregator)
                    {
                        yield return searchItem;
                    }
                }
            }
        }
    }

    class ResultAggregation<TAggregator> : BaseResultAggregation<TAggregator>
        where TAggregator : class
    {
        OnAggregateResult<TAggregator> m_OnAggregateResult;
        OnAggregateStart<TAggregator> m_OnAggregateStart;

        public ResultAggregation(ArgumentEnumerable argumentEnumerable, OnAggregateResult<TAggregator> onAggregateResult, TAggregator aggregator, OnAggregateToSearchItems<TAggregator> onAggregateToSearchItems, bool skipNull = true)
            : base(argumentEnumerable, aggregator, onAggregateToSearchItems, skipNull)
        {
            m_OnAggregateResult = onAggregateResult;
        }

        public ResultAggregation(ArgumentEnumerable argumentEnumerable, OnAggregateResult<TAggregator> onAggregateResult, TAggregator aggregator, OnAggregateToSearchItem<TAggregator> onAggregateToSearchItem, bool skipNull = true)
            : base(argumentEnumerable, aggregator, onAggregateToSearchItem, skipNull)
        {
            m_OnAggregateResult = onAggregateResult;
        }

        public ResultAggregation(ArgumentEnumerable argumentEnumerable, OnAggregateResult<TAggregator> onAggregateResult, TAggregator aggregator, bool skipNull = true)
            : base(argumentEnumerable, aggregator, skipNull)
        {
            m_OnAggregateResult = onAggregateResult;
        }

        public ResultAggregation(ArgumentEnumerable argumentEnumerable, OnAggregateStart<TAggregator> onAggregateStart, OnAggregateResult<TAggregator> onAggregateResult, TAggregator aggregator, OnAggregateToSearchItems<TAggregator> onAggregateToSearchItems, bool skipNull = true)
            : this(argumentEnumerable, onAggregateResult, aggregator, onAggregateToSearchItems)
        {
            m_OnAggregateStart = onAggregateStart;
        }

        public ResultAggregation(ArgumentEnumerable argumentEnumerable, OnAggregateStart<TAggregator> onAggregateStart, OnAggregateResult<TAggregator> onAggregateResult, TAggregator aggregator, OnAggregateToSearchItem<TAggregator> onAggregateToSearchItem, bool skipNull = true)
            : this(argumentEnumerable, onAggregateResult, aggregator, onAggregateToSearchItem, skipNull)
        {
            m_OnAggregateStart = onAggregateStart;
        }

        public ResultAggregation(ArgumentEnumerable argumentEnumerable, OnAggregateStart<TAggregator> onAggregateStart, OnAggregateResult<TAggregator> onAggregateResult, TAggregator aggregator, bool skipNull = true)
            : this(argumentEnumerable, onAggregateResult, aggregator, skipNull)
        {
            m_OnAggregateStart = onAggregateStart;
        }

        protected override void OnAggregateStart(SearchExpressionContext currentContext)
        {
            m_OnAggregateStart?.Invoke(currentContext, m_Aggregator);
        }

        protected override void OnAggregateResult(SearchExpressionContext currentContext, SearchItem currentItem)
        {
            m_OnAggregateResult(currentContext, currentItem, m_Aggregator);
        }
    }

    class ResultPrimitiveAggregation<TAggregator> : BaseResultAggregation<TAggregator>
        where TAggregator : struct
    {
        OnAggregatePrimitiveResult<TAggregator> m_OnAggregateResult;
        OnAggregatePrimitiveStart<TAggregator> m_OnAggregateStart;

        TAggregator m_StartValue;

        public ResultPrimitiveAggregation(ArgumentEnumerable argumentEnumerable, OnAggregatePrimitiveResult<TAggregator> onAggregateResult, TAggregator aggregator, OnAggregateToSearchItems<TAggregator> onAggregateToSearchItems, bool skipNull = true)
            : base(argumentEnumerable, aggregator, onAggregateToSearchItems, skipNull)
        {
            m_OnAggregateResult = onAggregateResult;
            m_StartValue = aggregator;
        }

        public ResultPrimitiveAggregation(ArgumentEnumerable argumentEnumerable, OnAggregatePrimitiveResult<TAggregator> onAggregateResult, TAggregator aggregator, OnAggregateToSearchItem<TAggregator> onAggregateToSearchItem, bool skipNull = true)
            : base(argumentEnumerable, aggregator, onAggregateToSearchItem, skipNull)
        {
            m_OnAggregateResult = onAggregateResult;
            m_StartValue = aggregator;
        }

        public ResultPrimitiveAggregation(ArgumentEnumerable argumentEnumerable, OnAggregatePrimitiveStart<TAggregator> onAggregateStart, OnAggregatePrimitiveResult<TAggregator> onAggregateResult, TAggregator aggregator, OnAggregateToSearchItems<TAggregator> onAggregateToSearchItems, bool skipNull = true)
            : this(argumentEnumerable, onAggregateResult, aggregator, onAggregateToSearchItems)
        {
            m_OnAggregateStart = onAggregateStart;
        }

        public ResultPrimitiveAggregation(ArgumentEnumerable argumentEnumerable, OnAggregatePrimitiveStart<TAggregator> onAggregateStart, OnAggregatePrimitiveResult<TAggregator> onAggregateResult, TAggregator aggregator, OnAggregateToSearchItem<TAggregator> onAggregateToSearchItem, bool skipNull = true)
            : this(argumentEnumerable, onAggregateResult, aggregator, onAggregateToSearchItem, skipNull)
        {
            m_OnAggregateStart = onAggregateStart;
        }

        protected override void OnAggregateStart(SearchExpressionContext currentContext)
        {
            if (m_OnAggregateStart != null)
                m_Aggregator = m_OnAggregateStart(currentContext, m_Aggregator);
            else
                m_Aggregator = m_StartValue;
        }

        protected override void OnAggregateResult(SearchExpressionContext currentContext, SearchItem currentItem)
        {
            m_Aggregator = m_OnAggregateResult(currentContext, currentItem, m_Aggregator);
        }
    }
}
