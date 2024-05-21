// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Search
{
    abstract class DefaultQueryFilterHandlerBase<TData>
    {
        public abstract IFilter CreateFilter(string filterToken, QueryEngineImpl<TData> engineImp);
    }

    class DefaultQueryFilterHandler<TData, TFilter> : DefaultQueryFilterHandlerBase<TData>
    {
        Func<TData, string, TFilter> m_GetDataFunc;

        public DefaultQueryFilterHandler(Func<TData, string, TFilter> defaultGetDataFunc)
        {
            m_GetDataFunc = defaultGetDataFunc;
        }

        public override IFilter CreateFilter(string filterToken, QueryEngineImpl<TData> engineImp)
        {
            return new DefaultFilter<TData, TFilter>(filterToken, m_GetDataFunc, engineImp);
        }
    }

    class DefaultQueryFilterResolverHandler<TData, TFilter> : DefaultQueryFilterHandlerBase<TData>
    {
        Func<TData, string, string, TFilter, bool> m_FilterResolver;

        public DefaultQueryFilterResolverHandler(Func<TData, string, string, TFilter, bool> defaultFilterResolver)
        {
            m_FilterResolver = defaultFilterResolver;
        }

        public override IFilter CreateFilter(string filterToken, QueryEngineImpl<TData> engineImp)
        {
            return new DefaultFilterResolver<TData, TFilter>(filterToken, m_FilterResolver, engineImp);
        }
    }

    class DefaultQueryParamFilterHandler<TData, TParam, TFilter> : DefaultQueryFilterHandlerBase<TData>
    {
        Func<TData, string, TParam, TFilter> m_GetDataFunc;
        Func<string, TParam> m_ParamTransformer;

        public DefaultQueryParamFilterHandler(Func<TData, string, TParam, TFilter> defaultGetDataFunc)
        {
            m_GetDataFunc = defaultGetDataFunc;
        }

        public DefaultQueryParamFilterHandler(Func<TData, string, TParam, TFilter> defaultGetDataFunc, Func<string, TParam> paramTransformer)
            : this(defaultGetDataFunc)
        {
            m_ParamTransformer = paramTransformer;
        }

        public override IFilter CreateFilter(string filterToken, QueryEngineImpl<TData> engineImp)
        {
            return new DefaultParamFilter<TData, TParam, TFilter>(filterToken, m_GetDataFunc, m_ParamTransformer, engineImp);
        }
    }

    class DefaultQueryParamFilterResolverHandler<TData, TParam, TFilter> : DefaultQueryFilterHandlerBase<TData>
    {
        Func<TData, string, TParam, string, TFilter, bool> m_FilterResolver;
        Func<string, TParam> m_ParamTransformer;

        public DefaultQueryParamFilterResolverHandler(Func<TData, string, TParam, string, TFilter, bool> defaultFilterResolver)
        {
            m_FilterResolver = defaultFilterResolver;
        }

        public DefaultQueryParamFilterResolverHandler(Func<TData, string, TParam, string, TFilter, bool> defaultFilterResolver, Func<string, TParam> paramTransformer)
            : this(defaultFilterResolver)
        {
            m_ParamTransformer = paramTransformer;
        }

        public override IFilter CreateFilter(string filterToken, QueryEngineImpl<TData> engineImp)
        {
            return new DefaultParamFilterResolver<TData, TParam, TFilter>(filterToken, m_FilterResolver, m_ParamTransformer, engineImp);
        }
    }
}
