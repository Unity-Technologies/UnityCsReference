// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Search
{
    abstract class DefaultQueryFilterHandlerBase<TData>
    {
        Action<IFilter> m_OnCreateFilter;

        protected DefaultQueryFilterHandlerBase(Action<IFilter> onCreateFilter)
        {
            m_OnCreateFilter = onCreateFilter;
        }

        public IFilter CreateFilter(string filterToken, QueryEngineImpl<TData> engineImp)
        {
            var filter = CreateFilter_Internal(filterToken, engineImp);
            m_OnCreateFilter?.Invoke(filter);
            return filter;
        }

        protected abstract IFilter CreateFilter_Internal(string filterToken, QueryEngineImpl<TData> engineImp);
    }

    class DefaultQueryFilterHandler<TData, TFilter> : DefaultQueryFilterHandlerBase<TData>
    {
        Func<TData, string, TFilter> m_GetDataFunc;

        public DefaultQueryFilterHandler(Func<TData, string, TFilter> defaultGetDataFunc)
            : base(null)
        {
            m_GetDataFunc = defaultGetDataFunc;
        }

        public DefaultQueryFilterHandler(Func<TData, string, TFilter> defaultGetDataFunc, Action<IFilter> onCreateFilter)
            : base(onCreateFilter)
        {
            m_GetDataFunc = defaultGetDataFunc;
        }

        protected override IFilter CreateFilter_Internal(string filterToken, QueryEngineImpl<TData> engineImp)
        {
            return new DefaultFilter<TData, TFilter>(filterToken, m_GetDataFunc, engineImp);
        }
    }

    class DefaultQueryFilterResolverHandler<TData, TFilter> : DefaultQueryFilterHandlerBase<TData>
    {
        Func<TData, string, string, TFilter, bool> m_FilterResolver;

        public DefaultQueryFilterResolverHandler(Func<TData, string, string, TFilter, bool> defaultFilterResolver)
        : base(null)
        {
            m_FilterResolver = defaultFilterResolver;
        }

        public DefaultQueryFilterResolverHandler(Func<TData, string, string, TFilter, bool> defaultFilterResolver, Action<IFilter> onCreateFilter)
            : base(onCreateFilter)
        {
            m_FilterResolver = defaultFilterResolver;
        }

        protected override IFilter CreateFilter_Internal(string filterToken, QueryEngineImpl<TData> engineImp)
        {
            return new DefaultFilterResolver<TData, TFilter>(filterToken, m_FilterResolver, engineImp);
        }
    }

    class DefaultQueryParamFilterHandler<TData, TParam, TFilter> : DefaultQueryFilterHandlerBase<TData>
    {
        Func<TData, string, TParam, TFilter> m_GetDataFunc;
        Func<string, TParam> m_ParamTransformer;

        public DefaultQueryParamFilterHandler(Func<TData, string, TParam, TFilter> defaultGetDataFunc)
            : base(null)
        {
            m_GetDataFunc = defaultGetDataFunc;
        }

        public DefaultQueryParamFilterHandler(Func<TData, string, TParam, TFilter> defaultGetDataFunc, Action<IFilter> onCreateFilter)
            : base(onCreateFilter)
        {
            m_GetDataFunc = defaultGetDataFunc;
        }

        public DefaultQueryParamFilterHandler(Func<TData, string, TParam, TFilter> defaultGetDataFunc, Func<string, TParam> paramTransformer)
            : this(defaultGetDataFunc)
        {
            m_ParamTransformer = paramTransformer;
        }

        public DefaultQueryParamFilterHandler(Func<TData, string, TParam, TFilter> defaultGetDataFunc, Func<string, TParam> paramTransformer, Action<IFilter> onCreateFilter)
            : this(defaultGetDataFunc, onCreateFilter)
        {
            m_ParamTransformer = paramTransformer;
        }

        protected override IFilter CreateFilter_Internal(string filterToken, QueryEngineImpl<TData> engineImp)
        {
            return new DefaultParamFilter<TData, TParam, TFilter>(filterToken, m_GetDataFunc, m_ParamTransformer, engineImp);
        }
    }

    class DefaultQueryParamFilterResolverHandler<TData, TParam, TFilter> : DefaultQueryFilterHandlerBase<TData>
    {
        Func<TData, string, TParam, string, TFilter, bool> m_FilterResolver;
        Func<string, TParam> m_ParamTransformer;

        public DefaultQueryParamFilterResolverHandler(Func<TData, string, TParam, string, TFilter, bool> defaultFilterResolver)
            : base(null)
        {
            m_FilterResolver = defaultFilterResolver;
        }

        public DefaultQueryParamFilterResolverHandler(Func<TData, string, TParam, string, TFilter, bool> defaultFilterResolver, Action<IFilter> onCreateFilter)
            : base(onCreateFilter)
        {
            m_FilterResolver = defaultFilterResolver;
        }

        public DefaultQueryParamFilterResolverHandler(Func<TData, string, TParam, string, TFilter, bool> defaultFilterResolver, Func<string, TParam> paramTransformer)
            : this(defaultFilterResolver)
        {
            m_ParamTransformer = paramTransformer;
        }

        public DefaultQueryParamFilterResolverHandler(Func<TData, string, TParam, string, TFilter, bool> defaultFilterResolver, Func<string, TParam> paramTransformer, Action<IFilter> onCreateFilter)
            : this(defaultFilterResolver, onCreateFilter)
        {
            m_ParamTransformer = paramTransformer;
        }

        protected override IFilter CreateFilter_Internal(string filterToken, QueryEngineImpl<TData> engineImp)
        {
            return new DefaultParamFilterResolver<TData, TParam, TFilter>(filterToken, m_FilterResolver, m_ParamTransformer, engineImp);
        }
    }
}
