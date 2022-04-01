// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Assertions;

namespace UnityEditor.Search
{
    class WhereEnumerable<T> : IQueryEnumerable<T>
    {
        IEnumerable<T> m_Payload;

        public Func<T, bool> predicate { get; }
        public bool fastYielding { get; }

        public WhereEnumerable(Func<T, bool> predicate, bool fastYielding)
        {
            this.predicate = predicate;
            this.fastYielding = fastYielding;
        }

        public void SetPayload(IEnumerable<T> payload)
        {
            m_Payload = payload;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (fastYielding)
                return FastYieldingEnumerator();
            return m_Payload.Where(e => e != null && predicate(e)).GetEnumerator();
        }

        public IEnumerator<T> FastYieldingEnumerator()
        {
            foreach (var element in m_Payload)
            {
                if (element == null)
                    yield return default;

                if (predicate(element))
                    yield return element;
                else
                    yield return default;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    [EnumerableCreator(QueryNodeType.Where)]
    class WhereEnumerableFactory : IQueryEnumerableFactory
    {
        public IQueryEnumerable<T> Create<T>(IQueryNode root, QueryEngine<T> engine, ICollection<QueryError> errors, bool fastYielding)
        {
            if (root.leaf || root.children == null || root.children.Count != 1)
            {
                errors.Add(new QueryError(root.token.position, root.token.length, "Where node must have a child."));
                return null;
            }

            var predicateGraphRoot = root.children[0];

            var predicate = BuildFunctionFromNode(predicateGraphRoot, engine, errors, fastYielding);
            var whereEnumerable = new WhereEnumerable<T>(predicate, fastYielding);
            return whereEnumerable;
        }

        private Func<T, bool> BuildFunctionFromNode<T>(IQueryNode node, QueryEngine<T> engine, ICollection<QueryError> errors, bool fastYielding)
        {
            Func<T, bool> noOp = o => false;

            if (node == null)
                return noOp;

            switch (node.type)
            {
                case QueryNodeType.And:
                {
                    Assert.IsFalse(node.leaf, "And node cannot be leaf.");
                    var leftFunc = BuildFunctionFromNode(node.children[0], engine, errors, fastYielding);
                    var rightFunc = BuildFunctionFromNode(node.children[1], engine, errors, fastYielding);
                    return o => leftFunc(o) && rightFunc(o);
                }
                case QueryNodeType.Or:
                {
                    Assert.IsFalse(node.leaf, "Or node cannot be leaf.");
                    var leftFunc = BuildFunctionFromNode(node.children[0], engine, errors, fastYielding);
                    var rightFunc = BuildFunctionFromNode(node.children[1], engine, errors, fastYielding);
                    return o => leftFunc(o) || rightFunc(o);
                }
                case QueryNodeType.Not:
                {
                    Assert.IsFalse(node.leaf, "Not node cannot be leaf.");
                    var childFunc = BuildFunctionFromNode(node.children[0], engine, errors, fastYielding);
                    return o => !childFunc(o);
                }
                case QueryNodeType.Filter:
                {
                    var filterNode = node as FilterNode;
                    if (filterNode == null)
                        return noOp;
                    var filterOperation = GenerateFilterOperation(filterNode, engine, errors);
                    if (filterOperation == null)
                        return noOp;
                    return o => filterOperation.Match(o);
                }
                case QueryNodeType.Search:
                {
                    if (engine.searchDataCallback == null)
                        return o => false;
                    var searchNode = node as SearchNode;
                    Assert.IsNotNull(searchNode);
                    Func<string, bool> matchWordFunc;
                    var stringComparison = engine.globalStringComparison;
                    if (engine.searchDataOverridesStringComparison)
                        stringComparison = engine.searchDataStringComparison;

                    if (engine.searchWordMatcher != null)
                        matchWordFunc = s => engine.searchWordMatcher(searchNode.searchValue, searchNode.exact, stringComparison, s);
                    else
                    {
                        if (searchNode.exact)
                            matchWordFunc = s => s != null && s.Equals(searchNode.searchValue, stringComparison);
                        else
                            matchWordFunc = s => s != null && s.IndexOf(searchNode.searchValue, stringComparison) >= 0;
                    }
                    return o => engine.searchDataCallback(o).Any(data => matchWordFunc(data));
                }
                case QueryNodeType.FilterIn:
                {
                    var filterNode = node as InFilterNode;
                    if (filterNode == null)
                        return noOp;
                    var filterOperation = GenerateFilterOperation(filterNode, engine, errors);
                    if (filterOperation == null)
                        return noOp;
                    var inFilterFunction = GenerateInFilterFunction(filterNode, filterOperation, engine, errors, fastYielding);
                    return inFilterFunction;
                }
            }

            return noOp;
        }

        private static BaseFilterOperation<T> GenerateFilterOperation<T>(FilterNode node, QueryEngine<T> engine, ICollection<QueryError> errors)
        {
            var operatorIndex = node.token.position + node.filter.token.Length + (node.paramValueStringView.IsNullOrEmpty() ? 0 : node.paramValueStringView.length);
            var filterValueIndex = operatorIndex + node.op.token.Length;

            Type filterValueType;
            IParseResult parseResult = null;
            if (QueryEngineUtils.IsNestedQueryToken(node.filterValueStringView))
            {
                if (node.filter?.nestedQueryHandlerTransformer == null)
                {
                    errors.Add(new QueryError(filterValueIndex, node.filterValueStringView.length, $"No nested query handler transformer set on filter \"{node.filter.token}\"."));
                    return null;
                }
                filterValueType = node.filter.nestedQueryHandlerTransformer.rightHandSideType;
            }
            else
            {
                parseResult = engine.ParseFilterValue(node.filterValue, node.filter, in node.op, out filterValueType);
                if (!parseResult.success)
                {
                    errors.Add(new QueryError(filterValueIndex, node.filterValueStringView.length, $"The value \"{node.filterValue}\" could not be converted to any of the supported handler types."));
                    return null;
                }
            }

            IFilterOperationGenerator generator = engine.GetGeneratorForType(filterValueType);
            if (generator == null)
            {
                errors.Add(new QueryError(filterValueIndex, node.filterValueStringView.length, $"Unknown type \"{filterValueType}\". Did you set an operator handler for this type?"));
                return null;
            }

            var generatorData = new FilterOperationGeneratorData
            {
                filterName = node.filterId,
                filterValue = node.filterValueStringView,
                filterValueParseResult = parseResult,
                globalStringComparison = engine.globalStringComparison,
                op = node.op,
                paramValue = node.paramValueStringView,
                generator = generator
            };
            var operation = node.filter.GenerateOperation(generatorData, operatorIndex, errors);
            return operation as BaseFilterOperation<T>;
        }

        private Func<T, bool> GenerateInFilterFunction<T>(InFilterNode node, BaseFilterOperation<T> filterOperation, QueryEngine<T> engine, ICollection<QueryError> errors, bool fastYielding)
        {
            if (node.leaf || node.children == null || node.children.Count == 0)
            {
                errors.Add(new QueryError(node.token.position, node.token.length, "InFilter node cannot be a leaf."));
                return null;
            }

            var nestedQueryType = GetNestedQueryType(node);
            if (nestedQueryType == null)
            {
                errors.Add(new QueryError(node.token.position, node.token.length, "Could not deduce nested query type. Did you forget to set the nested query handler?"));
                return null;
            }

            var transformType = node.filter.nestedQueryHandlerTransformer.rightHandSideType;

            var inFilterFunc = typeof(WhereEnumerableFactory)
                .GetMethod("GenerateInFilterFunctionWithTypes", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                ?.MakeGenericMethod(typeof(T), nestedQueryType, transformType)
                ?.Invoke(this, new object[] { node, filterOperation, engine, errors, fastYielding }) as Func<T, bool>;

            if (inFilterFunc == null)
            {
                errors.Add(new QueryError(node.token.position, node.token.length, "Could not create filter function with nested query."));
                return null;
            }

            return inFilterFunc;
        }

        private Func<T, bool> GenerateInFilterFunctionWithTypes<T, TNested, TTransform>(InFilterNode node, BaseFilterOperation<T> filterOperation, QueryEngine<T> engine, ICollection<QueryError> errors, bool fastYielding)
        {
            var nestedQueryEnumerable = EnumerableCreator.Create<TNested>(node.children[0], null, errors, fastYielding);
            if (nestedQueryEnumerable == null)
                return null;
            var nestedQueryTransformer = node.filter.nestedQueryHandlerTransformer as NestedQueryHandlerTransformer<TNested, TTransform>;
            if (nestedQueryTransformer == null)
                return null;
            var transformerFunction = nestedQueryTransformer.handler;
            var dynamicFilterOperation = filterOperation as IDynamicFilterOperation<TTransform>;
            if (dynamicFilterOperation == null)
                return null;
            return o =>
            {
                foreach (var item in nestedQueryEnumerable)
                {
                    var transformedValue = transformerFunction(item);
                    dynamicFilterOperation.SetFilterValue(transformedValue);
                    if (filterOperation.Match(o))
                        return true;
                }

                return false;
            };
        }

        private static Type GetNestedQueryType(IQueryNode node)
        {
            if (node.type == QueryNodeType.NestedQuery)
            {
                var nn = node as NestedQueryNode;
                return nn?.nestedQueryHandler.enumerableType;
            }

            if (node.leaf || node.children == null)
                return null;

            foreach (var child in node.children)
            {
                var type = GetNestedQueryType(child);
                if (type != null)
                    return type;
            }

            return null;
        }
    }
}
