// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.Search
{
    /// <summary>
    /// Enumeration representing the query node types.
    /// </summary>
    public enum QueryNodeType
    {
        /// <summary>
        /// And node.
        /// </summary>
        And,
        /// <summary>
        /// Or node.
        /// </summary>
        Or,
        /// <summary>
        /// Filter node.
        /// </summary>
        Filter,
        /// <summary>
        /// Word search node.
        /// </summary>
        Search,
        /// <summary>
        /// Negation node.
        /// </summary>
        Not,
        /// <summary>
        /// Nested query node.
        /// </summary>
        NestedQuery,
        /// <summary>
        /// Where enumerator node.
        /// </summary>
        Where,
        /// <summary>
        /// Filter with nested query node.
        /// </summary>
        FilterIn,
        /// <summary>
        /// Union node.
        /// </summary>
        Union,
        /// <summary>
        /// Intersection node.
        /// </summary>
        Intersection,
        /// <summary>
        /// Aggregator node.
        /// </summary>
        Aggregator
    }

    /// <summary>
    /// Interface representing a query node.
    /// </summary>
    public interface IQueryNode
    {
        /// <summary>
        /// Parent of this node. Null if this node is the root.
        /// </summary>
        IQueryNode parent { get; set; }

        /// <summary>
        /// The node's type.
        /// </summary>
        QueryNodeType type { get; }

        /// <summary>
        /// The node's children. Can be null.
        /// </summary>
        List<IQueryNode> children { get; }

        /// <summary>
        /// True if this node is a leaf. A leaf doesn't have any child.
        /// </summary>
        bool leaf { get; }

        /// <summary>
        /// True if this node is skipped during evaluation. A node can be skipped if the QueryEngine was configured to skip unsupported nodes instead of generating errors.
        /// </summary>
        bool skipped { get; set; }

        /// <summary>
        /// A string representing this node and its children.
        /// </summary>
        string identifier { get; }

        /// <summary>
        /// Returns a hashcode for this node. Multiple nodes can have the same hashcode if they have the same identifier.
        /// </summary>
        /// <returns>An integer representing the hashcode.</returns>
        int QueryHashCode();

        /// <summary>
        /// The token used to build this node.
        /// </summary>
        QueryToken token { get; set; }
    }

    /// <summary>
    /// Interface representing a filter node.
    /// </summary>
    /// <inheritdoc cref="IQueryNode"/>
    public interface IFilterNode : IQueryNode
    {
        /// <summary>
        /// The filter identifier.
        /// </summary>
        string filterId { get; }

        /// <summary>
        /// The parameter value. This can be null or empty if the filter is not a filter function, or there is no parameter written yet.
        /// </summary>
        string paramValue { get; }

        /// <summary>
        /// The operator identifier. This can be null or empty if the operator has not been written yet.
        /// </summary>
        string operatorId { get; }

        /// <summary>
        /// The filter value. This can be null or empty if the value has not been written yet.
        /// </summary>
        string filterValue { get; }
    }

    /// <summary>
    /// Interface representing a word search node.
    /// </summary>
    /// <inheritdoc cref="IQueryNode"/>
    public interface ISearchNode : IQueryNode
    {
        /// <summary>
        /// True if the word or text should match exactly, false if it is a "contains" operation.
        /// </summary>
        bool exact { get; }

        /// <summary>
        /// Word or text used for the search.
        /// </summary>
        string searchValue { get; }
    }

    /// <summary>
    /// Interface representing a nested query node.
    /// </summary>
    /// <inheritdoc cref="IQueryNode"/>
    public interface INestedQueryNode : IQueryNode
    {
        /// <summary>
        /// If the nested query is part of a filter operation, this represents the filter identifier. Otherwise, this is null or empty.
        /// </summary>
        string associatedFilter { get; }
    }

    interface ICopyableNode
    {
        IQueryNode Copy();
    }

    class FilterNode : IFilterNode, ICopyableNode
    {
        public IFilter filter;
        public QueryFilterOperator op;

        public IQueryNode parent { get; set; }
        public virtual QueryNodeType type => QueryNodeType.Filter;
        public virtual List<IQueryNode> children => null;
        public virtual bool leaf => true;
        public virtual bool skipped { get; set; }
        public string identifier { get; }
        public QueryToken token { get; set; }

        public string filterId { get; }
        public string paramValue { get; }
        public string operatorId { get; }
        public string filterValue { get; }

        public FilterNode(string filterId, string operatorId, string filterValue, string paramValue, string filterString)
        {
            this.filterId = filterId;
            this.paramValue = paramValue;
            this.operatorId = operatorId;
            this.filterValue = filterValue;
            identifier = filterString;
        }

        public FilterNode(IFilter filter, string filterId, in QueryFilterOperator op, string filterValue, string paramValue, string filterString)
            : this(filterId, op.token, filterValue, paramValue, filterString)
        {
            this.filter = filter;
            this.op = op;
        }

        public int QueryHashCode()
        {
            return identifier.GetHashCode();
        }

        public virtual IQueryNode Copy()
        {
            return new FilterNode(filterId, operatorId, filterValue, paramValue, identifier)
            {
                filter = filter,
                op = op,
                skipped = skipped,
                token = token,
                parent = parent
            };
        }
    }

    class SearchNode : ISearchNode, ICopyableNode
    {
        public bool exact { get; }
        public string searchValue { get; }

        public IQueryNode parent { get; set; }
        public QueryNodeType type => QueryNodeType.Search;
        public List<IQueryNode> children => null;
        public bool leaf => true;
        public bool skipped { get; set; }
        public string identifier { get; }
        public QueryToken token { get; set; }

        public SearchNode(string searchValue, bool isExact)
        {
            this.searchValue = searchValue;
            exact = isExact;
            identifier = exact ? ("!" + searchValue) : searchValue;
        }

        public int QueryHashCode()
        {
            return identifier.GetHashCode();
        }

        public IQueryNode Copy()
        {
            return new SearchNode(searchValue, exact)
            {
                parent = parent,
                skipped = skipped,
                token = token
            };
        }
    }

    abstract class CombinedNode : IQueryNode, ICopyableNode
    {
        public IQueryNode parent { get; set; }
        public abstract QueryNodeType type { get; }
        public List<IQueryNode> children { get; }
        public bool leaf => children.Count == 0;
        public bool skipped { get; set; }
        public abstract string identifier { get; }
        public QueryToken token { get; set; }

        protected CombinedNode()
        {
            children = new List<IQueryNode>();
        }

        public void AddNode(IQueryNode node)
        {
            children.Add(node);
            node.parent = this;
        }

        public void RemoveNode(IQueryNode node)
        {
            if (!children.Contains(node))
                return;

            children.Remove(node);
            if (node.parent == this)
                node.parent = null;
        }

        public void Clear()
        {
            foreach (var child in children)
            {
                if (child.parent == this)
                    child.parent = null;
            }
            children.Clear();
        }

        public abstract void SwapChildNodes();

        public virtual int QueryHashCode()
        {
            var hc = 0;
            foreach (var child in children)
            {
                hc ^= child.GetHashCode();
            }
            return hc;
        }

        public IQueryNode Copy()
        {
            if (!(CopySelf() is CombinedNode selfNode))
                return null;

            // Don't call Clear directly, as you will destroy the parenting for the original children.
            selfNode.children.Clear();

            foreach (var child in children)
            {
                if (!(child is ICopyableNode copyableChild))
                    return null;
                var childCopy = copyableChild.Copy();
                if (childCopy != null)
                    selfNode.AddNode(childCopy);
            }

            return selfNode;
        }

        protected abstract IQueryNode CopySelf();
    }

    class AndNode : CombinedNode
    {
        public override QueryNodeType type => QueryNodeType.And;
        public override string identifier => "(" + children[0].identifier + " " + children[1].identifier + ")";

        public override void SwapChildNodes()
        {
            if (children.Count != 2)
                return;

            var tmp = children[0];
            children[0] = children[1];
            children[1] = tmp;
        }

        protected override IQueryNode CopySelf()
        {
            return new AndNode
            {
                parent = parent,
                token = token,
                skipped = skipped
            };
        }
    }

    class OrNode : CombinedNode
    {
        public override QueryNodeType type => QueryNodeType.Or;
        public override string identifier => "(" + children[0].identifier + " or " + children[1].identifier + ")";

        public override void SwapChildNodes()
        {
            if (children.Count != 2)
                return;

            var tmp = children[0];
            children[0] = children[1];
            children[1] = tmp;
        }

        protected override IQueryNode CopySelf()
        {
            return new OrNode
            {
                parent = parent,
                token = token,
                skipped = skipped
            };
        }
    }

    class NotNode : CombinedNode
    {
        public override QueryNodeType type => QueryNodeType.Not;
        public override string identifier => "-" + children[0].identifier;

        public override void SwapChildNodes()
        {}

        protected override IQueryNode CopySelf()
        {
            return new NotNode
            {
                parent = parent,
                token = token,
                skipped = skipped
            };
        }
    }

    class NestedQueryNode : INestedQueryNode, ICopyableNode
    {
        public IQueryNode parent { get; set; }
        public QueryNodeType type => QueryNodeType.NestedQuery;
        public List<IQueryNode> children { get; } = null;
        public bool leaf => true;
        public bool skipped { get; set; }
        public string identifier { get; }
        public string associatedFilter { get; }
        public QueryToken token { get; set; }

        public INestedQueryHandler nestedQueryHandler { get; }

        public NestedQueryNode(string nestedQuery, INestedQueryHandler nestedQueryHandler)
        {
            identifier = nestedQuery;
            this.nestedQueryHandler = nestedQueryHandler;
        }

        public NestedQueryNode(string nestedQuery, string filterToken, INestedQueryHandler nestedQueryHandler)
            : this(nestedQuery, nestedQueryHandler)
        {
            associatedFilter = filterToken;
        }

        public int QueryHashCode()
        {
            return identifier.GetHashCode();
        }

        public IQueryNode Copy()
        {
            return new NestedQueryNode(identifier, associatedFilter, nestedQueryHandler)
            {
                parent = parent,
                token = token,
                skipped = skipped
            };
        }
    }

    class WhereNode : CombinedNode
    {
        public override QueryNodeType type => QueryNodeType.Where;
        public override string identifier => children[0].identifier;

        public override void SwapChildNodes()
        {}

        protected override IQueryNode CopySelf()
        {
            return new WhereNode
            {
                parent = parent,
                token = token,
                skipped = skipped
            };
        }
    }

    class InFilterNode : FilterNode
    {
        public override QueryNodeType type => QueryNodeType.FilterIn;
        public override List<IQueryNode> children { get; } = new List<IQueryNode>();
        public override bool leaf => children.Count == 0;

        public InFilterNode(IFilter filter, string filterId, in QueryFilterOperator op, string filterValue, string paramValue, string filterString)
            : base(filter, filterId, in op, filterValue, paramValue, filterString) {}

        public override IQueryNode Copy()
        {
            var selfNode = new InFilterNode(filter, filterId, in op, filterValue, paramValue, identifier);

            selfNode.children.Clear();

            foreach (var child in children)
            {
                if (!(child is ICopyableNode copyableChild))
                    return null;
                var childCopy = copyableChild.Copy();
                if (childCopy != null)
                {
                    childCopy.parent = selfNode;
                    selfNode.children.Add(childCopy);
                }
            }

            return selfNode;
        }
    }

    class UnionNode : OrNode
    {
        public override QueryNodeType type => QueryNodeType.Union;

        protected override IQueryNode CopySelf()
        {
            return new UnionNode
            {
                parent = parent,
                token = token,
                skipped = skipped
            };
        }
    }

    class IntersectionNode : AndNode
    {
        public override QueryNodeType type => QueryNodeType.Intersection;

        protected override IQueryNode CopySelf()
        {
            return new IntersectionNode
            {
                parent = parent,
                token = token,
                skipped = skipped
            };
        }
    }

    class AggregatorNode : CombinedNode
    {
        public override QueryNodeType type => QueryNodeType.Aggregator;
        public override string identifier { get; }
        public INestedQueryAggregator aggregator { get; }

        public AggregatorNode(string token, INestedQueryAggregator aggregator)
        {
            identifier = token;
            this.aggregator = aggregator;
        }

        public override void SwapChildNodes()
        {}

        protected override IQueryNode CopySelf()
        {
            return new AggregatorNode(identifier, aggregator)
            {
                parent = parent,
                token = token,
                skipped = skipped
            };
        }
    }
}
