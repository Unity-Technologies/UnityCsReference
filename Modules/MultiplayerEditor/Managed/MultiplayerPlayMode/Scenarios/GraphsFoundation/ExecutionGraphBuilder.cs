// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.Multiplayer.PlayMode.Editor;

/// <summary>
/// Builder class for creating execution graphs. This class provides methods to add nodes and connect them.
/// It is used to create the execution graph for a play mode scenario.
/// </summary>
class ExecutionGraphBuilder
{
    ExecutionGraph m_Graph;
    Dictionary<ExecutionNode, NodeMetadata> m_NodeMetadata;

    internal ExecutionGraphBuilder()
    {
        m_NodeMetadata = new();
        m_Graph = new ExecutionGraph();
    }

    /// <summary>
    /// Adds a node of type TNode to the graph, and returns it. The node will be added to the stage provided as parameter.
    /// </summary>
    /// <typeparam name="TNode">The type of the node to add.</typeparam>
    /// <param name="stage">The stage to which the node will be added.</param>
    /// <returns>The added node as <typeparamref name="TNode"/>.</returns>
    public TNode AddNode<TNode>(ExecutionStage stage) where TNode : ExecutionNode, new()
    {
        ThrowIfGraphAlreadyBuilt();

        if (!Enum.IsDefined(typeof(ExecutionStage), stage))
        {
            throw new ArgumentException($"Invalid stage {stage}.", nameof(stage));
        }

        var node = new TNode();
        m_NodeMetadata[node] = new NodeMetadata
        {
            Stage = stage,
            UpstreamNodes = new(),
            ConnectedInputs = new()
        };

        m_Graph.AddNode(node, stage);

        return node;
    }

    /// <summary>
    /// Connects a constant value to a node input. The value will be used as input for the node during execution.
    /// </summary>
    /// <typeparam name="T">The type of the value to connect.</typeparam>
    /// <param name="input">The node input to connect the value to.</param>
    /// <param name="value">The constant value to connect.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the target input belongs to a node that is not part of this builder.</exception>
    /// <exception cref="NotImplementedException"></exception>
    public void ConnectConstant<T>(NodeInput<T> input, T value)
    {
        ThrowIfGraphAlreadyBuilt();

        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }
        
        
        if (!m_NodeMetadata.ContainsKey(input.GetNode()))
        {
            throw new InvalidOperationException("The target input must belong to a node in this builder.");
        }

        m_Graph.ConnectConstant(input, value);
    }

    /// <summary>
    /// Connects a node output to a node input. The output of the "from" node will be used as input for the "to" node during execution.
    /// </summary>
    /// <typeparam name="T">The type of the value being passed from the output to the input.</typeparam>
    /// <param name="from">The node output to connect from.</param>
    /// <param name="to">The node input to connect to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="from"/> or <paramref name="to"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when either endpoint belongs to a node that is not part of this builder, when the input is already connected, when the source node is in a later stage than the target node, or when the connection would introduce a cycle dependency.</exception>
    /// <exception cref="NotImplementedException"></exception>
    public void Connect<T>(NodeOutput<T> from, NodeInput<T> to)
    {
        ThrowIfGraphAlreadyBuilt();

        if (from == null)
        {
            throw new ArgumentNullException(nameof(from));
        }

        if (to == null)
        {
            throw new ArgumentNullException(nameof(to));
        }

        if (!m_NodeMetadata.TryGetValue(from.GetNode(), out var fromMetadata) ||
            !m_NodeMetadata.TryGetValue(to.GetNode(), out var toMetadata))
        {
            throw new InvalidOperationException("Both the source output and the target input must belong to nodes in this builder.");
        }

        if (toMetadata.ConnectedInputs.Contains(to))
        {
            throw new InvalidOperationException("The target input is already connected to an output.");
        }

        // We cannot connect nodes from later stages to earlier stages, as it would create an execution order conflict.
        if (fromMetadata.Stage > toMetadata.Stage)
        {
            throw new InvalidOperationException("Cannot connect an output from a later stage to an input of an earlier stage.");
        }

        // We also need to check for potential cycles in the graph.
        // Connecting an output to an input of a node that is upstream of the source node would create a cycle.
        if (fromMetadata.UpstreamNodes.Contains(to.GetNode()) || from.GetNode() == to.GetNode())
        {
            throw new InvalidOperationException("Connecting the specified output and input would create a cycle in the graph.");
        }

        m_Graph.Connect(from, to);
        toMetadata.ConnectedInputs.Add(to);
        toMetadata.UpstreamNodes.Add(from.GetNode());
        toMetadata.UpstreamNodes.UnionWith(fromMetadata.UpstreamNodes);
        m_NodeMetadata[to.GetNode()] = toMetadata;
    }

    internal ExecutionGraph Build()
    {
        ThrowIfGraphAlreadyBuilt();

        var result = m_Graph;
        m_Graph = null;
        return result;
    }

    void ThrowIfGraphAlreadyBuilt()
    {
        if (m_Graph == null)
        {
            throw new InvalidOperationException("The graph has already been built.");
        }
    }

    struct NodeMetadata
    {
        public ExecutionStage Stage;
        public HashSet<ExecutionNode> UpstreamNodes;
        public HashSet<NodeInput> ConnectedInputs;
    }
}
