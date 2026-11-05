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
    ///
    ///- Lifecycle management (via <see cref="OnEnable"/>, <see cref="OnDisable"/>)
    ///- Change tracking (via <see cref="OnGraphChanged"/>)
    ///- Access to nodes and variables
    ///
    /// To register a graph type and associate it with a custom file extension and configuration options,
    /// apply the <see cref="GraphAttribute"/> to your custom <c>Graph</c> class.
    ///
    /// You can further control the graph's behavior using the
    /// <see cref="GraphOptions"/> enum, which defines traits such as support for subgraphs.
    /// If your graph supports subgraphs (via <see cref="GraphOptions.SupportsSubgraphs"/>), you can declare valid subgraph types
    /// using the <see cref="SubgraphAttribute"/>.
    ///
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
        /// The globally unique identifier for this graph.
        /// </summary>
        public Hash128 ID
        {
            get
            {
                CheckImplementation();
                return m_Implementation.Guid;
            }
        }

        /// <summary>
        /// The `GUID` of the asset file associated with this graph.
        /// </summary>
        /// <remarks>
        /// The default value of <see cref="GUID"/> for local subgraphs, because they are not stored as separate asset files.
        /// For graphs that are persistent assets, this property contains the valid unique identifier of the asset file on disk.
        /// </remarks>
        public GUID AssetGuid
        {
            get
            {
                CheckImplementation();
                // Rely on GraphReference instead of GraphObject as local subgraph asset GUID must be default(GUID)
                // and not the asset GUID of the root graph.
                return m_Implementation.GetGraphReference(true).AssetGuid;
            }
        }

        /// <summary>
        /// The set of types supported by this graph, including all port types, available variable types, and available constant types.
        /// </summary>
        /// <remarks>
        /// Override <see cref="BuildAvailableVariableTypes"/> or <see cref="BuildAvailableConstantTypes"/> to change
        /// which types are included. Do not call <see cref="SupportedTypes"/> from those methods; use the
        /// <c>baseSupportedTypes</c> parameter instead to avoid a circular dependency.
        /// </remarks>
        public IReadOnlyCollection<Type> SupportedTypes
        {
            get
            {
                CheckImplementation();
                return m_Implementation.SupportedTypes;
            }
        }

        /// <summary>
        /// The set of types offered in the blackboard for variable creation.
        /// </summary>
        /// <remarks>
        /// Override <see cref="BuildAvailableVariableTypes"/> to customize this set. Overriding that method also
        /// affects <see cref="SupportedTypes"/>, which includes this set.
        /// This controls what the UI offers, not what can be created. Variables of any type can still be created
        /// programmatically via <see cref="CreateVariable(string, Type, object, VariableKind)"/> regardless of this set.
        /// </remarks>
        public IReadOnlyCollection<Type> AvailableVariableTypes
        {
            get
            {
                CheckImplementation();
                return m_Implementation.AvailableVariableTypes;
            }
        }

        /// <summary>
        /// Builds the set of variable types offered in the blackboard for variable creation.
        /// </summary>
        /// <param name="baseSupportedTypes">Types auto-discovered from the graph's node port definitions, used as the base set to build upon.</param>
        /// <returns>The types to offer in the blackboard for variable creation, or <see langword="null"/> to offer none.</returns>
        /// <remarks>
        /// Override this method to filter or extend the default set. The default implementation returns
        /// <paramref name="baseSupportedTypes"/> unchanged.
        /// This controls what the UI offers, not what can be created. Variables of any type can still be created
        /// programmatically via <see cref="CreateVariable(string, Type, object, VariableKind)"/> regardless of this set.
        /// Use <paramref name="baseSupportedTypes"/> as the base set; do not call <see cref="SupportedTypes"/> from
        /// this method, as that would create a circular dependency and throw an <see cref="InvalidOperationException"/>.
        /// </remarks>
        /// <example>
        /// <code><![CDATA[
        /// protected override IEnumerable<Type> BuildAvailableVariableTypes(IReadOnlyCollection<Type> baseSupportedTypes)
        /// {
        ///     // Start from the auto-discovered types, then add or remove as needed.
        ///     foreach (var t in baseSupportedTypes)
        ///         if (t != typeof(Untyped))
        ///             yield return t;
        ///
        ///     yield return typeof(Vector3);
        /// }
        /// ]]></code>
        /// </example>
        protected virtual IEnumerable<Type> BuildAvailableVariableTypes(IReadOnlyCollection<Type> baseSupportedTypes)
            => baseSupportedTypes;

        /// <summary>
        /// The set of types offered in the graph item library for constant nodes.
        /// </summary>
        /// <remarks>
        /// Override <see cref="BuildAvailableConstantTypes"/> to customize this set. Overriding that method also
        /// affects <see cref="SupportedTypes"/>, which includes this set.
        /// This controls what the UI offers, not what can be created. Constant nodes of any type can still be created
        /// programmatically via <see cref="CreateConstantNode(UnityEngine.Vector2, Type, object)"/> regardless of this set.
        /// </remarks>
        public IReadOnlyCollection<Type> AvailableConstantTypes
        {
            get
            {
                CheckImplementation();
                return m_Implementation.AvailableConstantTypes;
            }
        }

        /// <summary>
        /// Builds the set of types offered in the graph item library for constant nodes.
        /// </summary>
        /// <param name="baseSupportedTypes">Types auto-discovered from the graph's node port definitions, used as the base set to build upon.</param>
        /// <returns>The types to offer in the item library for constant nodes, or <see langword="null"/> to offer none.</returns>
        /// <remarks>
        /// Override this method to filter or extend the default set. The default implementation returns
        /// <paramref name="baseSupportedTypes"/> unchanged.
        /// This controls what the UI offers, not what can be created. Constant nodes of any type can still be created
        /// programmatically via <see cref="CreateConstantNode(UnityEngine.Vector2, Type, object)"/> regardless of this set.
        /// Use <paramref name="baseSupportedTypes"/> as the base set; do not call <see cref="SupportedTypes"/> from
        /// this method, as that would create a circular dependency and throw an <see cref="InvalidOperationException"/>.
        /// </remarks>
        /// <example>
        /// <code><![CDATA[
        /// protected override IEnumerable<Type> BuildAvailableConstantTypes(IReadOnlyCollection<Type> baseSupportedTypes)
        /// {
        ///     // Start from the auto-discovered types, then add or remove as needed.
        ///     foreach (var t in baseSupportedTypes)
        ///         yield return t;
        ///
        ///     yield return typeof(Vector3);
        /// }
        /// ]]></code>
        /// </example>
        protected virtual IEnumerable<Type> BuildAvailableConstantTypes(IReadOnlyCollection<Type> baseSupportedTypes)
            => baseSupportedTypes;

        /// <summary>
        /// Creates and adds a new variable to the graph.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="valueType">The data type of the variable.</param>
        /// <param name="defaultValue">The default value. Must be compatible with <paramref name="valueType"/> and work with Unity serialization rules for <see cref="SerializeField"/>.</param>
        /// <param name="kind">The kind of variable, defined by <see cref="VariableKind"/>.</param>
        /// <returns>The newly created variable.</returns>
        /// <remarks>
        /// If successful, also adds the node's type to the graph's list of supported types.
        /// Enclose this method with <see cref="UndoBeginRecordGraph"/> and <see cref="UndoEndRecordGraph"/> to
        /// add this operation to the undo stack and to update the graph view with the changes.
        /// </remarks>
        public IVariable CreateVariable(string name, Type valueType, object defaultValue = null, VariableKind kind = VariableKind.Local)
        {
            CheckImplementation();
            return m_Implementation.CreateVariable(name, valueType, defaultValue, kind);
        }

        /// <summary>
        /// Creates and adds a new variable to the graph.
        /// </summary>
        /// <typeparam name="T">The data type of the variable.</typeparam>
        /// <param name="name">The name of the variable.</param>
        /// <param name="defaultValue">The default value. Must be compatible with <typeparamref name="T"/> and work with Unity serialization rules for <see cref="SerializeField"/>.</param>
        /// <param name="kind">The kind of variable, defined by <see cref="VariableKind"/>.</param>
        /// <returns>The newly created variable.</returns>
        /// <remarks>
        /// If successful, also adds the variable's type to the graph's list of supported types.
        /// Enclose this method with <see cref="UndoBeginRecordGraph"/> and <see cref="UndoEndRecordGraph"/> to
        /// add this operation to the undo stack and to update the graph view with the changes.
        /// </remarks>
        public IVariable CreateVariable<T>(string name, T defaultValue = default, VariableKind kind = VariableKind.Local)
        {
            return CreateVariable(name, typeof(T), defaultValue, kind);
        }

        /// <summary>
        /// Removes a variable from the Graph.
        /// </summary>
        /// <param name="variable">The variable to remove. Must belong to this graph.</param>
        /// <param name="forceRemove">If true, removes the variable and all variable nodes referencing it. If false, removal fails if nodes exist.</param>
        /// <returns>True if the variable was removed; otherwise false.</returns>
        /// <remarks>
        /// Enclose this method with <see cref="UndoBeginRecordGraph"/> and <see cref="UndoEndRecordGraph"/> to
        /// add this operation to the undo stack and to update the graph view with the changes.
        /// </remarks>
        public bool RemoveVariable(IVariable variable, bool forceRemove = false)
        {
            CheckImplementation();
            return m_Implementation.RemoveVariable(variable, forceRemove);
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
        /// To get the variables in a specific order, use <see cref="GetVariables(SortMethod)"/>.
        /// </remarks>
        public IEnumerable<IVariable> GetVariables()
        {
            CheckImplementation();
            return m_Implementation.VariableModels;
        }

        /// <summary>
        /// Retrieves all variables declared in the graph in a specific order using <see cref="SortMethod"/>.
        /// </summary>
        /// <param name="sort">The sorting method.</param>
        /// <returns>An <c>IEnumerable</c> of all <see cref="IVariable"/>s declared in the graph, ordered using the provided <see cref="SortMethod"/>.</returns>
        /// <remarks>
        /// Use this method to enumerate all <see cref="IVariable"/>s declared in the graph.
        /// This list does not include variable nodes that reference variables.
        /// The collection reflects the variables ordered using the provided <see cref="SortMethod"/>.
        ///
        ///- The <see cref="SortMethod.Creation"/> option returns variables in their order of creation.
        ///- The <see cref="SortMethod.Display"/> option returns variables in the order they are displayed in the blackboard.
        ///
        /// </remarks>
        public IEnumerable<IVariable> GetVariables(SortMethod sort)
        {
            CheckImplementation();

            switch (sort)
            {
                case SortMethod.Creation:
                    return m_Implementation.VariableModels;
                case SortMethod.Display:
                    return m_Implementation.VariableModelsByDisplayOrder;
                default:
                    throw new ArgumentException("Not expected sort method", nameof(sort));
            }
        }

        /// <summary>
        /// Adds a node to the Graph.
        /// </summary>
        /// <param name="node">The node to add.</param>
        /// <remarks>
        /// If the node is already in this graph, this method does nothing.
        /// If the node is currently in another graph, it will be removed from that graph and added to this one.
        /// Enclose this method with <see cref="UndoBeginRecordGraph"/> and <see cref="UndoEndRecordGraph"/> to
        /// add this operation to the undo stack and to update the graph view with the changes.
        /// </remarks>
        public void AddNode(Node node)
        {
            CheckImplementation();
            m_Implementation.AddNode(node);
        }

        /// <summary>
        /// Removes a node from the Graph.
        /// </summary>
        /// <param name="node">The node to remove.</param>
        /// <remarks>
        /// Enclose this method with <see cref="UndoBeginRecordGraph"/> and <see cref="UndoEndRecordGraph"/> to
        /// add this operation to the undo stack and to update the graph view with the changes.
        /// </remarks>
        public void RemoveNode(INode node)
        {
            CheckImplementation();
            m_Implementation.RemoveNode(node);
        }

        /// <summary>
        /// Creates and adds a new constant node to the graph.
        /// </summary>
        /// <param name="position">The position of the node.</param>
        /// <param name="valueType">The type of the value held by the constant.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The newly created constant node.</returns>
        /// <remarks>
        /// If successful, also adds the node's type to the graph's list of supported types.
        /// Enclose this method with <see cref="UndoBeginRecordGraph"/> and <see cref="UndoEndRecordGraph"/> to
        /// add this operation to the undo stack and to update the graph view with the changes.
        /// </remarks>
        public IConstantNode CreateConstantNode(Vector2 position, Type valueType, object defaultValue = null)
        {
            CheckImplementation();
            return m_Implementation.CreateConstantNode(position, valueType, defaultValue);
        }

        /// <summary>
        /// Creates and adds a new constant node to the graph.
        /// </summary>
        /// <typeparam name="T">The type of the value held by the constant.</typeparam>
        /// <param name="position">The position of the node.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The newly created constant node.</returns>
        /// <remarks>
        /// If successful, also adds the node's type to the graph's list of supported types.
        /// Enclose this method with <see cref="UndoBeginRecordGraph"/> and <see cref="UndoEndRecordGraph"/> to
        /// add this operation to the undo stack and to update the graph view with the changes.
        /// </remarks>
        public IConstantNode CreateConstantNode<T>(Vector2 position, T defaultValue = default)
        {
            return CreateConstantNode(position, typeof(T), defaultValue);
        }

        /// <summary>
        /// Creates and adds a new variable node referencing an existing variable.
        /// </summary>
        /// <param name="variable">Variable to reference. Must belong to this graph.</param>
        /// <param name="position">Position of the node on the graph canvas.</param>
        /// <returns>The newly created variable node.</returns>
        /// <remarks>
        /// The created node is in <see cref="VariableNodeMode.Get"/> mode.
        /// To create a node that can write to the variable, use
        /// <see cref="AddVariableNode(IVariable, Vector2, VariableNodeMode)"/> and pass <see cref="VariableNodeMode.Set"/>.
        /// Set mode is only available for variables of kind <see cref="VariableKind.Local"/>.
        ///
        /// Enclose this method with <see cref="UndoBeginRecordGraph"/> and <see cref="UndoEndRecordGraph"/> to
        /// add this operation to the undo stack and to update the graph view with the changes.
        /// </remarks>
        /// <example nocheck="true">
        /// <code lang="cs"><![CDATA[
        /// IVariable health = graph.CreateVariable<int>("Health");
        ///
        /// graph.UndoBeginRecordGraph("Add Variable Node");
        /// IVariableNode node = graph.AddVariableNode(health, new Vector2(100, 100));
        /// graph.UndoEndRecordGraph();
        ///
        /// // node.Mode == VariableNodeMode.Get
        /// // Health is a local variable, so the node has an output port that provides its current value.
        /// ]]></code>
        /// </example>
        public IVariableNode AddVariableNode(IVariable variable, Vector2 position)
        {
            CheckImplementation();
            return m_Implementation.AddVariableNode(variable, position);
        }

        /// <summary>
        /// Creates and adds a new variable node with the specified mode, referencing an existing variable.
        /// </summary>
        /// <param name="variable">Variable to reference. Must belong to this graph.</param>
        /// <param name="position">Position of the node on the graph canvas.</param>
        /// <param name="mode">Mode of the variable node.</param>
        /// <returns>The newly created variable node.</returns>
        /// <remarks>
        /// The <paramref name="mode"/> parameter determines the ports the node exposes. Refer to <see cref="VariableNodeMode"/>
        /// for details. <see cref="VariableNodeMode.Set"/> is only supported for variables of kind <see cref="VariableKind.Local"/>.
        ///
        /// To change the mode of an existing variable node, use the **Allow to set value in graph** checkbox in the node's inspector.
        ///
        /// Enclose this method with <see cref="UndoBeginRecordGraph"/> and <see cref="UndoEndRecordGraph"/> to
        /// add this operation to the undo stack and to update the graph view with the changes.
        /// </remarks>
        /// <example nocheck="true">
        /// <code lang="cs"><![CDATA[
        /// // Local variables support both Get and Set modes.
        /// IVariable speed = graph.CreateVariable<float>("Speed");
        ///
        /// graph.UndoBeginRecordGraph("Add Set Variable Node");
        /// IVariableNode setNode = graph.AddVariableNode(speed, new Vector2(100, 100), VariableNodeMode.Set);
        /// graph.UndoEndRecordGraph();
        ///
        /// // setNode.Mode == VariableNodeMode.Set
        /// // The node has an extra input port for writing the value of Speed in the graph.
        /// ]]></code>
        /// </example>
        public IVariableNode AddVariableNode(IVariable variable, Vector2 position, VariableNodeMode mode)
        {
            CheckImplementation();
            return m_Implementation.AddVariableNode(variable, position, mode);
        }

        /// <summary>
        /// Creates and adds a new subgraph node referencing an existing Graph asset.
        /// </summary>
        /// <param name="subgraph">The Graph asset to reference.</param>
        /// <param name="position">The position of the node.</param>
        /// <returns>The newly created subgraph node.</returns>
        /// <remarks>
        /// Enclose this method with <see cref="UndoBeginRecordGraph"/> and <see cref="UndoEndRecordGraph"/> to
        /// add this operation to the undo stack and to update the graph view with the changes.
        /// </remarks>
        public ISubgraphNode AddSubgraphNode(Graph subgraph, Vector2 position)
        {
            CheckImplementation();
            return m_Implementation.AddSubgraphNode(subgraph, position);
        }

        /// <summary>
        /// Creates and adds a new local subgraph node.
        /// </summary>
        /// <param name="subgraphType">The type of the subgraph to create.</param>
        /// <param name="name">The name of the local subgraph.</param>
        /// <param name="position">The position of the node.</param>
        /// <returns>The newly created subgraph node.</returns>
        /// <remarks>
        /// Enclose this method with <see cref="UndoBeginRecordGraph"/> and <see cref="UndoEndRecordGraph"/> to
        /// add this operation to the undo stack and to update the graph view with the changes.
        /// </remarks>
        public ISubgraphNode CreateLocalSubgraphNode(Type subgraphType, string name, Vector2 position)
        {
            CheckImplementation();
            return m_Implementation.CreateLocalSubgraphNode(subgraphType, name, position);
        }

        /// <summary>
        /// Creates and adds a new local subgraph node.
        /// </summary>
        /// <typeparam name="TSubGraph">The type of the subgraph to create.</typeparam>
        /// <param name="name">The name of the local subgraph.</param>
        /// <param name="position">The position of the node.</param>
        /// <returns>The newly created subgraph node.</returns>
        /// <remarks>
        /// Enclose this method with <see cref="UndoBeginRecordGraph"/> and <see cref="UndoEndRecordGraph"/> to
        /// add this operation to the undo stack and to update the graph view with the changes.
        /// </remarks>
        public ISubgraphNode CreateLocalSubgraphNode<TSubGraph>(string name, Vector2 position) where TSubGraph : Graph, new()
        {
            return CreateLocalSubgraphNode(typeof(TSubGraph), name, position);
        }

        /// <summary>
        /// Creates a wire connection between two ports.
        /// </summary>
        /// <param name="output">The output port to connect from.</param>
        /// <param name="input">The input port to connect to.</param>
        /// <returns>true if the connection was created; false if the connection already existed.</returns>
        /// <remarks>
        /// Enclose this method with <see cref="UndoBeginRecordGraph"/> and <see cref="UndoEndRecordGraph"/> to
        /// add this operation to the undo stack and to update the graph view with the changes.
        /// </remarks>
        public bool Connect(IPort output, IPort input)
        {
            CheckImplementation();
            return m_Implementation.Connect(output, input);
        }

        /// <summary>
        /// Removes the wire connection between two ports.
        /// </summary>
        /// <param name="output">The output port to disconnect.</param>
        /// <param name="input">The input port to disconnect.</param>
        /// <returns>true if a connection (wire or portal) existed and was removed; otherwise false.</returns>
        /// <remarks>
        /// This method removes direct wires and any Portals that serve only this specific connection.
        /// Enclose this method with <see cref="UndoBeginRecordGraph"/> and <see cref="UndoEndRecordGraph"/> to
        /// add this operation to the undo stack and to update the graph view with the changes.
        /// </remarks>
        public bool Disconnect(IPort output, IPort input)
        {
            CheckImplementation();
            return m_Implementation.DeleteWiresBetween(output, input);
        }

        /// <summary>
        /// Gets the representation of the wire between an output port and an input port in the graph, if such a connection exists.
        /// </summary>
        /// <param name="output">The output port at the start of the connection.</param>
        /// <param name="input">The input port at the end of the connection.</param>
        /// <returns>
        /// A <see cref="Wire"/> when the ports are connected (including through portals); otherwise <c>null</c>.
        /// </returns>
        public Wire GetWire(IPort output, IPort input)
        {
            CheckImplementation();
            return m_Implementation.GetWire(output, input);
        }

        /// <summary>
        /// Retrieves a node defined in the graph by its index.
        /// </summary>
        /// <param name="index">The zero-based index of the node to retrieve.</param>
        /// <returns>The <see cref="INode"/> at the specified index.</returns>
        /// <remarks>
        /// Use this method to access a node based on its creation order in the graph. The index is zero-based and must be within range (see: <see cref="NodeCount"/>).
        ///
        /// The list includes:
        ///
        ///- Your own <see cref="Node"/>s
        ///- <see cref="ContextNode"/>s
        ///- <see cref="IVariableNode"/>s
        ///- <see cref="IConstantNode"/>s
        ///- <see cref="ISubgraphNode"/>s
        ///
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
        ///
        /// The list includes:
        ///
        ///- Your own <see cref="Node"/>s
        ///- <see cref="ContextNode"/>s
        ///- <see cref="IVariableNode"/>s
        ///- <see cref="IConstantNode"/>s
        ///- <see cref="ISubgraphNode"/>s
        ///
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
        /// the graph becomes active in the editor, including after code reload or when reopening the asset. This method complements
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
        /// <param name="graphLogger">The <see cref="GraphLogger"/> that receives any errors or warnings related to the graph,
        /// and provides access to information about what changed (added/deleted/modified nodes).</param>
        /// <remarks>
        /// Unity calls this method after any change to the graph. Override it to validate the graph's integrity,
        /// log issues using the provided <see cref="GraphLogger"/>, or react to specific changes via
        /// <see cref="GraphLogger.GraphChanges"/>.
        ///
        /// Use this method to detect invalid configurations, highlight issues in the editor, or provide user feedback.
        /// You can iterate the changed nodes efficiently without checking all nodes in the graph.
        ///
        /// Do not modify the graph within this method, as it may cause instability or recursive updates.
        /// </remarks>
        public virtual void OnGraphChanged(GraphLogger graphLogger) { }

        /// <summary>
        /// Signals the beginning of an undoable operation.
        /// </summary>
        /// <param name="actionName">The name of the operation, which is displayed in the undo menu.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if an undo operation has already been registered to the Graph.
        /// </exception>
        /// <remarks>
        /// Call this before you trigger a sequence of graph modification methods to record those operations to the undo stack.
        /// Call <see cref="UndoEndRecordGraph"/> after the sequence to signal that the operation is complete.
        ///
        /// Throws <c>InvalidOperationException</c> if there is no undo operation currently registered to the Graph.
        /// </remarks>
        public void UndoBeginRecordGraph(string actionName)
        {
            CheckImplementation();
            m_Implementation.UndoBeginRecordGraph(actionName);
        }

        /// <summary>
        /// Signals the beginning of an undoable operation and opts specific nodes into full serialized-state capture.
        /// </summary>
        /// <param name="actionName">The name of the operation, which is displayed in the undo menu.</param>
        /// <param name="nodesToRecord">
        /// The nodes whose serialized state you plan to mutate inside this scope. Each node must belong to this graph.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if an undo operation has already been registered to the Graph.
        /// </exception>
        /// <remarks>
        /// Use this overload when your custom <see cref="Node"/> subclasses expose <c>[SerializeField]</c> fields that you
        /// mutate directly (for example, from a <see cref="NodeView{T}"/> callback), rather than through the built-in
        /// graph modification methods.
        ///
        /// Passing nodes here has two effects at <see cref="UndoEndRecordGraph"/> time:
        /// <list type="bullet">
        /// <item><description>
        /// Unity's undo system captures the full serialized state of the containing graph asset at Begin, so undo restores
        /// the previous values of your <c>[SerializeField]</c> fields.
        /// </description></item>
        /// <item><description>
        /// The nodes are reported as changed to <see cref="OnGraphChanged"/> and the graph asset is marked dirty.
        /// </description></item>
        /// </list>
        ///
        /// <para>
        /// The change-notification side effects (<see cref="OnGraphChanged"/> and automatic dirty-marking) run
        /// inside <see cref="UndoEndRecordGraph"/> only when the graph is currently being displayed in a GraphView
        /// window. From an editor script or other headless context with no open window, undo capture via
        /// <c>Undo.RegisterCompleteObjectUndo</c> still occurs and undo restores your <c>[SerializeField]</c> values
        /// as expected, but <see cref="OnGraphChanged"/> is not invoked and the asset is not automatically marked dirty.
        /// To persist your changes in that case, call <see cref="GraphDatabase.SaveGraph"/> explicitly.
        /// </para>
        ///
        /// If you only call the built-in graph modification methods, use the parameterless <see cref="UndoBeginRecordGraph(string)"/> overload instead.
        /// </remarks>
        public void UndoBeginRecordGraph(string actionName, params Node[] nodesToRecord)
        {
            CheckImplementation();
            m_Implementation.UndoBeginRecordGraph(actionName, nodesToRecord);
        }

        /// <summary>
        /// Signals the end of an undoable operation. Sends the undo data to the editor undo system and refreshes the graph view
        /// with the changes made.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is no undo operation currently registered to the Graph.
        /// </exception>
        /// <remarks>
        /// Call this after you trigger a sequence of graph modification methods to finalize recording the operations to the
        /// undo stack. Call <see cref="UndoBeginRecordGraph"/> before the sequence to signal the operations to be recorded.
        ///
        /// Throws <c>InvalidOperationException</c> if there is no undo operation currently registered to the Graph.
        /// </remarks>
        public void UndoEndRecordGraph()
        {
            CheckImplementation();
            m_Implementation.UndoEndRecordGraph();
        }

        /// <summary>
        /// Determines whether a connection between the specified output and input ports is allowed.
        /// </summary>
        /// <param name="output">The output port from which the connection originates.</param>
        /// <param name="input">The input port to which the connection is being made.</param>
        /// <returns>
        /// <c>true</c> if a connection between the specified ports is allowed; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// The default implementation checks type compatibility by verifying that the input port's
        /// <see cref="IPort.DataType"/> is assignable from the output port's <see cref="IPort.DataType"/>.
        /// This allows connections between ports of the same type or where the output type is derived
        /// from the input type.
        /// <para>
        /// Override this method in derived graph classes to implement custom connection validation logic,
        /// such as additional type constraints, node-specific rules, or graph-level restrictions.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code lang="cs">
        /// <![CDATA[
        /// public override bool IsConnectionAllowed(IPort output, IPort input)
        /// {
        ///     // Allow connection if the output is a string and the input is an int
        ///     if (output.DataType == typeof(string) && input.DataType == typeof(int))
        ///         return true;
        ///
        ///     // Fallback on the default behaviour
        ///     return base.IsConnectionAllowed(output, input);
        /// }
        /// ]]>
        /// </code>
        /// </example>
        public virtual bool IsConnectionAllowed(IPort output, IPort input)
        {
            // Avoid connection from object to Untyped
            if (output.DataType == typeof(Untyped) || input.DataType == typeof(Untyped))
                return output.DataType == input.DataType;

            return input.DataType.IsAssignableFrom(output.DataType);
        }

        /// <summary>
        /// Called to define the <see cref="INodeOption"/>s available on subgraph nodes that reference this type of graph.
        /// Similar in function to <see cref="Node.OnDefineOptions"/>.
        /// </summary>
        /// <param name="context">Provides methods for defining node options.</param>
        /// <remarks>
        /// Override this method to add options to all subgraph nodes that point to this type of graph using the provided <see cref="Node.IOptionDefinitionContext"/>.
        /// This method is only applicable to graphs that can act as subgraphs. If the graph is not a subgraph, this method has no effect. To qualify as a subgraph, the graph must be marked with <see cref="SubgraphAttribute"/>.
        /// </remarks>
        /// <example>
        /// <code lang="cs">
        /// <![CDATA[
        /// protected override void OnDefineSubgraphNodeOptions(Node.IOptionDefinitionContext context)
        /// {
        ///     context.AddOption<int>("ID")
        ///         .WithTooltip("The ID of the subgraph.")
        ///         .Delayed();
        ///
        ///     context.AddOption<string>("Description")
        ///         .WithDefaultValue("What is the purpose of this subgraph?")
        ///         .Delayed();
        /// }
        /// ]]>
        /// </code>
        /// </example>
        protected virtual void OnDefineSubgraphNodeOptions(Node.IOptionDefinitionContext context) { }
    }
}
