// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace Unity.GraphToolkit.Editor
{
    class UserConditionViewBuilderLookup
    {
        const BindingFlags k_ConstructorBindingFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        readonly Dictionary<Type, ConstructorInfo> m_ConstructorCache = new();

        public IUserConditionView Build(Condition condition, IConditionView view)
        {
            if (condition == null)
                return null;

            var conditionType = condition.GetType();

            if (!m_ConstructorCache.TryGetValue(conditionType, out var constructor))
            {
                constructor = FindConstructor(conditionType);
                m_ConstructorCache[conditionType] = constructor;
            }

            if (constructor == null)
                return null;

            var instance = (IUserConditionView)constructor.Invoke(Array.Empty<object>());
            instance.Initialize(condition, view);
            return instance;
        }

        public void Clear()
        {
            m_ConstructorCache.Clear();
        }

        static ConstructorInfo FindConstructor(Type conditionType)
        {
            var current = conditionType;
            while (current != null && typeof(Condition).IsAssignableFrom(current))
            {
                foreach (var candidate in TypeCache.GetTypesDerivedFrom<IUserConditionView>())
                {
                    if (candidate.IsAbstract || candidate.IsGenericTypeDefinition)
                        continue;

                    if (!IsConditionViewOf(candidate, current))
                        continue;

                    var constructor = candidate.GetConstructor(
                        k_ConstructorBindingFlags,
                        null,
                        Type.EmptyTypes,
                        null);

                    if (constructor != null)
                        return constructor;
                }

                current = current.BaseType;
            }

            return null;
        }

        static bool IsConditionViewOf(Type candidate, Type conditionType)
        {
            var t = candidate;
            while (t != null && t != typeof(object))
            {
                if (t.IsGenericType
                    && t.GetGenericTypeDefinition() == typeof(ConditionView<>)
                    && t.GetGenericArguments()[0] == conditionType)
                    return true;
                t = t.BaseType;
            }
            return false;
        }

        internal class TestAccess
        {
            readonly UserConditionViewBuilderLookup m_Lookup;

            public TestAccess(UserConditionViewBuilderLookup lookup)
            {
                m_Lookup = lookup;
            }

            public int ConstructorCacheCount => m_Lookup.m_ConstructorCache.Count;

            public bool TryGetCachedConstructor(Type conditionType, out ConstructorInfo constructor)
            {
                return m_Lookup.m_ConstructorCache.TryGetValue(conditionType, out constructor);
            }
        }
    }
}
