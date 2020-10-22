// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Search
{
    internal class DefaultQueryHandlerFactory<TData> : IQueryHandlerFactory<TData, DefaultQueryHandler<TData>, IEnumerable<TData>>
    {
        QueryEngine<TData> m_Engine;

        public DefaultQueryHandlerFactory(QueryEngine<TData> engine)
        {
            m_Engine = engine;
        }

        public DefaultQueryHandler<TData> Create(QueryGraph graph, ICollection<QueryError> errors)
        {
            if (errors.Count > 0)
                return null;
            var dataWalker = new DefaultQueryHandler<TData>();
            dataWalker.Initialize(m_Engine, graph, errors);
            return dataWalker;
        }
    }

    internal class DefaultQueryHandler<TData> : IQueryHandler<TData, IEnumerable<TData>>
    {
        IQueryEnumerable<TData> m_QueryEnumerable;

        public void Initialize(QueryEngine<TData> engine, QueryGraph graph, ICollection<QueryError> errors)
        {
            if (graph != null && !graph.empty)
                m_QueryEnumerable = EnumerableCreator.Create(graph.root, engine, errors);
        }

        public IEnumerable<TData> Eval(IEnumerable<TData> payload)
        {
            if (payload == null || m_QueryEnumerable == null)
                return new TData[] {};
            m_QueryEnumerable.SetPayload(payload);
            return m_QueryEnumerable;
        }
    }
}
