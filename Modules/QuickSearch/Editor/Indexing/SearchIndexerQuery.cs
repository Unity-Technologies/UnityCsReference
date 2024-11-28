// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace UnityEditor.Search
{
    enum Combine
    {
        None,
        Intersection,
        Union
    }

    class SearchIndexerQueryFactory : SearchQueryEvaluatorFactory<SearchResult>
    {
        public SearchIndexerQueryFactory(SearchQueryEvaluator<SearchResult>.EvalHandler handler)
            : base(handler)
        {}
    }

    class SearchQueryEvaluatorFactory<T> : IQueryHandlerFactory<T, SearchQueryEvaluator<T>, object>
    {
        SearchQueryEvaluator<T>.EvalHandler m_Handler;

        public SearchQueryEvaluatorFactory(SearchQueryEvaluator<T>.EvalHandler handler)
        {
            m_Handler = handler;
        }

        public SearchQueryEvaluator<T> Create(QueryGraph graph, ICollection<QueryError> errors)
        {
            ValidateGraph(graph.root, errors);
            return new SearchQueryEvaluator<T>(graph, m_Handler);
        }

        private static void ValidateGraph(IQueryNode root, ICollection<QueryError> errors)
        {
            if (root == null)
                return;

            ValidateNode(root, errors);
            if (root.leaf || root.children == null)
                return;

            foreach (var child in root.children)
            {
                ValidateGraph(child, errors);
            }
        }

        private static void ValidateNode(IQueryNode node, ICollection<QueryError> errors)
        {
            switch (node.type)
            {
                case QueryNodeType.Aggregator:
                case QueryNodeType.FilterIn:
                case QueryNodeType.Intersection:
                case QueryNodeType.Union:
                case QueryNodeType.NestedQuery:
                {
                    errors.Add(new QueryError(node.token.position, node.token.length, "Nested queries are not supported.", SearchQueryErrorType.Warning));
                    break;
                }
            }
        }
    }

    class SearchQueryEvaluator<T> : IQueryHandler<T, object>
    {
        private QueryGraph graph { get; set; }
        private EvalHandler m_Handler;

        public delegate EvalResult EvalHandler(EvalHandlerArgs args);

        public readonly struct EvalHandlerArgs
        {
            public readonly string name;
            public readonly object value;
            public readonly bool exclude;
            public readonly SearchIndexOperator op;

            public readonly IEnumerable<T> andSet;
            public readonly IEnumerable<T> orSet;

            public readonly object payload;

            public EvalHandlerArgs(string name, object value, SearchIndexOperator op, bool exclude,
                                   IEnumerable<T> andSet, IEnumerable<T> orSet, object payload)
            {
                this.name = name;
                this.value = value;
                this.op = op;
                this.exclude = exclude;
                this.andSet = andSet;
                this.orSet = orSet;
                this.payload = payload;
            }
        }

        public readonly struct EvalResult
        {
            public readonly bool combined;
            public readonly IEnumerable<T> results;

            public static EvalResult None = new EvalResult(false, null);

            public EvalResult(bool combined, IEnumerable<T> results = null)
            {
                this.combined = combined;
                this.results = results;
            }

            public static EvalResult Combined(IEnumerable<T> results)
            {
                return new EvalResult(true, results);
            }

            public static void Print(EvalHandlerArgs args, IEnumerable<T> results = null, SearchResultCollection subset = null, double elapsedTime = -1)
            {
                var combineString = GetCombineString(args.andSet, args.orSet);
                var elapsedTimeString = "";
                if (elapsedTime > 0)
                    elapsedTimeString = $"{elapsedTime:F2} ms";
                UnityEngine.Debug.LogFormat(UnityEngine.LogType.Log, UnityEngine.LogOption.None, null,
                    $"Eval -> {combineString} | op:{args.op,14} | exclude:{args.exclude,7} | " +
                    $"[{args.name,4}, {args.value,8}] | i:{GetAddress(args.andSet)} | a:{GetAddress(args.orSet)} | " +
                    $"h:{GetAddress(subset)} | results:{GetAddress(results)} | " + elapsedTimeString);
            }

            private static string GetCombineString(object inputSet, object addedSet)
            {
                var combineString = "  -  ";
                if (addedSet != null && inputSet != null)
                    combineString = "\u2229\u222A";
                else if (addedSet != null)
                    combineString = " \u2229 ";
                else if (inputSet != null)
                    combineString = " \u222A ";
                return combineString;
            }

            private static int GetHandle(object obj)
            {
                return obj.GetHashCode();
            }

            private static string GetAddress(object obj)
            {
                if (obj == null)
                    return "0x00000000";
                var addr = GetHandle(obj);
                return $"0x{addr.ToString("X8")}";
            }

            private static string GetAddress(ICollection<T> list)
            {
                if (list == null)
                    return "0x00000000";
                var addr = GetHandle(list);
                return $"({list.Count}) 0x{addr.ToString("X8")}";
            }
        }

        public SearchQueryEvaluator(QueryGraph graph, EvalHandler handler)
        {
            this.graph = graph;
            graph.Optimize(true, true);
            m_Handler = handler;
        }

        public IEnumerable<T> Eval(object payload)
        {
            if (graph.empty)
                return new List<T>();

            var root = BuildInstruction(graph.root, m_Handler, payload, false);
            root.Execute(Combine.None);
            if (root.results == null)
                return new List<T>();
            else
            {
                // TODO SearchIndexer Memory: is it needed to return a Distinct here? Should we use a local HashSet instead of having Distinct allocate a new set each time?
                return root.results.Distinct();
            }
        }

        public bool Eval(T element)
        {
            throw new System.NotSupportedException();
        }

        internal static Instruction BuildInstruction(IQueryNode node, EvalHandler eval, object payload, bool not)
        {
            switch (node.type)
            {
                case QueryNodeType.And:
                {
                    Assert.IsFalse(node.leaf, "And node cannot be leaf.");
                    if (!not)
                        return new AndInstruction(node, eval, payload, not);
                    else
                        return new OrInstruction(node, eval, payload, not);
                }
                case QueryNodeType.Or:
                {
                    Assert.IsFalse(node.leaf, "Or node cannot be leaf.");
                    if (!not)
                        return new OrInstruction(node, eval, payload, not);
                    else
                        return new AndInstruction(node, eval, payload, not);
                }
                case QueryNodeType.Not:
                {
                    Assert.IsFalse(node.leaf, "Not node cannot be leaf.");
                    Instruction instruction = BuildInstruction(node.children[0], eval, payload, !not);
                    return instruction;
                }
                case QueryNodeType.Filter:
                case QueryNodeType.Search:
                {
                    Assert.IsNotNull(node);
                    return new ResultInstruction(node, eval, payload, not);
                }
                case QueryNodeType.Where:
                {
                    Assert.IsFalse(node.leaf, "Where node cannot be leaf.");
                    return BuildInstruction(node.children[0], eval, payload, not);
                }
            }

            return null;
        }

        private static SearchIndexOperator ParseOperatorToken(string token)
        {
            switch (token)
            {
                case ":": return SearchIndexOperator.Contains;
                case "=": return SearchIndexOperator.Equal;
                case ">": return SearchIndexOperator.Greater;
                case ">=": return SearchIndexOperator.GreaterOrEqual;
                case "<": return SearchIndexOperator.Less;
                case "<=": return SearchIndexOperator.LessOrEqual;
                case "!=": return SearchIndexOperator.NotEqual;
            }
            return SearchIndexOperator.None;
        }

        internal abstract class Instruction : IInstruction<T>
        {
            public object payload;
            public IEnumerable<T> andSet; //not null for a AND
            public IEnumerable<T> orSet; //not null for a OR
            public IEnumerable<T> results;
            protected EvalHandler eval;

            public long estimatedCost { get; protected set; }

            public IQueryNode node { get; protected set; }

            public abstract bool Execute(Combine combine);
        }

        internal abstract class OperandInstruction : Instruction, IOperandInstruction<T>
        {
            public Instruction leftInstruction;
            public Instruction rightInstruction;
            public IInstruction<T> LeftInstruction => leftInstruction;
            public IInstruction<T> RightInstruction => rightInstruction;
            protected Combine combine;

            public OperandInstruction(IQueryNode node, EvalHandler eval, object payload, bool not)
            {
                leftInstruction = BuildInstruction(node.children[0], eval, payload, not);
                rightInstruction = BuildInstruction(node.children[1], eval, payload, not);
                this.eval = eval;
                this.payload = payload;
            }

            public override bool Execute(Combine combine)
            {
                // Pass the inputSet from the parent
                if (andSet != null)
                {
                    leftInstruction.andSet = andSet;
                }
                // Pass the addedSet from the parent (only added by the right instruction)
                if (orSet != null)
                {
                    rightInstruction.orSet = orSet;
                }
                bool combinedLeft = leftInstruction.Execute(combine);

                UpdateRightInstruction(leftInstruction.results);

                bool combinedRight = rightInstruction.Execute(this.combine);

                UpdateOutputSet(combinedLeft, combinedRight, leftInstruction.results, rightInstruction.results);
                return IsCombineHandled(combinedLeft, combinedRight);
            }

            internal abstract bool IsCombineHandled(bool combinedLeft, bool combinedRight);
            internal abstract void UpdateOutputSet(bool combinedLeft, bool combinedRight, IEnumerable<T> leftReturn, IEnumerable<T> rightReturn);
            internal abstract void UpdateRightInstruction(IEnumerable<T> leftReturn);
        }

        internal class AndInstruction : OperandInstruction, IAndInstruction<T>
        {
            public AndInstruction(IQueryNode node, EvalHandler eval, object dataSet, bool not) : base(node, eval, dataSet, not)
            {
                combine = Combine.Intersection;
            }

            internal override bool IsCombineHandled(bool combinedLeft, bool combinedRight)
            {
                return combinedLeft; // For a AND the inputSet is used by the left
            }

            internal override void UpdateOutputSet(bool combinedLeft, bool combinedRight, IEnumerable<T> leftReturn, IEnumerable<T> rightReturn)
            {
                if (!combinedRight)
                {
                    var result = eval(new EvalHandlerArgs(null, null, SearchIndexOperator.None, false, leftReturn, rightReturn, payload));
                    results = result.results;
                    if (!result.combined)
                    {
                        results = leftReturn.Intersect(rightReturn).ToList();
                    }
                }
                else
                    results = rightReturn;
            }

            internal override void UpdateRightInstruction(IEnumerable<T> leftReturn)
            {
                rightInstruction.andSet = leftReturn;
            }
        }

        internal class OrInstruction : OperandInstruction, IOrInstruction<T>
        {
            public OrInstruction(IQueryNode node, EvalHandler eval, object payload, bool not) : base(node, eval, payload, not)
            {
                combine = Combine.Union;
            }

            internal override bool IsCombineHandled(bool combinedLeft, bool combinedRight)
            {
                return combinedRight; // For a OR the addedSet is used by the right
            }

            internal override void UpdateOutputSet(bool combinedLeft, bool combinedRight, IEnumerable<T> leftReturn, IEnumerable<T> rightReturn)
            {
                if (!combinedRight)
                {
                    int rightReturnOriginalCount = rightReturn.Count();
                    int leftReturnOriginalCount = leftReturn.Count();
                    var result = eval(new EvalHandlerArgs(null, null, SearchIndexOperator.None, false, leftReturn, rightReturn, payload));
                    results = result.results;
                    if (!result.combined)
                    {
                        results = leftReturn.Union(rightReturn);
                    }
                }
                else
                    results = rightReturn;
            }

            internal override void UpdateRightInstruction(IEnumerable<T> leftReturn)
            {
                // Pass the input Set from the parent
                if (andSet != null)
                    rightInstruction.andSet = andSet;

                // might be wrong to modify that list because it's used somewhere else
                if (rightInstruction.orSet == null)
                    rightInstruction.orSet = leftReturn;
                else
                {
                    rightInstruction.orSet = rightInstruction.orSet.Union(leftReturn);
                }
            }
        }

        internal class ResultInstruction : Instruction, IResultInstruction<T>
        {
            public readonly bool exclude;

            public ResultInstruction(IQueryNode node, EvalHandler eval, object payload, bool exclude)
            {
                this.node = node;
                this.eval = eval;
                this.payload = payload;
                this.exclude = exclude;
            }

            public override bool Execute(Combine combine)
            {
                string searchName = null;
                object searchValue = null;
                var searchOperator = SearchIndexOperator.None;
                if (node is FilterNode filterNode)
                {
                    searchName = filterNode.filter.token;
                    searchValue = filterNode.filterValue;
                    searchOperator = ParseOperatorToken(filterNode.op.token);
                }
                else if (node is SearchNode searchNode)
                {
                    searchName = null;
                    searchValue = searchNode.searchValue;
                    searchOperator = searchNode.exact ? SearchIndexOperator.Equal : SearchIndexOperator.Contains;
                }

                var result = eval(new EvalHandlerArgs(searchName, searchValue, searchOperator, exclude, andSet, orSet, payload));
                results = result.results;
                return result.combined;
            }
        }
    }
}
