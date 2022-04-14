// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Search
{
    [Obsolete("Query has been deprecated. Use ParsedQuery instead (UnityUpgradable) -> ParsedQuery<TData, TPayload>", false)]
    public class Query<TData, TPayload>
        where TPayload : class
    {
        /// <summary>
        /// The text that generated this query.
        /// </summary>
        public string text { get; }

        /// <summary> Indicates if the query is valid or not. </summary>
        public bool valid => errors.Count == 0 && evaluationGraph != null;

        /// <summary> List of QueryErrors. </summary>
        public ICollection<QueryError> errors { get; }

        /// <summary>
        /// List of tokens found in the query.
        /// </summary>
        public ICollection<string> tokens { get; }

        internal ICollection<QueryToggle> toggles { get; }

        internal IQueryHandler<TData, TPayload> graphHandler { get; set; }

        public QueryGraph evaluationGraph { get; }
        public QueryGraph queryGraph { get; }

        internal Query(string text, QueryGraph evaluationGraph, QueryGraph queryGraph, ICollection<QueryError> errors, ICollection<string> tokens, ICollection<QueryToggle> toggles)
        {
            this.text = text;
            this.evaluationGraph = evaluationGraph;
            this.queryGraph = queryGraph;
            this.errors = errors;
            this.tokens = tokens;
            this.toggles = toggles;
        }

        internal Query(string text, QueryGraph evaluationGraph, QueryGraph queryGraph, ICollection<QueryError> errors, ICollection<string> tokens, ICollection<QueryToggle> toggles, IQueryHandler<TData, TPayload> graphHandler)
            : this(text, evaluationGraph, queryGraph, errors, tokens, toggles)
        {
            if (valid)
            {
                this.graphHandler = graphHandler;
            }
        }

        /// <summary>
        /// Apply the filtering on a payload.
        /// </summary>
        /// <param name="payload">The data to filter</param>
        /// <returns>A filtered IEnumerable.</returns>
        public virtual IEnumerable<TData> Apply(TPayload payload = null)
        {
            if (!valid)
                return null;
            return graphHandler.Eval(payload);
        }

        /// <summary>
        /// Optimize the query by optimizing the underlying filtering graph.
        /// </summary>
        /// <param name="propagateNotToLeaves">Propagate "Not" operations to leaves, so only leaves can have "Not" operations as parents.</param>
        /// <param name="swapNotToRightHandSide">Swaps "Not" operations to the right hand side of combining operations (i.e. "And", "Or"). Useful if a "Not" operation is slow.</param>
        public void Optimize(bool propagateNotToLeaves, bool swapNotToRightHandSide)
        {
            evaluationGraph?.Optimize(propagateNotToLeaves, swapNotToRightHandSide);
        }

        /// <summary>
        /// Optimize the query by optimizing the underlying filtering graph.
        /// </summary>
        /// <param name="options">Optimization options.</param>
        public void Optimize(QueryGraphOptimizationOptions options)
        {
            evaluationGraph?.Optimize(options);
        }

        /// <summary>
        /// Get the query node located at the specified position in the query.
        /// </summary>
        /// <param name="position">The position of the query node in the text.</param>
        /// <returns>An IQueryNode.</returns>
        public IQueryNode GetNodeAtPosition(int position)
        {
            // Allow position at Length, to support cursor at end of word.
            if (position < 0 || position > text.Length)
                throw new ArgumentOutOfRangeException(nameof(position));

            if (queryGraph == null || queryGraph.empty)
                return null;

            return GetNodeAtPosition(queryGraph.root, position);
        }

        internal bool HasToggle(string toggle)
        {
            return HasToggle(toggle, StringComparison.Ordinal);
        }

        internal bool HasToggle(string toggle, StringComparison stringComparison)
        {
            return toggles.Any(s => s.value.Equals(toggle, stringComparison));
        }

        static IQueryNode GetNodeAtPosition(IQueryNode root, int position)
        {
            if (root.type == QueryNodeType.Where || root.type == QueryNodeType.Group)
                return GetNodeAtPosition(root.children[0], position);

            if (!string.IsNullOrEmpty(root.token.text) && position >= root.token.position && position <= root.token.position + root.token.length)
                return root;

            if (root.leaf || root.children == null)
                return null;

            if (root.children.Count == 1)
                return GetNodeAtPosition(root.children[0], position);

            // We have no more than two children
            return GetNodeAtPosition(position < root.token.position ? root.children[0] : root.children[1], position);
        }
    }

    [Obsolete("Query has been deprecated. Use ParsedQuery instead (UnityUpgradable) -> ParsedQuery<T>", false)]
    public class Query<T> : Query<T, IEnumerable<T>>
    {
        /// <summary>
        /// Boolean indicating if the original payload should be return when the query is empty.
        /// If set to false, an empty array is returned instead.
        /// </summary>
        public bool returnPayloadIfEmpty { get; set; } = true;

        internal Query(string text, QueryGraph evaluationGraph, QueryGraph queryGraph, ICollection<QueryError> errors, ICollection<string> tokens, ICollection<QueryToggle> toggles, IQueryHandler<T, IEnumerable<T>> graphHandler)
            : base(text, evaluationGraph, queryGraph, errors, tokens, toggles, graphHandler)
        {}

        /// <summary>
        /// Apply the filtering on an IEnumerable data set.
        /// </summary>
        /// <param name="data">The data to filter</param>
        /// <returns>A filtered IEnumerable.</returns>
        public override IEnumerable<T> Apply(IEnumerable<T> data)
        {
            return Apply(data, returnPayloadIfEmpty);
        }

        internal IEnumerable<T> Apply(IEnumerable<T> data, bool returnInputIfEmpty)
        {
            if (!valid)
                return new T[] {};

            if (evaluationGraph.empty)
            {
                return returnInputIfEmpty ? data : new T[] {};
            }

            return graphHandler.Eval(data);
        }

        /// <summary>
        /// Test the query on a single object. Returns true if the test passes.
        /// </summary>
        /// <param name="element">A single object to be tested.</param>
        /// <returns>True if the object passes the query, false otherwise.</returns>
        public bool Test(T element)
        {
            if (!valid)
                return false;

            if (evaluationGraph.empty)
            {
                return returnPayloadIfEmpty;
            }

            return graphHandler.Eval(element);
        }
    }
}
