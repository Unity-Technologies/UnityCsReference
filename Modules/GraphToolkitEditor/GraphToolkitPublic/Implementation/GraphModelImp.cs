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
    partial class GraphModelImp : GraphModel
    {
        [NonSerialized]
        protected IReadOnlyList<Type> m_SupportedNodes;

        [NonSerialized]
        protected HashSet<Type> m_AutoSupportedTypes;
        [NonSerialized]
        HashSet<Type> m_SupportedTypes;
        [NonSerialized]
        HashSet<Type> m_AvailableVariableTypes;
        [NonSerialized]
        HashSet<Type> m_AvailableConstantTypes;

        [NonSerialized]
        ReadOnlyHashSet<Type> m_ReadOnlyAutoSupportedTypes;
        [NonSerialized]
        ReadOnlyHashSet<Type> m_ReadOnlySupportedTypes;
        [NonSerialized]
        ReadOnlyHashSet<Type> m_ReadOnlyAvailableVariableTypes;
        [NonSerialized]
        ReadOnlyHashSet<Type> m_ReadOnlyAvailableConstantTypes;

        static string CircularDependencyError(string accessedProperty, string buildMethod)
            => $"Do not access {accessedProperty} from within {buildMethod}: it creates a circular dependency. Use the baseSupportedTypes parameter as the base set instead.";

        [NonSerialized]
        bool m_IsBuildingSupportedTypes;
        [NonSerialized]
        bool m_IsBuildingAvailableVariableTypes;
        [NonSerialized]
        bool m_IsBuildingAvailableConstantTypes;

        [SerializeReference]
        IGraphInternal m_Graph;

        [NonSerialized]
        UndoStateComponent m_CurrentUndoStateComponent;

        [NonSerialized]
        readonly List<GraphElementModel> m_ScopePendingModels = new();

        // Coalescing state for consecutive params-mode UndoBeginRecordGraph scopes.
        // A "chain" is a run of scopes that share the same action name and model set with no
        // unrelated undo activity in between; the whole chain folds into one undo entry.
        [NonSerialized]
        int m_UndoChainStartGroup = -1;
        [NonSerialized]
        int m_UndoChainEndGroup = -1;
        [NonSerialized]
        string m_UndoChainActionName;
        [NonSerialized]
        readonly List<GraphElementModel> m_UndoChainModels = new();
        [NonSerialized]
        bool m_ScopeExtendsChain;

        [NonSerialized]
        List<INode> m_Nodes;

        // Maps a deleted port's guid to its owning node's guid (captured before the port is unregistered).
        // Only cleared by CollectChangeData, so every override must clear it or it grows for the model's lifetime.
        [NonSerialized]
        protected Dictionary<Hash128, Hash128> m_DeletedPortToNodeGuid = new Dictionary<Hash128, Hash128>();

        // Typed as the internal IGraphInternal contract so this can hold either a Graph or a StateMachine (which are
        // independent public types). Use `Graph as Graph` / `Graph as StateMachine` to get the concrete wrapper.
        public IGraphInternal Graph => m_Graph;

        public override bool AllowSubgraphCreation => Graph?.GetType().GetCustomAttribute<GraphAttribute>()?.Options.HasFlag(GraphOptions.SupportsSubgraphs) ?? false;

        public override bool AllowDeleteAndReconnect => true;

        // This method is meant to be called on new GraphObjectImps, before OnEnable is called, to override the default behaviour which is to create the graph based on the GraphObjectImp.GraphType, if it is null.
        internal void InstantiateGraph(Type graphType)
        {
            if (m_Graph != null)
            {
                Debug.LogError("InstantiateGraph called while Graph was already created.");
            }
            m_Graph = (IGraphInternal)Activator.CreateInstance(graphType);
        }

        public override void OnEnable()
        {
            var graphObject = GraphObject as GraphObjectImp;
            if (graphObject != null && m_Graph == null)
            {
                var graphType = graphObject.GraphType;
                if (graphType != null)
                {
                    m_Graph = (IGraphInternal)Activator.CreateInstance(graphType);
                }
            }

            foreach (var variable in VariableDeclarations)
            {
                if (variable != null && VariableDeclarationRequiresInitialization(variable) && variable.InitializationModel == null)
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
                    OnEnableGraphElementModels();
                }
                finally
                {
                    LockForModification = false;
                }
            }
        }

        protected virtual void OnEnableGraphElementModels()
        {
            foreach (var nodeModel in NodeModels)
            {
                if (nodeModel is IUserModelImp userModelImp)
                {
                    userModelImp.CallOnEnable();
                }
            }
        }

        public override void OnDisable()
        {
            LockForModification = true;
            try
            {
                OnDisableGraphElementModels();
                m_Graph?.OnDisable();
            }
            finally
            {
                LockForModification = false;
            }

            base.OnDisable();
        }

        protected virtual void OnDisableGraphElementModels()
        {
            foreach (var nodeModel in NodeModels)
            {
                if (nodeModel is IUserModelImp userModelImp)
                {
                    userModelImp.CallOnDisable();
                }
            }
        }

        public IReadOnlyList<IVariable> VariableModels => this.VariableDeclarations;
        public IReadOnlyList<IVariable> VariableModelsByDisplayOrder => GetVariableDeclarationsByDisplayOrder();

        protected override Type VariableNodeType => typeof(VariableNodeModelImp);
        protected override Type SubgraphNodeType => IsStateMachineGraph ? typeof(SubgraphStateModelImp) : typeof(SubgraphNodeModelImp);
        protected override Type ConstantNodeType => typeof(ConstantNodeModelImp);

        public override bool CanAssignTo(PortModel destination, PortModel source)
        {
            if (m_Graph is Graph graphInstance)
                return graphInstance.IsConnectionAllowed(source, destination);

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

        public void UndoBeginRecordGraph(string actionName)
        {
            UndoBeginRecordGraph(actionName, Array.Empty<Node>());
        }

        public void UndoBeginRecordGraph(string actionName, params Node[] nodesToRecord)
        {
            CheckModificationLock();

            // Filter to nodes that actually belong to this graph. Callers may pass nodes from other
            // graphs (or loose nodes), and tracking those here would attach the wrong undo/dirty
            // signals to this graph.
            m_ScopePendingModels.Clear();
            if (nodesToRecord != null)
            {
                for (var i = 0; i < nodesToRecord.Length; i++)
                {
                    var node = nodesToRecord[i];
                    if (node != null && node.Graph == m_Graph)
                        m_ScopePendingModels.Add(node.GetImplementation());
                }
            }

            BeginRecordScope(actionName);
        }

        public void UndoBeginRecordGraph(string actionName, Condition[] conditionsToRecord)
        {
            CheckModificationLock();

            // Same ownership filter as the node overload. m_Implementation is read directly instead of
            // GetImplementation() so a loose condition doesn't get an implementation created just to be rejected.
            m_ScopePendingModels.Clear();
            if (conditionsToRecord != null)
            {
                for (var i = 0; i < conditionsToRecord.Length; i++)
                {
                    var condition = conditionsToRecord[i];
                    if (condition?.m_Implementation != null && condition.m_Implementation.GraphModel == this)
                        m_ScopePendingModels.Add(condition.m_Implementation);
                }
            }

            BeginRecordScope(actionName);
        }

        void BeginRecordScope(string actionName)
        {
            var pendingCount = m_ScopePendingModels.Count;

            // Decide whether this scope continues a coalescing chain established by a prior
            // params-mode scope. The chain is broken if anything else advanced the current
            // undo group between the previous End and this Begin.
            m_ScopeExtendsChain =
                pendingCount > 0
                && m_UndoChainStartGroup >= 0
                && m_UndoChainActionName == actionName
                && Undo.GetCurrentGroup() == m_UndoChainEndGroup
                && SameNodeSet(m_UndoChainModels, m_ScopePendingModels);

            if (pendingCount > 0 && !m_ScopeExtendsChain)
            {
                m_UndoChainStartGroup = Undo.GetCurrentGroup();
                m_UndoChainActionName = actionName;
                m_UndoChainModels.Clear();
                m_UndoChainModels.AddRange(m_ScopePendingModels);
            }

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

                    // AddChangedModel also calls SetGraphObjectDirty and feeds the observer that dispatches
                    // OnGraphChanged; without this the user's direct field mutation is invisible to both.
                    var pendingCount = m_ScopePendingModels.Count;
                    if (pendingCount > 0)
                    {
                        var desc = CurrentGraphChangeDescription;
                        foreach (var model in m_ScopePendingModels)
                        {
                            desc.AddChangedModel(model, ChangeHint.Data);
                        }
                    }

                    var currentGraphModelStateUpdater = window.GraphView.GraphViewModel.GraphModelState.UpdateScope;
                    currentGraphModelStateUpdater.MarkUpdated(CurrentGraphChangeDescription);
                    currentGraphModelStateUpdater.Dispose();
                    PopGraphChangeDescription();
                    m_CurrentUndoStateComponent.EndOperation();

                    // Fold this scope into the chain's single undo entry. The framework's
                    // EndOperation just collapsed this scope; we now collapse the whole chain
                    // back to the group index captured before its first Begin.
                    if (m_ScopeExtendsChain && m_UndoChainStartGroup >= 0)
                    {
                        Undo.CollapseUndoOperations(m_UndoChainStartGroup);
                    }

                    if (pendingCount > 0)
                    {
                        m_UndoChainEndGroup = Undo.GetCurrentGroup();
                    }
                }
                finally
                {
                    m_CurrentUndoStateComponent = null;
                    m_ScopeExtendsChain = false;
                    m_ScopePendingModels.Clear();
                }
            }
        }

        static bool SameNodeSet(List<GraphElementModel> a, List<GraphElementModel> b)
        {
            if (a == null || b == null || a.Count != b.Count)
                return false;
            for (var i = 0; i < a.Count; i++)
            {
                var aNode = a[i];
                var bNode = b[i];
                if (!ReferenceEquals(bNode, aNode))
                {
                    return false;
                }
            }
            return true;
        }

        protected override void CreateGraphProcessors()
        {
            base.CreateGraphProcessors();

            var isStateMachine = Graph is StateMachine;
            var changedMethodName = isStateMachine
                ? nameof(GraphToolkit.Editor.StateMachine.OnStateMachineChanged)
                : nameof(GraphToolkit.Editor.Graph.OnGraphChanged);
            var loggerType = isStateMachine ? typeof(StateMachineLogger) : typeof(GraphLogger);
            var declaringType = Graph?.GetType().GetMethod(changedMethodName, new[] { loggerType })?.DeclaringType;
            var overridden = declaringType != null && declaringType != typeof(Graph) && declaringType != typeof(StateMachine);

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

            if (result?.DataType.Resolve() is { } variableType)
                AddAutoSupportedType(variableType);

            return result;
        }

        public bool RemoveVariable(IVariable variable, bool forceRemove)
        {
            CheckModificationLock();

            if (variable == null)
                throw new ArgumentNullException(nameof(variable));

            if (variable is not VariableDeclarationModelBase variableModel)
                return false;

            if (variable.Graph != Graph && variable.StateMachine != Graph)
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

            if (!IsTypeCompatibleWithGraph(node.GetType()))
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
            if (nodeModel != null)
                AddAutoSupportedType(valueType);

            return (IConstantNode)nodeModel;
        }

        public IVariableNode AddVariableNode(IVariable variable, Vector2 position)
        {
            return AddVariableNode(variable, position, VariableNodeMode.Get);
        }

        public IVariableNode AddVariableNode(IVariable variable, Vector2 position, VariableNodeMode mode)
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

            if (mode == VariableNodeMode.Set && !declModel.CanCreateSetVariableNode)
                throw new ArgumentException($"Variable '{declModel.Title}' cannot be used as a set variable node.", nameof(mode));

            return (IVariableNode)base.CreateVariableNode(declModel, position, mode: mode);
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
            name ??= SubgraphCreationHelper.DefaultLocalSubgraphName;
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
                    return IsTypeCompatibleWithGraph(customNodeModel.Node.GetType());

                case VariableNodeModel variableNodeModel:
                    return variableNodeModel.VariableDeclarationModel.GetType() == typeof(VariableDeclarationModel) && SupportedTypesSet.Contains(variableNodeModel.VariableDeclarationModel.DataType.Resolve());

                case ConstantNodeModel constantNodeModel:
                    return SupportedTypesSet.Contains(constantNodeModel.Type);

                case WirePortalModel portalNodeModel:
                    return SupportedTypesSet.Contains(portalNodeModel.GetPortDataTypeHandle().Resolve());

                case SubgraphNodeModel subgraphNodeModel:
                    if (!AllowSubgraphCreation)
                    {
                        Debug.LogError($"Graph {Name} does not support subgraph creation. Subgraph nodes cannot be added to the graph.");
                        return false;
                    }
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

        protected bool IsTypeCompatibleWithGraph(Type elementType)
        {
            var graphType = m_Graph.GetType();

            // If the compatibility attribute is present, we do not fall into auto inclusion
            if (TryGetGraphElementCompatibility(elementType, graphType, out var isCompatible))
                return isCompatible;

            // Default behaviour : Check Assembly Auto-inclusion rules
            var graphAttr = graphType.GetCustomAttribute<GraphAttribute>();
            bool autoInclude = graphAttr == null || !graphAttr.Options.HasFlag(GraphOptions.DisableAutoInclusionOfNodesFromGraphAssembly);
            return autoInclude && elementType.Assembly == graphType.Assembly;
        }

        /// <summary>
        /// Determines whether <paramref name="elementType"/> explicitly declares its compatibility with
        /// <paramref name="graphType"/> through a compatibility attribute (<see cref="UseWithGraphAttribute"/> for
        /// regular graphs). When it does, <paramref name="isCompatible"/> reports the result and auto-inclusion is
        /// bypassed. State machines override this to read <see cref="UseWithStateMachineAttribute"/> instead.
        /// </summary>
        protected virtual bool TryGetGraphElementCompatibility(Type elementType, Type graphType, out bool isCompatible)
        {
            var attr = elementType.GetCustomAttribute<UseWithGraphAttribute>(true);
            if (attr != null)
            {
                isCompatible = attr.IsGraphTypeSupported(graphType);
                return true;
            }

            isCompatible = false;
            return false;
        }

        public override bool CanPasteVariable(VariableDeclarationModelBase originalModel)
        {
            return originalModel is VariableDeclarationModel &&
                   SupportedTypesSet.Contains(originalModel.DataType.Resolve());
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

        protected virtual void TrackAddedNode(AbstractNodeModel nodeModel)
        {
            if (m_Nodes == null)
                BuildNodesFromNodeModels();
            else
                AddNodeFromNodeModel(nodeModel);
        }

        protected virtual void TrackRemovedNode(AbstractNodeModel nodeModel)
        {
            BuildNodesFromNodeModels();
            RemoveNodeFromNodeModel(nodeModel);
        }

        protected override void AddNode(AbstractNodeModel nodeModel)
        {
            base.AddNode(nodeModel);
            TrackAddedNode(nodeModel);
        }

        public IConstantNode CreateConstantNode(string name, Vector2 position, Type valueType, object defaultValue = null)
        {
            return ((ConstantNodeModelImp)CreateConstantNode(valueType.GenerateTypeHandle(), name, position, initializationCallback: n => n.Value.ObjectValue = defaultValue));
        }

        protected override void RemoveNode(AbstractNodeModel nodeModel)
        {
            TrackRemovedNode(nodeModel);
            base.RemoveNode(nodeModel);
        }

        protected override void UnregisterElement(GraphElementModel model)
        {
            if (model != null)
            {
                if (model is PortModel portModel)
                {
                    // Capture the parent node guid now; once base.UnregisterElement runs we may lose the link.
                    var ownerGuid = portModel.NodeModel?.Guid ?? default;
                    m_DeletedPortToNodeGuid[portModel.Guid] = ownerGuid;
                }
            }
            base.UnregisterElement(model);
        }

        public override bool CanExpandPort(PortModel port)
        {
            return port.IsExpandable;
        }

        public virtual IReadOnlyList<Type> SupportedNodes => m_SupportedNodes ??= PublicGraphFactory.GetNodeTypes(m_Graph.GetType());

        IReadOnlyCollection<Type> AutoSupportedTypes
        {
            get
            {
                if (m_AutoSupportedTypes == null)
                    InitializeAutoSupportedTypes();

                return m_ReadOnlyAutoSupportedTypes ??= new ReadOnlyHashSet<Type>(m_AutoSupportedTypes);
            }
        }

        HashSet<Type> AutoSupportedTypesSet
        {
            get
            {
                if (m_AutoSupportedTypes == null)
                    InitializeAutoSupportedTypes();

                return m_AutoSupportedTypes;
            }
        }

        void AddAutoSupportedType(Type type)
        {
            if (!AutoSupportedTypesSet.Add(type))
                return;

            // Invalidate caches that depend on the auto set.
            m_AvailableVariableTypes = null;
            m_ReadOnlyAvailableVariableTypes = null;
            m_AvailableConstantTypes = null;
            m_ReadOnlyAvailableConstantTypes = null;
            m_SupportedTypes = null;
            m_ReadOnlySupportedTypes = null;
        }

        public IReadOnlyCollection<Type> SupportedTypes
        {
            get
            {
                if (m_SupportedTypes != null)
                    return m_ReadOnlySupportedTypes;

                if (m_IsBuildingSupportedTypes)
                    throw new InvalidOperationException(CircularDependencyError(
                        nameof(SupportedTypes),
                        "BuildAvailableVariableTypes or BuildAvailableConstantTypes"));

                m_IsBuildingSupportedTypes = true;
                try
                {
                    var result = new HashSet<Type>(AutoSupportedTypesSet);
                    result.UnionWith(AvailableVariableTypes);
                    result.UnionWith(AvailableConstantTypes);
                    m_SupportedTypes = result;
                    m_ReadOnlySupportedTypes = new ReadOnlyHashSet<Type>(m_SupportedTypes);
                }
                finally
                {
                    m_IsBuildingSupportedTypes = false;
                }
                return m_ReadOnlySupportedTypes;
            }
        }

        HashSet<Type> SupportedTypesSet
        {
            get
            {
                _ = SupportedTypes; // ensure m_SupportedTypes is populated (or throws if circular)
                return m_SupportedTypes;
            }
        }

        public IReadOnlyCollection<Type> AvailableVariableTypes
        {
            get
            {
                if (m_AvailableVariableTypes != null)
                    return m_ReadOnlyAvailableVariableTypes;

                if (m_IsBuildingAvailableVariableTypes)
                    throw new InvalidOperationException(CircularDependencyError(
                        $"{nameof(SupportedTypes)} or {nameof(AvailableVariableTypes)}",
                        "BuildAvailableVariableTypes"));

                m_IsBuildingAvailableVariableTypes = true;
                try
                {
                    var built = m_Graph.InvokeBuildAvailableVariableTypes(AutoSupportedTypes);
                    m_AvailableVariableTypes = built != null ? new HashSet<Type>(built) : [];
                    m_ReadOnlyAvailableVariableTypes = new ReadOnlyHashSet<Type>(m_AvailableVariableTypes);
                }
                finally
                {
                    m_IsBuildingAvailableVariableTypes = false;
                }
                return m_ReadOnlyAvailableVariableTypes;
            }
        }

        public IReadOnlyCollection<Type> AvailableConstantTypes
        {
            get
            {
                if (m_AvailableConstantTypes != null)
                    return m_ReadOnlyAvailableConstantTypes;

                if (m_IsBuildingAvailableConstantTypes)
                    throw new InvalidOperationException(CircularDependencyError(
                        $"{nameof(SupportedTypes)} or {nameof(AvailableConstantTypes)}",
                        "BuildAvailableConstantTypes"));

                m_IsBuildingAvailableConstantTypes = true;
                try
                {
                    var built = m_Graph.InvokeBuildAvailableConstantTypes(AutoSupportedTypes);
                    m_AvailableConstantTypes = built != null ? new HashSet<Type>(built) : [];
                    m_ReadOnlyAvailableConstantTypes = new ReadOnlyHashSet<Type>(m_AvailableConstantTypes);
                }
                finally
                {
                    m_IsBuildingAvailableConstantTypes = false;
                }
                return m_ReadOnlyAvailableConstantTypes;
            }
        }

        /// <inheritdoc />
        protected override WireModel InstantiateWire(Type wireType, PortModel toPort, PortModel fromPort, Hash128 guid = default)
        {
            // A custom single-state transition is authored as a public SelfTransition. Map it to its backing model
            // (mirrors how a custom State maps to UserStateModelImp) so the public type flows through the regular wire
            // creation chain.
            if (typeof(SelfTransition).IsAssignableFrom(wireType))
            {
                var transitionImp = new UserSelfTransitionModelImp();
                transitionImp.InitCustomTransition((SelfTransition)Activator.CreateInstance(wireType));
                transitionImp.GraphModel = this;
                if (guid.isValid)
                    transitionImp.SetGuid(guid);
                transitionImp.SetPorts(toPort, fromPort);
                return transitionImp;
            }

            return base.InstantiateWire(wireType, toPort, fromPort, guid);
        }

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

        protected virtual void InitializeAutoSupportedTypes()
        {
            m_AutoSupportedTypes = [];

            using var _ = BlockAssetDirtyScope();
            var nodeCreationData = new GraphNodeCreationData(this, Vector2.zero, SpawnFlags.Orphan);

            foreach (var type in SupportedNodes)
            {
                IUserNodeModelImp createdElement;

                if (typeof(ContextNode).IsAssignableFrom(type))
                {
                    InitializeSupportedTypesFromContextNodeType(m_Graph.GetType(), nodeCreationData, type, m_AutoSupportedTypes);
                    createdElement = (IUserNodeModelImp)(CreateContextNodeFromData(nodeCreationData, type) as ContextNodeModel);
                }
                else
                    createdElement = (IUserNodeModelImp)CreateNodeFromData(nodeCreationData, type);

                GetPortTypesForNode((INode)createdElement.Node.m_Implementation, m_AutoSupportedTypes);

                createdElement.CallOnDisable();
            }
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

            if (newSubgraph == null)
                return null;

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

            ILogger logger = Graph is StateMachine
                ? new StateMachineLogger { errorsAndWarnings = result }
                : new GraphLogger { errorsAndWarnings = result };

            CollectChangeData(changes, logger);

            LockForModification = true;
            try
            {
                Graph.OnGraphChanged(logger);
                for (var i = 0; i < NodeModels.Count; i++)
                {
                    if (NodeModels[i] is NodeModel nodeModel)
                    {
                        nodeModel.CheckNodeErrors(result);
                    }
                }
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
                OnUndoRedoGraphElementModels();
            }
            finally
            {
                LockForModification = false;
            }
        }

        protected virtual void OnUndoRedoGraphElementModels()
        {
            foreach (var nodeModel in NodeAndBlockModels)
            {
                // Skip nodes that weren't recreated by undo/redo. They haven't lost their non-serialized state, so we don't need to call OnEnable on them.
                if (nodeModel is IUserModelImp userModelImp && !userModelImp.OnEnableCalled)
                {
                    userModelImp.CallOnEnable();
                }
            }
        }
    }
}
