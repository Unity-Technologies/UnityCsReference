// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Scripting.ScriptCompilation
{
    class VersionRangesFactory<TVersion> where TVersion : struct, IVersion<TVersion>
    {
        VersionRanges<TVersion> m_VersionRanges;

        public VersionRangesFactory()
        {
            m_VersionRanges = new VersionRanges<TVersion>(ExpressionTypeFactory<TVersion>.Create());
        }

        public VersionDefineExpression<TVersion> GetExpression(string expression)
        {
            return m_VersionRanges.GetExpression(expression);
        }
    }

    class CachedVersionRangesFactory<TVersion> where TVersion : struct, IVersion<TVersion>
    {
        private VersionRangesFactory<TVersion> m_InnerFactory = new VersionRangesFactory<TVersion>();

        private readonly Dictionary<string, CacheEntry> m_Cache =
            new Dictionary<string, CacheEntry>();


        public CacheEntry GetExpression(string expression)
        {
            lock (m_Cache)
            {
                if (m_Cache.TryGetValue(expression, out var result))
                {
                    return result;
                }

                try
                {
                    var entry = new CacheEntry(m_InnerFactory.GetExpression(expression));
                    m_Cache[expression] = entry;
                    return entry;
                }
                catch (ExpressionNotValidException ex)
                {
                    var entry = new CacheEntry(ex);
                    m_Cache[expression] = entry;
                    return entry;
                }
            }
        }

        public void Clear()
        {
            lock (m_Cache)
            {
                m_Cache.Clear();
                m_InnerFactory = new VersionRangesFactory<TVersion>();
            }
        }

        public readonly struct CacheEntry
        {
            public VersionDefineExpression<TVersion> Expression { get; }
            public ExpressionNotValidException ValidationError { get; }

            public CacheEntry(VersionDefineExpression<TVersion> expression)
            {
                Expression = expression;
                ValidationError = null;
            }

            public CacheEntry(ExpressionNotValidException validationError)
            {
                Expression = VersionDefineExpression<TVersion>.Invalid;
                ValidationError = validationError;
            }
        }
    }
}
