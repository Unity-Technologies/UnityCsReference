// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace Unity.GraphToolkit.Editor.Implementation
{
    [Serializable]
    class GraphModelImp : GraphModel
    {
        List<Type> m_SupportedTypes;
        IReadOnlyList<Type> m_SupportedNodes;

        [SerializeReference]
        Graph m_Graph;

        [NonSerialized]
        UndoStateComponent m_CurrentUndoStateComponent;

        [NonSerialized]
        List<INode> m_Nodes;

        public Graph Graph => m_Graph;

        public override bool AllowSubgraphCreation => Graph?.GetType().GetCustomAttribute<GraphAttribute>()?.Options.HasFlag(GraphOptions.SupportsSubgraphs) ?? false;

        public override bool AllowDeleteAndReconnect => true;

        // This method is meant to be called on new GraphObjectImps, before OnEnable is called, to override the default behaviour which is to create the graph based on the GraphObjectImp.GraphType, if it is null.
        internal void InstantiateGraph(Type graphType)
        {
            if (m_Graph != null)
            {
                Debug.LogError("InstantiateGraph called while Graph was already created.");
            }
            m_Graph = (Graph)Activator.CreateInstance(graphType);
        }

        public override void OnEnable()
        {
            var graphObject = GraphObject as GraphObjectImp;
            if (graphObject != null && m_Graph == null)
            {
                var graphType = graphObject.GraphType;
                if (graphType != null)
                {
                    m_Graph = (Graph)Activator.CreateInstance(graphType);
                }
            }

            foreach (var variable in VariableDeclarations)
            {
                if (VariableDeclarationRequiresInitialization(variable) && variable.InitializationModel == null)
                {
                    variable.CreateInitializationValue();
                }
            }

            if (m_Graph != null)
            {
                m_Graph.SetImplementation(this);

                base.OnEnable();

                LockForModification = true;
                try
                {
                    m_Graph.OnEnable();

                    foreach (var nodeModel in NodeModels)
                    {
                        if (nodeModel is IUserNodeModelImp userNodeModelImp)
                        {
                            userNodeModelImp.CallOnEnable();
                        }
                    }
                }
                finally
                {
                    LockForModification = false;
                }
            }
        }

        public override void OnDisable()
        {
            LockForModification = true;
            try
            {
                foreach (var nodeModel in NodeModels)
                {
                    if (nodeModel is IUserNodeModelImp userNodeModelImp)
                    {
                        userNodeModelImp.CallOnDisable();
                    }
                }
                m_Graph?.OnDisable();
            }
            finally
            {
                LockForModification = false;
            }

            base.OnDisable();
        }

        public IReadOnlyList<IVariable> VariableModels => this.VariableDeclarations;
        public IReadOnlyList<IVariable> VariableModelsByDisplayOrder => GetVariableDeclarationsByDisplayOrder();

        protected override Type VariableNodeType => typeof(VariableNodeModelImp);
        protected override Type SubgraphNodeType => typeof(SubgraphNodeModelImp);
        protected override Type ConstantNodeType => typeof(ConstantNodeModelImp);

        public override bool CanAssignTo(PortModel destination, PortModel source)
        {
            if (m_Graph != null)
                return m_Graph.IsConnectionAllowed(source, destination);

            if (destination.PortDataType == typeof(Untyped))
                return source.PortDataType == typeof(Untyped);

            return destination.PortDataType.IsAssignableFrom(source.PortDataType);
        }

        public NodeModel CreateNodeModel(Node node, Vector2 position)
        {
            if (node is ContextNode contextNode)
            {
                return CreateNode<UserContextNodeModelImp>(position : position, initializationCallback:n =>n.InitCustomNode(contextNode));
            }

            if (node is BlockNode blockNode)
            {
                return CreateNode<UserBlockNodeModelImp>(initializationCallback:n =>n.InitCustomNode(blockNode));
            }
            return CreateNode<UserNodeModelImp>(position : position, initializationCallback:n =>n.InitCustomNode(node));
        }

        //public override object ToolbarActionsObject => Graph;

        public IReadOnlyList<INode> Nodes
        {
            get
            {
                BuildNodesFromNodeModels();
                return m_Nodes;
            }
        }

        // Disallow modifications while in OnEnable, OnDisable and OnGraphChanged
        bool LockForModification { get; set; }

        internal void CheckModificationLock()
        {
            if (LockForModification)
                throw new InvalidOperationException("Cannot change the graph in OnEnable, OnDisable and OnGraphChanged.");
        }

        public override bool VariableDeclarationRequiresInitialization(VariableDeclarationModelBase _)
        {
            // We want all variables to have a default value field.
            return true;
        }

        public void UndoBeginRecordGraph(string actionName )
        {
            CheckModificationLock();

            var window = GraphViewEditorWindowImp.GetOpenedWindow((GraphObjectImp)GraphObject);

            if (window?.GraphTool?.UndoState != null && window.GraphView?.GraphModel == this)
            {
                if (m_CurrentUndoStateComponent != null)
                {
                    throw new InvalidOperationException("An undo operation has already been registered to the Graph.");
                }

                m_CurrentUndoStateComponent = window.GraphTool.UndoState;
                m_CurrentUndoStateComponent.BeginOperation(actionName);
                using (var undoStateUpdater = m_CurrentUndoStateComponent.UpdateScope)
                {
                    undoStateUpdater.SaveState(window.GraphView.GraphViewModel.GraphModelState);
                }
                PushNewGraphChangeDescription();
            }
            else
            {
                Undo.RegisterCompleteObjectUndo(GraphObject, actionName);
            }
        }

        public override Constant CreateConstantValue(TypeHandle constantTypeHandle)
        {
            try
            {
                return base.CreateConstantValue(constantTypeHandle);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return null;
        }

        public void UndoEndRecordGraph()
        {
            CheckModificationLock();

            var window = GraphViewEditorWindowImp.GetOpenedWindow((GraphObjectImp)GraphObject);

            if (window != null && window.GraphView?.GraphModel == this)
            {
                try
                {
                    if (m_CurrentUndoStateComponent == null)
                    {
                        throw new InvalidOperationException(
                            "There is no undo operation currently registered to the Graph. Use RegisterUndo to begin recording an undo operation.");
                    }

                    var currentGraphModelStateUpdater = window.GraphView.GraphViewModel.GraphModelState.UpdateScope;
                    currentGraphModelStateUpdater.MarkUpdated(CurrentGraphChangeDescription);
                    currentGraphModelStateUpdater.Dispose();
                    PopGraphChangeDescription();
                    m_CurrentUndoStateComponent.EndOperation();
                }
                finally
                {
                    m_CurrentUndoStateComponent = null;
                }
            }
        }

        protected override void CreateGraphProcessors()
        {
            base.CreateGraphProcessors();

            var declaringType = Graph?.GetType().GetMethod(nameof(GraphToolkit.Editor.Graph.OnGraphChanged))?.DeclaringType;
            var overridden = declaringType != null && declaringType != typeof(Graph);

            if (overridden)
                GetGraphProcessorContainer().AddGraphProcessor(new GraphProcessorImp(this));
        }

        public IVariable CreateVariable(string name, Type valueType, object defaultValue = null, VariableKind kind = VariableKind.Local)
        {
            CheckModificationLock();

            TypeHandle typeHandle;
            if (valueType == null)
            {
                if (defaultValue != null)
                    throw new ArgumentException("Cannot provide a default value for an Untyped variable (valueType is null).", nameof(defaultValue));

                typeHandle = TypeHandle.Untyped;
            }
            else
            {
                if (defaultValue != null)
                {
                    if (!InternalTypeHelpers.IsTypeSerializable(valueType))
                        throw new ArgumentException($"The type '{valueType.Name}' is not serializable. " +
                                                    $"You cannot provide a default value for it as it will be lost.", nameof(defaultValue));

                    if (defaultValue.GetType() != valueType)
                        throw new ArgumentException($"The default value type ({defaultValue.GetType().Name}) " +
                                                    $"must exactly match the variable type ({valueType.Name})", nameof(defaultValue));
                }

                typeHandle = valueType.GenerateTypeHandle();
            }

            var constant = CreateConstantValue(typeHandle);
            if( defaultValue != null )
                constant.ObjectValue = defaultValue;

            var result = CreateGraphVariableDeclaration(
                typeHandle,
                name,
                kind == VariableKind.Input ? ModifierFlags.Read : (kind == VariableKind.Output ? ModifierFlags.Write : ModifierFlags.None),
                (kind != VariableKind.Local) ? VariableScope.Exposed : VariableScope.Local,
                initializationModel: constant
            );

            if (result != null && result.DataType.Resolve() is { } variableType)
            {
                if (!SupportedTypes.Contains(variableType))
                {
                    m_SupportedTypes.Add(variableType);
                }
            }
            return result;
        }

        public bool RemoveVariable(IVariable variable, bool forceRemove)
        {
            CheckModificationLock();

            if (variable == null)
                throw new ArgumentNullException(nameof(variable));

            if (variable is not VariableDeclarationModelBase variableModel)
                return false;

            if (variable.Graph != Graph)
                throw new ArgumentException("The variable provided does not belong to this graph.", nameof(variable));

            if (!VariableDeclarations.Contains(variableModel))
                return false;

            // If we are not force removing, and there are still nodes referencing the variable declaration. return false
            if (!forceRemove)
            {
                using (var disposableReferences = ListPool<AbstractNodeModel>.Get(out List<AbstractNodeModel> references))
                {
                    FindReferencesInGraph(variableModel, references);
                    if (references.Count > 0)
                        return false;
                }
            }

            DeleteVariableDeclaration(variableModel, deleteUsages: forceRemove);
            return true;
        }

        internal void AddNode(Node node)
        {
            CheckModificationLock();

            if (node == null)
                throw new ArgumentNullException(nameof(node));

            if (node is BlockNode)
                throw new ArgumentException("Cannot add a BlockNode directly to a Graph. Use ContextNode.AddBlockNode instead.");

            if (!IsNodeCompatible(node))
            {
                throw new ArgumentException($"Node '{node.GetType().Name}' is not compatible with this graph type ({Graph.GetType().Name}). Ensure it is decorated with [UseWithGraph] or is in the same assembly.");
            }

            var previousGraph = node.Graph;

            // If already here, do nothing.
            if (previousGraph == m_Graph)
            {
                return;
            }

            // Reparenting: Remove from old graph first.
            if (previousGraph != null)
            {
                previousGraph.RemoveNode(node);
            }

            var nodeImp = node.GetImplementation();

            // Perform node initialization (similar to InstantiateNode behavior)
            nodeImp.GraphModel = this;
            nodeImp.OnCreateNode();
            AddNode(nodeImp);
        }

        internal void RemoveNode(INode node)
        {
            CheckModificationLock();

            if (node == null)
                throw new ArgumentNullException(nameof(node));

            if (!(node is Node || node is AbstractNodeModel))
                throw new ArgumentException($"The provided INode ('{node.GetType().Name}') is not a valid internal node implementation.", nameof(node));

            if (node.Graph != Graph)
                throw new ArgumentException("The node provided does not belong to this graph.", nameof(node));

            switch (node)
            {
                case BlockNode blockNode:
                    DeleteNode(blockNode.GetImplementation(), deleteConnections: true);
                    break;
                case Node userNode:
                    DeleteNode(userNode.GetImplementation(), deleteConnections: true);
                    break;
                case AbstractNodeModel abstractNode:
                    DeleteNode(abstractNode, deleteConnections: true);
                    break;
                default:
                    DeleteNode(node.NodeModel, deleteConnections: true);
                    break;
            }
        }

        public IConstantNode CreateConstantNode(Vector2 position, Type valueType, object defaultValue = null)
        {
            CheckModificationLock();

            if (valueType == null)
                throw new ArgumentNullException(nameof(valueType));

            if (!InternalTypeHelpers.IsTypeSerializable(valueType))
            {
                throw new ArgumentException($"The type '{valueType.Name}' is not serializable. Constant nodes require serializable types.", nameof(valueType));
            }

            if (defaultValue != null && defaultValue.GetType() != valueType)
            {
                throw new ArgumentException($"Default value type {defaultValue.GetType()} does not match constant type {valueType}.", nameof(defaultValue));
            }

            var typeHandle = valueType.GenerateTypeHandle();

            var nodeModel = base.CreateConstantNode(typeHandle, string.Empty, position, initializationCallback: n =>
            {
                if (defaultValue != null)
                    n.Value.ObjectValue = defaultValue;
            });

            // Add to supported types for Blackboard compatibility
            if (nodeModel != null && !SupportedTypes.Contains(valueType))
            {
                m_SupportedTypes.Add(valueType);
            }

            return (IConstantNode)nodeModel;
        }

        public IVariableNode AddVariableNode(IVariable variable, Vector2 position)
        {
            CheckModificationLock();

            if (variable == null)
                throw new ArgumentNullException(nameof(variable));

            if (variable.Graph != Graph)
            {
                throw new ArgumentException("The variable does not belong to this graph.", nameof(variable));
            }

            if (variable is not VariableDeclarationModel declModel)
                throw new ArgumentException("Invalid variable implementation.", nameof(variable));

            if (!VariableDeclarations.Contains(declModel))
            {
                throw new ArgumentException("The variable declaration doesn't exist in the graph. It may have been removed", nameof(variable));
            }

            return (IVariableNode)base.CreateVariableNode(declModel, position);
        }

        public ISubgraphNode AddSubgraphNode(Graph subgraph, Vector2 position)
        {
            CheckModificationLock();

            if (subgraph == null)
                throw new ArgumentNullException(nameof(subgraph));

            if (!AllowSubgraphCreation)
                throw new InvalidOperationException("This graph does not support subgraphs.");

            // If local subgraph, throw
            if (subgraph.m_Implementation is GraphModelImp { IsLocalSubgraph: true })
            {
                throw new ArgumentException("Cannot add a Local Subgraph directly. Use CreateLocalSubgraphNode to create a new local instance.");
            }

            // Compatibility Check
            var validTypes = PublicGraphFactory.GetSubGraphTypes(Graph.GetType());
            if (!validTypes.Contains(subgraph.GetType()))
            {
                throw new ArgumentException($"The subgraph type '{subgraph.GetType().Name}' is not compatible with '{Graph.GetType().Name}'.");
            }

            // We need the GraphModel of the target to create the node reference
            var targetModel = subgraph.m_Implementation as GraphModel;
            if (targetModel == null)
                throw new ArgumentException("Invalid subgraph implementation.");

            return (ISubgraphNode)base.CreateSubgraphNode(targetModel, position);
        }

        public ISubgraphNode CreateLocalSubgraphNode(Type subgraphType, string name, Vector2 position)
        {
            CheckModificationLock();

            if (subgraphType == null)
                throw new ArgumentNullException(nameof(subgraphType));

            if (!AllowSubgraphCreation)
                throw new InvalidOperationException("This graph does not support subgraphs.");

            if (!typeof(Graph).IsAssignableFrom(subgraphType) || subgraphType.IsAbstract)
            {
                throw new ArgumentException("Subgraph type must be a concrete class deriving from Graph.", nameof(subgraphType));
            }

            // Compatibility Check
            var validTypes = PublicGraphFactory.GetSubGraphTypes(Graph.GetType());
            if (!validTypes.Contains(subgraphType))
            {
                throw new ArgumentException($"The subgraph type '{subgraphType.Name}' is not compatible with '{Graph.GetType().Name}'.");
            }

            // Create the local subgraph model
            name ??= SubgraphCreationHelper.defaultLocalSubgraphName;
            var template = new SubgraphTemplateImp(subgraphType, name);
            var localSubgraphModel = CreateLocalSubgraph(typeof(GraphModelImp), name, template);

            // Create the node referencing it
            return (ISubgraphNode)base.CreateSubgraphNode(localSubgraphModel, position);
        }

        public bool Connect(IPort output, IPort input)
        {
            CheckModificationLock();

            // Null checks
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            if (input == null)
                throw new ArgumentNullException(nameof(input));

            // Validate Order : Output -> Input
            if (output.Direction != PortDirection.Output)
                throw new ArgumentException($"The 'output' parameter must be an Output port. It was {output.Direction}.", nameof(output));

            if (input.Direction != PortDirection.Input)
                throw new ArgumentException($"The 'input' parameter must be an Input port. It was {input.Direction}.", nameof(input));

            // Ownership Validation
            if (output.GetNode().Graph != Graph)
                throw new ArgumentException("The output port does not belong to this graph.", nameof(output));

            if (input.GetNode().Graph != Graph)
                throw new ArgumentException("The input port does not belong to this graph.", nameof(input));

            var outputModel = (PortModel)output;
            var inputModel = (PortModel)input;

            // Check Basic Compatibility (Types)
            if (!IsCompatiblePort(outputModel, inputModel))
            {
                if (!CanAssignTo(inputModel, outputModel))
                    throw new ArgumentException($"Ports are incompatible. Cannot connect type {TypeHelpers.GetFriendlyName(output.DataType)} to {TypeHelpers.GetFriendlyName(input.DataType)}.");

                // Check Self-Connection
                if (outputModel.NodeModel == inputModel.NodeModel)
                    throw new ArgumentException("Cannot connect a node to itself.");

                throw new ArgumentException("Ports are not compatible.");
            }

            //  Check Capacity
            if (inputModel.Capacity == PortCapacity.Single && inputModel.IsConnected())
                throw new ArgumentException("Input port capacity reached. Cannot connect multiple wires to a Single capacity port.");

            if (outputModel.Capacity == PortCapacity.Single && outputModel.IsConnected())
                throw new ArgumentException("Output port capacity reached.");

            // Check Existing Connection
            bool alreadyConnected = GetAnyWireConnectedToPorts(inputModel, outputModel) != null;
            if (alreadyConnected)
                return false;

            return CreateWire(inputModel, outputModel) != null;
        }

        public Wire GetWire(IPort output, IPort input)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (output.Direction != PortDirection.Output)
                throw new ArgumentException($"The 'output' parameter must be an Output port. It was {output.Direction}.", nameof(output));

            if (input.Direction != PortDirection.Input)
                throw new ArgumentException($"The 'input' parameter must be an Input port. It was {input.Direction}.", nameof(input));

            if (output.GetNode().Graph != Graph)
                throw new ArgumentException("The output port does not belong to this graph.", nameof(output));

            if (input.GetNode().Graph != Graph)
                throw new ArgumentException("The input port does not belong to this graph.", nameof(input));

            var outputModel = (PortModel)output;
            var inputModel = (PortModel)input;

            if (VirtualWireBuilder.TryGetVirtualWire(outputModel, inputModel, out var virtualWire))
                return new Wire(output, input, virtualWire);

            return null;
        }

        public bool DeleteWiresBetween(IPort output, IPort input)
        {
            CheckModificationLock();

            // Null checks
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            // Direction check
            if (input.Direction == output.Direction)
            {
                return false;
            }
            if (output.Direction == PortDirection.Input)
            {
                (output, input) = (input, output);
            }

            var outputModel = (PortModel)output;
            var inputModel = (PortModel)input;
            using var dispose = ListPool<GraphElementModel>.Get( out var elementsToDelete);

            // A. Check Direct Wires
            foreach (var wire in WireModels)
            {
                if (wire.FromPort == outputModel && wire.ToPort == inputModel)
                {
                    elementsToDelete.Add(wire);
                }
            }

            // B. Portal Connections
            foreach (var wireFromOutput in outputModel.GetConnectedWires())
            {
                if (wireFromOutput.ToPort.NodeModel is WirePortalEntryModel entryPortal)
                {
                    var declaration = entryPortal.DeclarationModel;
                    var exitPortals = GetExitPortals(declaration);

                    foreach (var exitPortal in exitPortals)
                    {
                        var exitPortalModel = ((WirePortalExitModel)exitPortal);
                        foreach (var wireToInput in exitPortalModel.OutputPort.GetConnectedWires())
                        {
                            if (wireToInput.ToPort == inputModel)
                            {
                                // Always delete the final wire (Exit -> Input)
                                elementsToDelete.Add(wireToInput);

                                // Check if the Entry Portal is serving other Exits
                                int activeExits = 0;
                                foreach (var otherExit in exitPortals)
                                {
                                    if (exitPortalModel.OutputPort.IsConnected())
                                        activeExits++;
                                }

                                // If this was the only active chain, we cleanup the Entry side too.
                                bool isLastConnection = activeExits <= 1;

                                if (isLastConnection)
                                {
                                    elementsToDelete.Add(wireFromOutput);
                                    elementsToDelete.Add(entryPortal);
                                    elementsToDelete.Add(exitPortal);
                                }
                                else
                                {
                                    // Only remove the specific Exit portal used for this connection
                                    elementsToDelete.Add(exitPortal);
                                }
                            }
                        }
                    }
                }
            }


            // Execution
            if (elementsToDelete.Count > 0)
            {
                DeleteElements(elementsToDelete);
                return true;
            }

            return false;
        }

        public override bool CanPasteNode(AbstractNodeModel originalModel)
        {
            switch (originalModel)
            {
                case IUserNodeModelImp customNodeModel:
                    return IsNodeCompatible(customNodeModel.Node);

                case VariableNodeModel variableNodeModel:
                    return variableNodeModel.VariableDeclarationModel.GetType() == typeof(VariableDeclarationModel) && SupportedTypes.Contains(variableNodeModel.VariableDeclarationModel.DataType.Resolve());

                case ConstantNodeModel constantNodeModel:
                    return SupportedTypes.Contains(constantNodeModel.Type);

                case WirePortalModel portalNodeModel:
                    return SupportedTypes.Contains(portalNodeModel.GetPortDataTypeHandle().Resolve());

                case SubgraphNodeModel subgraphNodeModel:
                    var subgraph = (subgraphNodeModel.GetSubgraphModel() as GraphModelImp)?.Graph ??
                                   (GraphReference.ResolveGraphModel(subgraphNodeModel.SubgraphReference) as GraphModelImp)?.Graph;

                    if (subgraph == null)
                    {
                        Debug.LogWarning("Cannot paste subgraph node because the referenced subgraph could not be resolved.");
                        return false;
                    }

                    var subgraphTypes = PublicGraphFactory.GetSubGraphTypes(Graph.GetType());

                    foreach (var subgraphType in subgraphTypes)
                    {
                        if (subgraphType.IsInstanceOfType(subgraph))
                            return true;
                    }

                    break;
            }

            return false;
        }

        bool IsNodeCompatible(Node node)
        {
            var graphType = m_Graph.GetType();

            // If the attribute is present, we do not fall into auto inclusion
            var attr = node.GetType().GetCustomAttribute<UseWithGraphAttribute>(true);
            if (attr != null)
            {
                return attr.IsGraphTypeSupported(graphType);
            }

            // Default behaviour : Check Assembly Auto-inclusion rules
            var graphAttr = graphType.GetCustomAttribute<GraphAttribute>();
            bool autoInclude = graphAttr == null || !graphAttr.Options.HasFlag(GraphOptions.DisableAutoInclusionOfNodesFromGraphAssembly);
            if (autoInclude && node.GetType().Assembly == graphType.Assembly)
            {
                return true;
            }

            return false;
        }

        public override bool CanPasteVariable(VariableDeclarationModelBase originalModel)
        {
            return originalModel is VariableDeclarationModel &&
                   SupportedTypes.Contains(originalModel.DataType.Resolve());
        }

        public override bool CanBeDroppedInOtherGraph(GraphModel otherGraph)
        {
            if (otherGraph is GraphModelImp otherGraphModelImp)
            {
                var validSubgraphTypesForOtherGraph = PublicGraphFactory.GetSubGraphTypes(otherGraphModelImp.Graph.GetType());
                var droppedGraphType = Graph.GetType();
                return validSubgraphTypesForOtherGraph.Contains(droppedGraphType);
            }

            return false;
        }

        public override List<GraphTemplate> SubgraphTemplates
        {
            get
            {
                var subgraphTemplates = new List<GraphTemplate>();
                var subGraphTypes = PublicGraphFactory.GetSubGraphTypes(Graph.GetType());

                foreach (var subGraphType in subGraphTypes)
                {
                    var template = new SubgraphTemplateImp(subGraphType,subGraphTypes.Count == 1 ? "Subgraph" : subGraphType.Name);
                    subgraphTemplates.Add(template);
                }

                return subgraphTemplates;
            }
        }

        void BuildNodesFromNodeModels()
        {
            if (m_Nodes == null)
            {
                m_Nodes = new List<INode>( NodeModels.Count);

                foreach (var nodeModel in NodeModels)
                {
                    AddNodeFromNodeModel(nodeModel);
                }
            }
        }

        void AddNodeFromNodeModel(AbstractNodeModel nodeModel)
        {
            if( nodeModel is IUserNodeModelImp imp)
                m_Nodes.Add(imp.Node);
            else if( nodeModel is IVariableNode || nodeModel is IConstantNode || nodeModel is ISubgraphNode)
            {
                m_Nodes.Add((INode)nodeModel);
            }
        }

        void RemoveNodeFromNodeModel(AbstractNodeModel nodeModel)
        {
            if (nodeModel is IUserNodeModelImp imp)
            {
                m_Nodes.Remove(imp.Node);
                imp.CallOnDisable();
            }
            else if( nodeModel is IVariableNode || nodeModel is IConstantNode || nodeModel is ISubgraphNode )
                m_Nodes.Remove((INode)nodeModel);
        }

        protected override void AddNode(AbstractNodeModel nodeModel)
        {
            BuildNodesFromNodeModels();
            base.AddNode(nodeModel);
            AddNodeFromNodeModel(nodeModel);
        }

        public IConstantNode CreateConstantNode(string name, Vector2 position, Type valueType, object defaultValue = null)
        {
            return ((ConstantNodeModelImp)CreateConstantNode(valueType.GenerateTypeHandle(), name, position, initializationCallback: n => n.Value.ObjectValue = defaultValue));
        }

        protected override void RemoveNode(AbstractNodeModel nodeModel)
        {
            BuildNodesFromNodeModels();
            RemoveNodeFromNodeModel(nodeModel);
            base.RemoveNode(nodeModel);
        }

        public override bool CanExpandPort(PortModel port)
        {
            return port.IsExpandable;
        }

        public IReadOnlyList<Type> SupportedTypes
        {
            get
            {
                if (m_SupportedTypes == null)
                {
                    InitializeSupportedTypes();
                }

                return m_SupportedTypes;
            }
        }

        public IReadOnlyList<Type> SupportedNodes => m_SupportedNodes ??= PublicGraphFactory.GetNodeTypes(m_Graph.GetType());

        internal static GraphElementModel CreateContextNodeFromData(IGraphNodeCreationData nodeCreationData, Type customNodeType)
        {
            return nodeCreationData.CreateNode(UserNodeHelper.GetNodeImpType(customNodeType),
                string.Empty,
                n => ((UserContextNodeModelImp)n).InitCustomNode((ContextNode)Activator.CreateInstance(customNodeType)));
        }

        internal static GraphElementModel CreateNodeFromData(IGraphNodeCreationData nodeCreationData, Type customNodeType)
        {
            return nodeCreationData.CreateNode(UserNodeHelper.GetNodeImpType(customNodeType),
                string.Empty,
                n => ((UserNodeModelImp)n).InitCustomNode((Node)Activator.CreateInstance(customNodeType)));
        }

        internal static GraphElementModel CreateContextFromBlockData(IGraphNodeCreationData nodeCreationData, Type blockType, Type contextType)
        {
            Action<AbstractNodeModel> initializationCallback = n => ((UserBlockNodeModelImp)n).InitCustomNode((BlockNode)Activator.CreateInstance(blockType));

            if (nodeCreationData is GraphBlockCreationData blockData)
                return blockData.ContextNodeModel.CreateAndInsertBlock(
                    typeof(UserBlockNodeModelImp), "", blockData.OrderInContext, nodeCreationData.Guid, initializationCallback, nodeCreationData.SpawnFlags);

            //This code path is only meant to display the block in the Item Library
            if (nodeCreationData.SpawnFlags != SpawnFlags.Orphan)
                return null;

            var context = nodeCreationData.GraphModel.CreateNode(typeof(UserContextNodeModelImp) , "Dummy Context", nodeCreationData.Position, nodeCreationData.Guid,
                n => ((UserContextNodeModelImp)n).InitCustomNode((ContextNode)Activator.CreateInstance(contextType)), nodeCreationData.SpawnFlags);
            (context as ContextNodeModel)?.CreateAndInsertBlock(typeof(UserBlockNodeModelImp), "", -1, nodeCreationData.Guid, initializationCallback, nodeCreationData.SpawnFlags);

            return context;
        }

        public class DummyContext : ContextNode
        {}

        void InitializeSupportedTypes()
        {
            using var _ = BlockAssetDirtyScope();
            m_SupportedTypes = new List<Type>();
            var supportedTypes = new HashSet<Type>();
            var nodeCreationData = new GraphNodeCreationData(this, Vector2.zero, SpawnFlags.Orphan);

            foreach (var type in SupportedNodes)
            {
                IUserNodeModelImp createdElement;

                if (typeof(ContextNode).IsAssignableFrom(type))
                {
                    InitializeSupportedTypesFromContextNodeType(m_Graph.GetType(), nodeCreationData, type, supportedTypes);
                    createdElement = (IUserNodeModelImp)(CreateContextNodeFromData(nodeCreationData, type) as ContextNodeModel);
                }
                else
                    createdElement = (IUserNodeModelImp)CreateNodeFromData(nodeCreationData, type);

                GetPortTypesForNode((INode)createdElement.Node.m_Implementation, supportedTypes);

                createdElement.CallOnDisable();
            }

            m_SupportedTypes.AddRange(supportedTypes);
            m_SupportedTypes.Sort((a, b) => Comparer<string>.Default.Compare(a.Name, b.Name));
        }

        public override void CloneGraph(GraphModel sourceGraphModel, bool keepVariableDeclarationGuids = false)
        {
            base.CloneGraph(sourceGraphModel, keepVariableDeclarationGuids);

            if (sourceGraphModel is GraphModelImp sourceGraphModelImp)
            {
                var sourceGraphType = sourceGraphModelImp.Graph.GetType();
                if (!sourceGraphType.IsInstanceOfType(Graph))
                    Debug.LogError("Graph was cloned with a different graph type than the original.");
            }
        }

        public override (Texture2D icon, Color color)? GetDataTypeStyle(Type dataType)
        {
            // Use the Graph's type (instead of the GraphModel's type) to get the correct style since the DataTypeStyleMapperAttribute is defined on Graph types in the public API.
            return BaseDataTypeStyleMapper.GetDataTypeStyle(dataType, Graph.GetType());
        }

        static void GetPortTypesForNode(INode node, HashSet<Type> hashSet)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            if (hashSet == null)
                throw new ArgumentNullException(nameof(hashSet));

            foreach (var input in node.GetInputPorts())
            {
                hashSet.Add(input.DataType ?? typeof(Untyped));
            }

            foreach (var output in node.GetOutputPorts())
            {
                hashSet.Add(output.DataType ?? typeof(Untyped));
            }
        }

        static void InitializeSupportedTypesFromContextNodeType(Type graphType, IGraphNodeCreationData nodeCreationData, Type type, HashSet<Type> supportedTypes)
        {
            foreach (var blockType in PublicGraphFactory.GetBlockTypes(graphType, type))
            {
                if (blockType.IsAbstract)
                    continue;

                var blockNode = (IUserNodeModelImp)((UserContextNodeModelImp)CreateContextFromBlockData(nodeCreationData, blockType, typeof(DummyContext))).Blocks[0].m_Implementation;
                if (blockNode != null)
                {
                    try
                    {
                        GetPortTypesForNode((INode)blockNode.Node.m_Implementation, supportedTypes);
                    }
                    finally
                    {
                        blockNode.CallOnDisable();
                    }
                }
            }
        }

        internal override GraphModel DuplicateLocalSubGraph(GraphModel sourceGraphModel, string name)
        {
            var subgraphType = (sourceGraphModel as GraphModelImp)?.Graph?.GetType();

            if (subgraphType == null)
                return null;

            // We use a SubgraphTemplate to pass the correct subgraph type. See SubgraphTemplateImp.LocalSubgraphPreOnEnableInit
            var subgraphTemplate = new SubgraphTemplateImp(subgraphType);
            var newSubgraph = CreateLocalSubgraph(
                sourceGraphModel.GetType(),
                name, subgraphTemplate);

            newSubgraph.CloneGraph(sourceGraphModel, true);

            return newSubgraph;
        }

        internal static class TestAccessImp
        {
            public static void GetPortTypesForNode(INode node, HashSet<Type> hashSet) => GraphModelImp.GetPortTypesForNode(node, hashSet);
            public static void InitializeSupportedTypesFromContextNodeType(Type graphType, IGraphNodeCreationData nodeCreationData, Type type, HashSet<Type> supportedTypes)
                => GraphModelImp.InitializeSupportedTypesFromContextNodeType(graphType, nodeCreationData, type, supportedTypes);
        }

        internal BaseGraphProcessingResult CallOnGraphChanged(GraphChangeDescription changes)
        {
            var result = new ErrorsAndWarningsImp(this);

            var graphChanges = new GraphLogger();
            graphChanges.errorsAndWarnings = result;
            LockForModification = true;
            try
            {
                Graph.OnGraphChanged(graphChanges);
            }
            finally
            {
                LockForModification = false;
            }
            return result;
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            m_Graph?.SetImplementation(this);
        }

        public override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();

            // Clear the nodes list so that it is rebuilt next time it is accessed
            m_Nodes = null;

            // Nodes that are re-created by undo/redo (eg: create, duplicate) lose all their non-serialized state (custom title, tooltip, subtitle, color).
            // To prevent this, we call OnEnable on undo/redo to restore their customization.
            LockForModification = true;
            try
            {
                foreach (var nodeModel in NodeAndBlockModels)
                {
                    // Skip nodes that weren't recreated by undo/redo. They haven't lost their non-serialized state, so we don't need to call OnEnable on them.
                    if (nodeModel is not IUserNodeModelImp userNodeModelImp || userNodeModelImp.OnEnableCalled)
                        continue;

                    userNodeModelImp.CallOnEnable();
                }
            }
            finally
            {
                LockForModification = false;
            }
        }
    }
}
