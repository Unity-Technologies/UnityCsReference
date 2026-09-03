// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.Implementation
{
    [Serializable]
    partial class StateMachineImp : GraphModelImp
    {
        [NonSerialized]
        IReadOnlyList<Type> m_SupportedSelfTransitions;

        [NonSerialized]
        List<Type> m_SelfTransitionSupportTypes;

        // States are IStates, not INodes, so they are kept in their own list rather than in the GraphModelImp.Nodes
        // list. Like Nodes, this is rebuilt lazily from the node models and maintained as states are added and removed.
        [NonSerialized]
        List<IState> m_States;

        [NonSerialized]
        List<(string, ConditionModelFactory)> m_AddConditionOptions;

        public override bool IsStateMachineGraph => true;
        public override bool AllowSubgraphCreation => Graph?.GetType().GetCustomAttribute<StateMachineAttribute>()?.Options.HasFlag(StateMachineOptions.SupportsSubgraphs) ?? false;

        public override bool CanPasteNode(AbstractNodeModel originalModel)
        {
            if (originalModel is SubgraphStateModel subgraphStateModel)
            {
                if (!AllowSubgraphCreation)
                {
                    Debug.LogError($"State machine {Name} does not support subgraph creation. Subgraph states cannot be added to the state machine.");
                    return false;
                }

                var subgraph = (subgraphStateModel.GetSubgraphModel() as GraphModelImp)?.Graph ??
                               (GraphReference.ResolveGraphModel(subgraphStateModel.SubgraphReference) as GraphModelImp)?.Graph;

                if (subgraph == null)
                {
                    Debug.LogWarning("Cannot paste subgraph state because the referenced subgraph could not be resolved.");
                    return false;
                }

                var subgraphTypes = PublicGraphFactory.GetSubGraphTypes(Graph.GetType());

                foreach (var subgraphType in subgraphTypes)
                {
                    if (subgraphType.IsInstanceOfType(subgraph))
                        return true;
                }
            }

            return originalModel is StateModel;
        }

        public override bool CanPasteVariable(VariableDeclarationModelBase originalModel)
        {
            return false;
        }

        public override bool CanCreateVariableNode(VariableDeclarationModelBase variable, GraphModel graphModel)
        {
            return false;
        }

        /// <inheritdoc />
        protected override bool TryGetGraphElementCompatibility(Type elementType, Type graphType, out bool isCompatible)
        {
            var attr = elementType.GetCustomAttribute<UseWithStateMachineAttribute>(true);
            if (attr != null)
            {
                isCompatible = attr.IsStateMachineTypeSupported(graphType);
                return true;
            }

            isCompatible = false;
            return false;
        }

        public override IReadOnlyList<Type> SupportedNodes => m_SupportedNodes ??= PublicGraphFactory.GetStateTypes(Graph.GetType());
        public IReadOnlyList<Type> SupportedSelfTransitions => m_SupportedSelfTransitions ??= PublicGraphFactory.GetSelfTransitionTypes(Graph.GetType());

        public override IReadOnlyList<(string, ConditionModelFactory)> GetAddConditionOptions()
        {
            if (m_AddConditionOptions != null)
                return m_AddConditionOptions;

            var options = new List<(string, ConditionModelFactory)>(base.GetAddConditionOptions());

            foreach (var conditionType in PublicGraphFactory.GetConditionTypes(Graph.GetType()))
            {
                options.Add((Condition.GetTypeDisplayName(conditionType), _ =>
                {
                    var conditionModel = new UserConditionModelImp();
                    var condition = (Condition)Activator.CreateInstance(conditionType);
                    conditionModel.InitCustomCondition(condition);
                    NormalizeComparison(conditionModel, condition);
                    return conditionModel;
                }));
            }

            m_AddConditionOptions = options;
            return m_AddConditionOptions;
        }

        // The stored default, Equal, may not be in the condition's SupportedComparisons override.
        static void NormalizeComparison(UserConditionModelImp conditionModel, Condition condition)
        {
            if (!condition.DisplayComparisonDropdownInternal)
                return;

            var supportedComparisons = condition.SupportedComparisonsInternal;
            if (supportedComparisons is not { Count: > 0 })
                return;

            foreach (var comparison in supportedComparisons)
            {
                if (comparison == conditionModel.Comparison)
                    return;
            }

            conditionModel.Comparison = supportedComparisons[0];
        }

        protected override void InitializeAutoSupportedTypes()
        {
            m_AutoSupportedTypes = [];
        }

        /// <summary>
        /// All the states in the state machine, in the order they were created.
        /// </summary>
        public IReadOnlyList<IState> States
        {
            get
            {
                BuildStatesFromNodeModels();
                return m_States;
            }
        }

        internal void AddState(State state)
        {
            CheckModificationLock();

            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (!IsTypeCompatibleWithGraph(state.GetType()))
            {
                throw new ArgumentException($"State '{state.GetType().Name}' is not compatible with this state machine type ({Graph.GetType().Name}). Ensure it is decorated with [UseWithStateMachine] or is in the same assembly.");
            }

            var stateImp = state.GetImplementation();

            // If already here, do nothing.
            if (stateImp.GraphModel == this)
                return;

            // Reparenting: remove from the previous state machine first.
            if (stateImp.GraphModel is StateMachineImp previousStateMachine)
                previousStateMachine.RemoveState(state);

            // Perform state initialization (mirrors AddNode for regular nodes).
            stateImp.GraphModel = this;
            stateImp.OnCreateNode();
            AddNode(stateImp);
        }

        internal void RemoveState(IState state)
        {
            CheckModificationLock();

            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (!(state is State || state is StateModel))
                throw new ArgumentException($"The provided IState ('{state.GetType().Name}') is not a valid internal state implementation.", nameof(state));

            var stateImp = state.StateModel;
            if (stateImp.GraphModel != this)
                throw new ArgumentException("The state provided does not belong to this state machine.", nameof(state));

            DeleteNode(stateImp, deleteConnections: true);
        }

        internal IEnumerable<ITransition> GetTransitions(IState fromState, IState toState)
        {
            ValidateStatePair(fromState, toState, out var fromModel, out var toModel);
            return GetTransitionsBetween(fromModel, toModel);
        }

        internal ITransition Connect(IState fromState, IState toState)
        {
            CheckModificationLock();
            ValidateStatePair(fromState, toState, out var fromModel, out var toModel);

            TransitionSupportModel support;
            if (ReferenceEquals(fromModel, toModel))
                support = CreateOrExtendSelfTransitionSupport(fromModel, typeof(SelfTransitionModel));
            else
            {
                var (fromSide, toSide) = ComputeFacingAnchorSides(fromModel.Position, toModel.Position);
                support = CreateTransitionSupport(
                    toModel.GetInPort(), toSide, ComputeAnchorOffset(toModel, toSide),
                    fromModel.GetOutPort(), fromSide, ComputeAnchorOffset(fromModel, fromSide),
                    typeof(StateToStateTransitionModel));
            }

            return support?.AsPublicTransition();
        }

        // Picks the pair of facing sides the transition would use if it had been drawn in the graph view.
        static (AnchorSide fromSide, AnchorSide toSide) ComputeFacingAnchorSides(Vector2 fromPosition, Vector2 toPosition)
        {
            var delta = toPosition - fromPosition;

            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                return delta.x >= 0 ? (AnchorSide.Right, AnchorSide.Left) : (AnchorSide.Left, AnchorSide.Right);

            return delta.y >= 0 ? (AnchorSide.Bottom, AnchorSide.Top) : (AnchorSide.Top, AnchorSide.Bottom);
        }

        // Stacks the new anchor past the transitions already anchored on that side, so parallel transitions do not overlap.
        static float ComputeAnchorOffset(StateModel state, AnchorSide side)
        {
            const float transitionWidth = 30.0f;

            var maxOffset = 0f;
            foreach (var wire in state.GetConnectedWires())
            {
                if (wire is not TransitionSupportModel transition)
                    continue;

                if (ReferenceEquals(transition.FromPort?.NodeModel, state) &&
                    transition.FromNodeAnchorSide == side && transition.FromNodeAnchorOffset > maxOffset)
                    maxOffset = transition.FromNodeAnchorOffset;

                if (ReferenceEquals(transition.ToPort?.NodeModel, state) &&
                    transition.ToNodeAnchorSide == side && transition.ToNodeAnchorOffset > maxOffset)
                    maxOffset = transition.ToNodeAnchorOffset;
            }

            return maxOffset + transitionWidth;
        }

        internal bool Disconnect(IState fromState, IState toState)
        {
            CheckModificationLock();
            ValidateStatePair(fromState, toState, out var fromModel, out var toModel);

            List<WireModel> supportsToDelete = null;
            foreach (var support in GetTransitionSupportsBetween(fromModel, toModel))
            {
                supportsToDelete ??= new List<WireModel>();
                supportsToDelete.Add(support);
            }

            if (supportsToDelete == null)
                return false;

            DeleteWires(supportsToDelete);
            return true;
        }

        void ValidateStatePair(IState fromState, IState toState, out StateModel fromModel, out StateModel toModel)
        {
            if (fromState == null)
                throw new ArgumentNullException(nameof(fromState));
            if (toState == null)
                throw new ArgumentNullException(nameof(toState));

            if (!(fromState is State || fromState is StateModel))
                throw new ArgumentException($"The provided IState ('{fromState.GetType().Name}') is not a valid internal state implementation.", nameof(fromState));
            if (!(toState is State || toState is StateModel))
                throw new ArgumentException($"The provided IState ('{toState.GetType().Name}') is not a valid internal state implementation.", nameof(toState));

            fromModel = fromState.StateModel;
            toModel = toState.StateModel;

            if (fromModel.GraphModel != this || toModel.GraphModel != this)
                throw new ArgumentException("Both states must belong to this state machine.");
        }

        /// <summary>
        /// All the transitions connected to a state port, in the order they are connected.
        /// </summary>
        internal static IEnumerable<ITransition> GetTransitionsOnPort(PortModel port)
        {
            foreach (var wire in port.GetConnectedWires())
            {
                if (wire is TransitionSupportModel support && support is not IGhostWireModel)
                    yield return support.AsPublicTransition();
            }
        }

        static IEnumerable<ITransition> GetTransitionsBetween(StateModel fromModel, StateModel toModel)
        {
            foreach (var support in GetTransitionSupportsBetween(fromModel, toModel))
                yield return support.AsPublicTransition();
        }

        static IEnumerable<TransitionSupportModel> GetTransitionSupportsBetween(StateModel fromModel, StateModel toModel)
        {
            foreach (var wire in fromModel.GetOutPort().GetConnectedWires())
            {
                if (wire is TransitionSupportModel support && support is not IGhostWireModel
                    && ReferenceEquals(support.ToPort?.NodeModel, toModel))
                {
                    yield return support;
                }
            }
        }

        void BuildStatesFromNodeModels()
        {
            if (m_States == null)
            {
                m_States = new List<IState>(NodeModels.Count);

                foreach (var nodeModel in NodeModels)
                {
                    AddStateFromNodeModel(nodeModel);
                }
            }
        }

        // A user-authored state is tracked through its State object, every internal state
        // through its model. The UserStateModelImp branch must come first: that model is itself an
        // IState, and the list must hold the user's State object rather than the model backing it.
        void AddStateFromNodeModel(AbstractNodeModel nodeModel)
        {
            if (nodeModel is UserStateModelImp stateImp && stateImp.Node != null)
                m_States.Add(stateImp.Node);
            else if (nodeModel is ISubgraphState subgraphState)
                m_States.Add(subgraphState);
        }

        void RemoveStateFromNodeModel(AbstractNodeModel nodeModel)
        {
            if (nodeModel is UserStateModelImp stateImp && stateImp.Node != null)
            {
                m_States.Remove(stateImp.Node);
                stateImp.CallOnDisable();
            }
            else if (nodeModel is ISubgraphState subgraphState)
            {
                m_States.Remove(subgraphState);
            }
        }

        protected override void TrackAddedNode(AbstractNodeModel nodeModel)
        {
            if (m_States == null)
                BuildStatesFromNodeModels();
            else
                AddStateFromNodeModel(nodeModel);
        }

        protected override void TrackRemovedNode(AbstractNodeModel nodeModel)
        {
            BuildStatesFromNodeModels();
            RemoveStateFromNodeModel(nodeModel);
        }

        public override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();

            // Clear the states list so that it is rebuilt next time it is accessed.
            m_States = null;
        }

        /// <inheritdoc />
        public override IReadOnlyList<Type> GetSelfTransitionModelTypes()
        {
            if (m_SelfTransitionSupportTypes == null)
            {
                m_SelfTransitionSupportTypes = new List<Type>(base.GetSelfTransitionModelTypes());
                m_SelfTransitionSupportTypes.AddRange(SupportedSelfTransitions);
            }

            return m_SelfTransitionSupportTypes;
        }

        internal static GraphElementModel CreateStateFromData(IGraphNodeCreationData nodeCreationData, Type customStateType)
        {
            return nodeCreationData.CreateNode(typeof(UserStateModelImp),
                string.Empty,
                n => ((UserStateModelImp)n).InitCustomState((State)Activator.CreateInstance(customStateType)));
        }

        /// <inheritdoc />
        protected override void AddWire(WireModel wireModel)
        {
            base.AddWire(wireModel);

            if (wireModel is UserSelfTransitionModelImp transitionImp)
                transitionImp.CallOnEnable();
        }

        /// <inheritdoc />
        protected override void RemoveWire(WireModel wireModel)
        {
            if (wireModel is UserSelfTransitionModelImp transitionImp)
                transitionImp.CallOnDisable();

            base.RemoveWire(wireModel);
        }

        protected override void OnEnableGraphElementModels()
        {
            base.OnEnableGraphElementModels();

            foreach (var wireModel in WireModels)
            {
                if (wireModel is UserSelfTransitionModelImp transitionImp)
                {
                    transitionImp.CallOnEnable();
                }
            }
        }

        protected override void OnDisableGraphElementModels()
        {
            base.OnDisableGraphElementModels();

            foreach (var wireModel in WireModels)
            {
                if (wireModel is UserSelfTransitionModelImp transitionImp)
                {
                    transitionImp.CallOnDisable();
                }
            }
        }

        protected override void OnUndoRedoGraphElementModels()
        {
            base.OnUndoRedoGraphElementModels();

            foreach (var wireModel in WireModels)
            {
                if (wireModel is UserSelfTransitionModelImp transitionImp && !transitionImp.OnEnableCalled)
                {
                    transitionImp.CallOnEnable();
                }
            }
        }
    }
}
