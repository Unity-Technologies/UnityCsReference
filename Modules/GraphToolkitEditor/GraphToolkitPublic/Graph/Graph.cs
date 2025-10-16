// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Represents the core definition of a graph and defines its behavior.
    /// </summary>
    /// <remarks>
    /// <c>Graph</c> serves as the central entry point for:
    /// <list type="bullet">
    /// <item><description>Lifecycle management (via <see cref="OnEnable"/>, <see cref="OnDisable"/>)</description></item>
    /// <item><description>Change tracking (via <see cref="OnGraphChanged"/>)</description></item>
    /// <item><description>Access to nodes and variables</description></item>
    /// </list>
    /// To register a graph type and associate it with a custom file extension and configuration options,
    /// apply the <see cref="GraphAttribute"/> to your custom <c>Graph</c> class.
    /// <br/>
    /// <br/>
    /// You can further control the graph's behavior using the
    /// <see cref="GraphOptions"/> enum, which defines traits such as support for subgraphs.
    /// If your graph supports subgraphs (via <see cref="GraphOptions.SupportsSubgraphs"/>), you can declare valid subgraph types
    /// using the <see cref="SubgraphAttribute"/>.
    /// <br/>
    /// <br/>
    /// Use the <see cref="GraphDatabase"/> utility class to create, load, and save graph assets in the Unity Editor.
    /// Graphs are serialized assets. You can create them through the editor UI with
    /// <see cref="GraphDatabase.PromptInProjectBrowserToCreateNewAsset{T}"/> or load them from disk with
    /// <see cref="GraphDatabase.LoadGraph{T}"/>.
    /// </remarks>
    [Serializable]
    public partial class Graph
    {
        /// <summary>
        /// The name of the graph.
        /// </summary>
        public string Name
        {
            get
            {
                CheckImplementation();
                return m_Implementation.Name;
            }
        }

        /// <summary>
        /// The number of <see cref="IVariable"/>s declared in the graph.
        /// </summary>
        public int VariableCount
        {
            get
            {
                CheckImplementation();
                return m_Implementation.VariableModels.Count;
            }
        }

        /// <summary>
        /// The number of <see cref="INode"/>s in the graph.
        /// </summary>
        public int NodeCount
        {
            get
            {
                CheckImplementation();
                return m_Implementation.Nodes.Count;
            }
        }

        /// <summary>
        /// Retrieves a variable declared in the graph by index.
        /// </summary>
        /// <param name="index">The index of the variable to retrieve.</param>
        /// <returns>The <see cref="IVariable"/> at the specified index.</returns>
        /// <remarks>
        /// Use this method to access a specific <see cref="IVariable"/> from the list of variables declared in the graph.
        /// This list does not include variable nodes that reference variables.
        /// The index is zero-based and reflects the order in which the variables were created.
        /// The index must be within the valid range of the variable list (see: <see cref="VariableCount"/>).
        /// </remarks>
        public IVariable GetVariable(int index)
        {
            CheckImplementation();
            return m_Implementation.VariableModels[index];
        }

        /// <summary>
        /// Retrieves all variables declared in the graph.
        /// </summary>
        /// <returns>An <c>IEnumerable</c> of all <see cref="IVariable"/>s declared in the graph.</returns>
        /// <remarks>
        /// Use this method to enumerate all <see cref="IVariable"/>s declared in the graph.
        /// This list does not include variable nodes that reference variables.
        /// The collection reflects the variables as declared, in their order of creation.
        /// </remarks>
        public IEnumerable<IVariable> GetVariables()
        {
            CheckImplementation();
            return m_Implementation.VariableModels;
        }

        /// <summary>
        /// Retrieves a node defined in the graph by its index.
        /// </summary>
        /// <param name="index">The zero-based index of the node to retrieve.</param>
        /// <returns>The <see cref="INode"/> at the specified index.</returns>
        /// <remarks>
        /// Use this method to access a node based on its creation order in the graph. The index is zero-based and must be within range (see: <see cref="NodeCount"/>).
        /// <br/>
        /// The list includes:
        /// <list type="bullet">
        /// <item><description>Your own <see cref="Node"/>s</description></item>
        /// <item><description><see cref="ContextNode"/>s</description></item>
        /// <item><description><see cref="IVariableNode"/>s</description></item>
        /// <item><description><see cref="IConstantNode"/>s</description></item>
        /// <item><description><see cref="ISubgraphNode"/>s</description></item>
        /// </list>
        /// It excludes <see cref="BlockNode"/>s, which are only accessible through their parent <see cref="ContextNode"/>.
        /// </remarks>
        public INode GetNode(int index)
        {
            CheckImplementation();
            return m_Implementation.Nodes[index];
        }

        /// <summary>
        /// Retrieves all nodes in the graph.
        /// </summary>
        /// <returns>An <c>IEnumerable</c> of all <see cref="INode"/>s in the graph.</returns>
        /// <remarks>
        /// Use this method to access every node in the graph. Nodes are returned in the order they were created.
        /// <br/>
        /// The list includes:
        /// <list type="bullet">
        /// <item><description>Your own <see cref="Node"/>s</description></item>
        /// <item><description><see cref="ContextNode"/>s</description></item>
        /// <item><description><see cref="IVariableNode"/>s</description></item>
        /// <item><description><see cref="IConstantNode"/>s</description></item>
        /// <item><description><see cref="ISubgraphNode"/>s</description></item>
        /// </list>
        /// It excludes <see cref="BlockNode"/>s, which are only accessible through their parent <see cref="ContextNode"/>.
        /// </remarks>
        public IEnumerable<INode> GetNodes()
        {
            CheckImplementation();
            return m_Implementation.Nodes;
        }

        /// <summary>
        /// Called when the graph is created or loaded in the editor.
        /// </summary>
        /// <remarks>
        /// Override this method to perform setup tasks, such as allocating resources, initializing internal state, or preparing data for editing. This method is invoked each time
        /// the graph becomes active in the editor, including after domain reload or when reopening the asset. This method complements
        /// <see cref="OnDisable"/> and is useful for maintaining consistency across editor sessions.
        /// </remarks>
        public virtual void OnEnable() { }

        /// <summary>
        /// Called when the graph is unloaded, or goes out of scope in the editor.
        /// </summary>
        /// <remarks>
        /// Unity calls this method when the graph is disabled, or is destroyed.
        /// Override this method to release resources, clear temporary data, or perform any required cleanup. This method complements
        /// <see cref="OnEnable"/> and is useful for maintaining consistency across editor sessions.
        /// </remarks>
        public virtual void OnDisable() { }

        /// <summary>
        /// Called after the graph has changed.
        /// </summary>
        /// <param name="graphLogger">The <see cref="GraphLogger"/> that receives any errors or warnings related to the graph.</param>
        /// <remarks>
        /// Unity calls this method after any change to the graph. Override it to validate the graph's integrity
        /// and report issues using the provided <see cref="GraphLogger"/>. Use this method to detect invalid configurations,
        /// highlight issues in the editor, or provide user feedback.
        /// Do not modify the graph within this method, as it may cause instability or recursive updates.
        /// </remarks>
        public virtual void OnGraphChanged(GraphLogger graphLogger) { }
    }
}
