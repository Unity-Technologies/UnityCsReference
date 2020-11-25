// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Search
{
    interface IQueryEnumerable<T> : IEnumerable<T>
    {
        void SetPayload(IEnumerable<T> payload);

        bool fastYielding { get; }
        IEnumerator<T> FastYieldingEnumerator();
    }

    interface IQueryEnumerableFactory
    {
        IQueryEnumerable<T> Create<T>(IQueryNode root, QueryEngine<T> engine, ICollection<QueryError> errors, bool fastYielding);
    }

    [AttributeUsage(AttributeTargets.Class)]
    class EnumerableCreatorAttribute : Attribute
    {
        public QueryNodeType nodeType { get; }

        public EnumerableCreatorAttribute(QueryNodeType nodeType)
        {
            this.nodeType = nodeType;
        }
    }

    static class EnumerableCreator
    {
        static Dictionary<QueryNodeType, IQueryEnumerableFactory> s_EnumerableFactories;

        [InitializeOnLoadMethod]
        public static void Init()
        {
            s_EnumerableFactories = new Dictionary<QueryNodeType, IQueryEnumerableFactory>();
            var factoryTypes = TypeCache.GetTypesWithAttribute<EnumerableCreatorAttribute>().Where(t => typeof(IQueryEnumerableFactory).IsAssignableFrom(t));
            foreach (var factoryType in factoryTypes)
            {
                var enumerableCreatorAttribute = factoryType.GetCustomAttributes(typeof(EnumerableCreatorAttribute), false).Cast<EnumerableCreatorAttribute>().FirstOrDefault();
                if (enumerableCreatorAttribute == null)
                    continue;

                var nodeType = enumerableCreatorAttribute.nodeType;
                var factory = Activator.CreateInstance(factoryType) as IQueryEnumerableFactory;
                if (factory == null)
                    continue;

                if (s_EnumerableFactories.ContainsKey(nodeType))
                {
                    Debug.LogWarning($"Factory for node type {nodeType} already exists.");
                    continue;
                }
                s_EnumerableFactories.Add(nodeType, factory);
            }
        }

        public static IQueryEnumerable<T> Create<T>(IQueryNode root, QueryEngine<T> engine, ICollection<QueryError> errors, bool fastYielding)
        {
            return s_EnumerableFactories.TryGetValue(root.type, out var factory) ? factory.Create<T>(root, engine, errors, fastYielding) : null;
        }
    }
}
