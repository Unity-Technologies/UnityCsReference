// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Search
{
    public partial class QueryEngine<TData>
    {
        [Obsolete("Parse has been deprecated. Use ParseQuery instead (UnityUpgradable) -> ParseQuery(*)")]
        public Query<TData> Parse(string text, bool useFastYieldingQueryHandler = false)
        {
            var handlerFactory = new DefaultQueryHandlerFactory<TData>(this, useFastYieldingQueryHandler);
            var pq = BuildQuery(text, handlerFactory, (evalGraph, queryGraph, errors, tokens, toggles, handler) => new ParsedQuery<TData>(text, evalGraph, queryGraph, errors, tokens, toggles, handler));
            return new Query<TData>(pq.text, pq.evaluationGraph, pq.queryGraph, pq.errors, pq.tokens, pq.toggles, pq.graphHandler);
        }

        [Obsolete("Parse has been deprecated. Use ParseQuery instead (UnityUpgradable) -> ParseQuery<TQueryHandler, TPayload>(*)")]
        public Query<TData, TPayload> Parse<TQueryHandler, TPayload>(string text, IQueryHandlerFactory<TData, TQueryHandler, TPayload> queryHandlerFactory)
            where TQueryHandler : IQueryHandler<TData, TPayload>
            where TPayload : class
        {
            var pq = BuildQuery(text, queryHandlerFactory, (evalGraph, queryGraph, errors, tokens, toggles, handler) => new ParsedQuery<TData, TPayload>(text, evalGraph, queryGraph, errors, tokens, toggles, handler));
            return new Query<TData, TPayload>(pq.text, pq.evaluationGraph, pq.queryGraph, pq.errors, pq.tokens, pq.toggles, pq.graphHandler);
        }
    }
}
