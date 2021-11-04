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
        Aggregator,
        /// <summary>
        /// Comment node.
        /// </summary>
        Comment,
        /// <summary>
        /// Toggle node
        /// </summary>
        Toggle,
        /// <summary>
        /// Group node
        /// </summary>
        Group
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

        string m_Identifier;
        public string identifier => m_Identifier ??= filterStringView.ToString();

        public QueryToken token { get; set; }

        string m_FilterId;
        public string filterId => m_FilterId ??= filterIdStringView.ToString();
        string m_ParamValue;
        public string paramValue => m_ParamValue ??= paramValueStringView.ToString();
        string m_OperatorId;
        public string operatorId => m_OperatorId ??= operatorIdStringView.ToString();
        string m_FilterValue;
        public string filterValue => m_FilterValue ??= filterValueStringView.ToString();

        internal StringView filterIdStringView;
        internal StringView paramValueStringView;
        internal StringView operatorIdStringView;
        internal StringView filterValueStringView;
        internal StringView filterStringView;
        internal StringView rawFilterValueStringView;

        public FilterNode(in StringView filterId, in StringView operatorId, in StringView filterValue, in StringView rawFilterValue, in StringView paramValue, in StringView filterString)
        {
            this.filterIdStringView = filterId;
            this.paramValueStringView = paramValue;
            this.operatorIdStringView = operatorId;
            this.filterValueStringView = filterValue;
            this.rawFilterValueStringView = rawFilterValue;
            filterStringView = filterString;
        }

        public FilterNode(in StringView filterId, in StringView operatorId, in StringView filterValue, in StringView paramValue, in StringView filterString)
            : this(filterId, operatorId, filterValue, filterValue, paramValue, filterString)
        {}

        public FilterNode(IFilter filter, in StringView filterId, in QueryFilterOperator op, in StringView operatorId, in StringView filterValue, in StringView rawFilterValue, in StringView paramValue, in StringView filterString)
            : this(filterId, operatorId, filterValue, rawFilterValue, paramValue, filterString)
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
            return new FilterNode(filter, in filterIdStringView, in op, in operatorIdStringView, in filterValueStringView, in rawFilterValueStringView, in paramValueStringView, in filterStringView)
            {
                skipped = skipped,
                token = token,
                parent = parent
            };
        }
    }

    class SearchNode : ISearchNode, ICopyableNode
    {
        public bool exact { get; }
        string m_SearchValue;
        public string searchValue => m_SearchValue ??= searchValueStringView.ToString();

        public IQueryNode parent { get; set; }
        public QueryNodeType type => QueryNodeType.Search;
        public List<IQueryNode> children => null;
        public bool leaf => true;
        public bool skipped { get; set; }
        public string identifier { get; }
        public QueryToken token { get; set; }

        internal StringView searchValueStringView;
        internal StringView rawSearchValueStringView;

        public SearchNode(in StringView searchValue, in StringView rawSearchValue, bool isExact)
        {
            searchValueStringView = searchValue;
            rawSearchValueStringView = rawSearchValue;
            exact = isExact;
            identifier = exact ? ("!" + searchValue.ToString()) : searchValue.ToString();
        }

        public int QueryHashCode()
        {
            return identifier.GetHashCode();
        }

        public IQueryNode Copy()
        {
            return new SearchNode(in searchValueStringView, in rawSearchValueStringView, exact)
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

        public CombinedNode AddNode(IQueryNode node)
        {
            children.Add(node);
            node.parent = this;
            return this;
        }

        public CombinedNode RemoveNode(IQueryNode node)
        {
            if (!children.Contains(node))
                return this;

            children.Remove(node);
            if (node.parent == this)
                node.parent = null;
            return this;
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

        public void RemoveFromTreeIfPossible(ref IQueryNode currentNode, ICollection<QueryError> errors)
        {
            // If we have become a leaf, remove ourselves from our parent
            if (leaf)
            {
                if (parent == null)
                    currentNode = null;
                else if (parent is CombinedNode cn)
                {
                    cn.RemoveNode(this);
                }
                return;
            }

            // If we cannot remove ourselves from the tree, nothing to do.
            if (!CanRemoveFromTree())
                return;

            // At this point, there should be only 1 child left
            if (children.Count != 1)
            {
                errors.Add(new QueryError(token.position, $"There should be only one child in the node. Cannot remove node: [{type}] \"{identifier}\"."));
                return;
            }

            // If we have no parent, we are the root so the remaining child must take our place
            if (parent == null)
            {
                currentNode = children[0];
                currentNode.parent = null;
                return;
            }

            // Otherwise, replace ourselves with the remaining child in our parent child list
            var index = parent.children.IndexOf(this);
            if (index == -1)
            {
                errors.Add(new QueryError(token.position, $"Node {type} not found in its parent's children list."));
                return;
            }

            parent.children[index] = children[0];
            children[0].parent = parent;
            parent = null;
        }

        protected abstract IQueryNode CopySelf();

        protected abstract bool CanRemoveFromTree();
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

        protected override bool CanRemoveFromTree()
        {
            return children.Count < 2;
        }
    }

    class OrNode : CombinedNode
    {
        public override QueryNodeType type => QueryNodeType.Or;
        public override string identifier => BuildIdentifier();

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

        protected override bool CanRemoveFromTree()
        {
            return children.Count < 2;
        }

        private string BuildIdentifier()
        {
            if (children.Count != 2)
                return string.Empty;
            return "(" + children[0].identifier + " or " + children[1].identifier + ")";
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

        protected override bool CanRemoveFromTree()
        {
            return children.Count == 0;
        }
    }

    class NestedQueryNode : INestedQueryNode, ICopyableNode
    {
        public IQueryNode parent { get; set; }
        public QueryNodeType type => QueryNodeType.NestedQuery;
        public List<IQueryNode> children { get; } = null;
        public bool leaf => true;
        public bool skipped { get; set; }
        string m_Identifier;
        public string identifier => m_Identifier ??= nestedQueryStringView.ToString();
        string m_AssociatedFilter;
        public string associatedFilter => m_AssociatedFilter ??= associatedFilterStringView.ToString();
        public QueryToken token { get; set; }

        public INestedQueryHandler nestedQueryHandler { get; }

        internal StringView nestedQueryStringView;
        internal StringView associatedFilterStringView;
        internal StringView rawNestedQueryStringView;

        public NestedQueryNode(in StringView nestedQuery, in StringView rawNestedQuery, INestedQueryHandler nestedQueryHandler)
        {
            nestedQueryStringView = nestedQuery;
            rawNestedQueryStringView = rawNestedQuery;
            this.nestedQueryHandler = nestedQueryHandler;
        }

        public NestedQueryNode(in StringView nestedQuery, in StringView rawNestedQuery, in StringView filterToken, INestedQueryHandler nestedQueryHandler)
            : this(nestedQuery, rawNestedQuery, nestedQueryHandler)
        {
            associatedFilterStringView = filterToken;
        }

        public int QueryHashCode()
        {
            return identifier.GetHashCode();
        }

        public IQueryNode Copy()
        {
            return new NestedQueryNode(in nestedQueryStringView, in rawNestedQueryStringView, in associatedFilterStringView, nestedQueryHandler)
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

        protected override bool CanRemoveFromTree()
        {
            return children.Count == 0;
        }
    }

    class InFilterNode : FilterNode
    {
        public override QueryNodeType type => QueryNodeType.FilterIn;
        public override List<IQueryNode> children { get; } = new List<IQueryNode>();
        public override bool leaf => children.Count == 0;

        public InFilterNode(IFilter filter, in StringView filterId, in QueryFilterOperator op, in StringView operatorId, in StringView filterValue, in StringView rawFilterValue, in StringView paramValue, in StringView filterString)
            : base(filter, in filterId, in op, in operatorId, in filterValue, in rawFilterValue, in paramValue, in filterString) {}

        public override IQueryNode Copy()
        {
            var selfNode = new InFilterNode(filter, in filterIdStringView, in op, in operatorIdStringView, in filterValueStringView, in rawFilterValueStringView, in paramValueStringView, in filterStringView);

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
        string m_Identifier;
        public override string identifier => m_Identifier ??= tokenStringView.ToString();
        public INestedQueryAggregator aggregator { get; }

        internal StringView tokenStringView;

        public AggregatorNode(in StringView token, INestedQueryAggregator aggregator)
        {
            tokenStringView = token;
            this.aggregator = aggregator;
        }

        public override void SwapChildNodes()
        {}

        protected override IQueryNode CopySelf()
        {
            return new AggregatorNode(in tokenStringView, aggregator)
            {
                parent = parent,
                token = token,
                skipped = skipped
            };
        }

        protected override bool CanRemoveFromTree()
        {
            return children.Count == 0;
        }
    }

    class CommentNode : IQueryNode, ICopyableNode
    {
        public IQueryNode parent { get; set; }
        public QueryNodeType type => QueryNodeType.Comment;
        public List<IQueryNode> children => null;
        public bool leaf => true;

        public bool skipped
        {
            get => true;
            set {}
        }

        string m_Identifier;
        public string identifier => m_Identifier ??= tokenStringView.ToString();

        public QueryToken token { get; set; }

        internal StringView tokenStringView;

        public CommentNode(in StringView token)
        {
            tokenStringView = token;
        }

        public int QueryHashCode()
        {
            return identifier.GetHashCode();
        }

        public IQueryNode Copy()
        {
            return new CommentNode(in tokenStringView)
            {
                parent = parent,
                token = token
            };
        }
    }

    class ToggleNode : IQueryNode, ICopyableNode
    {
        string m_Value;
        public string value => m_Value ??= valueStringView.ToString();

        public IQueryNode parent { get; set; }
        public QueryNodeType type => QueryNodeType.Toggle;
        public List<IQueryNode> children => null;
        public bool leaf => true;

        public bool skipped
        {
            get => true;
            set {}
        }

        string m_Identifier;
        public string identifier => m_Identifier ??= tokenStringView.ToString();

        public QueryToken token { get; set; }

        internal StringView tokenStringView;
        internal StringView valueStringView;

        public ToggleNode(in StringView token, in StringView value)
        {
            tokenStringView = token;
            valueStringView = value;
        }

        public int QueryHashCode()
        {
            return identifier.GetHashCode();
        }

        public IQueryNode Copy()
        {
            return new ToggleNode(in tokenStringView, in valueStringView)
            {
                parent = parent,
                token = token
            };
        }
    }

    class GroupNode : CombinedNode
    {
        public override QueryNodeType type => QueryNodeType.Group;
        string m_Identifier;
        public override string identifier => m_Identifier ??= tokenStringView.ToString();

        internal StringView tokenStringView;
        public GroupNode(in StringView token)
        {
            tokenStringView = token;
        }

        public override void SwapChildNodes()
        {}

        protected override IQueryNode CopySelf()
        {
            return new GroupNode(in tokenStringView)
            {
                parent = parent,
                skipped = skipped,
                token = token
            };
        }

        protected override bool CanRemoveFromTree()
        {
            return true;
        }
    }
}
