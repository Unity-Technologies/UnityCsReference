// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Search
{
    /// <summary>
    /// Interface for query handlers.
    /// </summary>
    /// <typeparam name="TData">The filtered data type.</typeparam>
    /// <typeparam name="TPayload">The payload type.</typeparam>
    public interface IQueryHandler<TData, in TPayload>
        where TPayload : class
    {
        /// <summary>
        /// Implement this function to evaluate the query on a payload.
        /// </summary>
        /// <param name="payload">The input data of the query.</param>
        /// <returns>An enumerable of type TData.</returns>
        IEnumerable<TData> Eval(TPayload payload);

        /// <summary>
        /// Implement this function to evaluate the query on a single element.
        /// </summary>
        /// <param name="element">A single object to be tested.</param>
        /// <returns>True if the object passes the query, false otherwise.</returns>
        bool Eval(TData element);
    }

    /// <summary>
    /// Interface for query handler factories.
    /// </summary>
    /// <typeparam name="TData">The filtered data type.</typeparam>
    /// <typeparam name="TQueryHandler">The query handler type.</typeparam>
    /// <typeparam name="TPayload">The payload type.</typeparam>
    public interface IQueryHandlerFactory<TData, out TQueryHandler, TPayload>
        where TQueryHandler : IQueryHandler<TData, TPayload>
        where TPayload : class
    {
        /// <summary>
        /// Implement this function to create a new query handler for a specific query graph.
        /// </summary>
        /// <param name="graph">A graph representing a query.</param>
        /// <param name="errors">A collection of errors. Use this to report errors when needed.</param>
        /// <returns>An object of type TQueryHandler.</returns>
        TQueryHandler Create(QueryGraph graph, ICollection<QueryError> errors);
    }

    /// <summary>
    /// A Query defines an operation that can be used to filter a data set.
    /// </summary>
    /// <typeparam name="TData">The filtered data type.</typeparam>
    /// <typeparam name="TPayload">The payload type.</typeparam>
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

        internal IQueryHandler<TData, TPayload> graphHandler { get; set; }

        internal QueryGraph evaluationGraph { get; }
        internal QueryGraph queryGraph { get; }

        internal Query(string text, QueryGraph evaluationGraph, QueryGraph queryGraph, ICollection<QueryError> errors, ICollection<string> tokens)
        {
            this.text = text;
            this.evaluationGraph = evaluationGraph;
            this.queryGraph = queryGraph;
            this.errors = errors;
            this.tokens = tokens;
        }

        internal Query(string text, QueryGraph evaluationGraph, QueryGraph queryGraph, ICollection<QueryError> errors, ICollection<string> tokens, IQueryHandler<TData, TPayload> graphHandler)
            : this(text, evaluationGraph, queryGraph, errors, tokens)
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

        static IQueryNode GetNodeAtPosition(IQueryNode root, int position)
        {
            if (root.type == QueryNodeType.Where)
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

    /// <summary>
    /// A Query defines an operation that can be used to filter a data set.
    /// </summary>
    /// <typeparam name="T">The filtered data type.</typeparam>
    public class Query<T> : Query<T, IEnumerable<T>>
    {
        /// <summary>
        /// Boolean indicating if the original payload should be return when the query is empty.
        /// If set to false, an empty array is returned instead.
        /// </summary>
        public bool returnPayloadIfEmpty { get; set; } = true;

        internal Query(string text, QueryGraph evaluationGraph, QueryGraph queryGraph, ICollection<QueryError> errors, ICollection<string> tokens, IQueryHandler<T, IEnumerable<T>> graphHandler)
            : base(text, evaluationGraph, queryGraph, errors, tokens, graphHandler)
        {}

        /// <summary>
        /// Apply the filtering on an IEnumerable data set.
        /// </summary>
        /// <param name="data">The data to filter</param>
        /// <returns>A filtered IEnumerable.</returns>
        public override IEnumerable<T> Apply(IEnumerable<T> data)
        {
            if (!valid)
                return new T[] {};

            if (evaluationGraph.empty)
            {
                return returnPayloadIfEmpty ? data : new T[] {};
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
