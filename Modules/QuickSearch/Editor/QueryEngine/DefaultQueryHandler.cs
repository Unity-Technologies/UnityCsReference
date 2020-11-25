// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.Search
{
    class DefaultQueryHandlerFactory<TData> : IQueryHandlerFactory<TData, DefaultQueryHandler<TData>, IEnumerable<TData>>
    {
        QueryEngine<TData> m_Engine;
        bool m_FastYielding;

        public DefaultQueryHandlerFactory(QueryEngine<TData> engine, bool fastYielding = false)
        {
            m_Engine = engine;
            m_FastYielding = fastYielding;
        }

        public DefaultQueryHandler<TData> Create(QueryGraph graph, ICollection<QueryError> errors)
        {
            if (errors.Count > 0)
                return null;
            var dataWalker = new DefaultQueryHandler<TData>();
            dataWalker.Initialize(m_Engine, graph, errors, m_FastYielding);
            return dataWalker;
        }
    }

    class DefaultQueryHandler<TData> : IQueryHandler<TData, IEnumerable<TData>>
    {
        IQueryEnumerable<TData> m_QueryEnumerable;

        public void Initialize(QueryEngine<TData> engine, QueryGraph graph, ICollection<QueryError> errors, bool fastYielding)
        {
            if (graph != null && !graph.empty)
                m_QueryEnumerable = EnumerableCreator.Create(graph.root, engine, errors, fastYielding);
        }

        public IEnumerable<TData> Eval(IEnumerable<TData> payload)
        {
            if (payload == null || m_QueryEnumerable == null)
                return new TData[] {};
            m_QueryEnumerable.SetPayload(payload);
            return m_QueryEnumerable;
        }

        public bool Eval(TData element)
        {
            if (element == null || !(m_QueryEnumerable is WhereEnumerable<TData> we))
                return false;

            return we.predicate != null && we.predicate(element);
        }
    }
}
