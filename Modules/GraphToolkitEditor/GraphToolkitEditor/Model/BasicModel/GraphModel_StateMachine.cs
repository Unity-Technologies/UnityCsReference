// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    abstract partial class GraphModel
    {
        [NoAutoStaticsCleanup] // fixed array of condition type factories; lambdas capture no user state, safe to persist
        static readonly (string, ConditionModelFactory)[] k_DefaultConditionTypes =
        {
            ("Group Condition", _ => new GroupConditionModel()),
            ("Variable Condition", _ => new VariableConditionModel()),
        };

        [NoAutoStaticsCleanup] // fixed array of self transition types; safe to persist across reload
        static readonly Type[] k_BuiltInSelfTransitionTypes =
        {
            typeof(SelfTransitionModel)
        };

        /// <summary>
        /// Returns the single-state transition support types that can be created on a state in this graph.
        /// </summary>
        /// <returns>The single-state transition support types that can be created on a state in this graph.</returns>
        /// <remarks>
        /// The returned types are passed to <see cref="CreateSelfTransitionSupport"/>.
        /// By default this is only the <see cref="SelfTransitionModel"/>).
        /// Implementations override this to surface additional, user-defined transition types.
        /// </remarks>
        public virtual IReadOnlyList<Type> GetSelfTransitionModelTypes()
        {
            return k_BuiltInSelfTransitionTypes;
        }

        /// <summary>
        /// A delegate to create a new condition model.
        /// </summary>
        /// <param name="parent">The <see cref="GroupConditionModel"/> in which this condition will be created.</param>
        public delegate ConditionModel ConditionModelFactory(GroupConditionModel parent);

        /// <summary>
        /// Returns a list of condition types that can be added to the graph as well as the add menu label for each.
        /// </summary>
        /// <returns>A list of condition types that can be added to the graph as well as the add menu label for each.</returns>
        public virtual IReadOnlyList<(string, ConditionModelFactory)> GetAddConditionOptions()
        {
            return k_DefaultConditionTypes;
        }

        /// <summary>
        /// Creates a transition support wire between two ports and adds it to the graph.
        /// </summary>
        /// <param name="toPort">The port to which the transition goes.</param>
        /// <param name="toStateAnchorSide">The side of the state to which the transition goes.</param>
        /// <param name="toStateAnchorOffset">The offset of the state to which the transition goes.</param>
        /// <param name="fromPort">The port from which the transition originates.</param>
        /// <param name="fromStateAnchorSide">The side of the state from which the transition originates.</param>
        /// <param name="fromStateAnchorOffset">The offset of the state from which the transition originates.</param>
        /// <param name="transitionSupportType">The type of transition to create. Must derive from <see cref="TransitionSupportModel"/>.</param>
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <returns>The newly created wire</returns>
        public virtual TransitionSupportModel CreateTransitionSupport(
            PortModel toPort, AnchorSide toStateAnchorSide, float toStateAnchorOffset,
            PortModel fromPort, AnchorSide fromStateAnchorSide, float fromStateAnchorOffset,
            Type transitionSupportType, Hash128 guid = default)
        {
            if (transitionSupportType == null)
                return null;

            var transitionSupport = CreateWire(transitionSupportType, toPort, fromPort, false, guid) as TransitionSupportModel;
            if (transitionSupport != null)
            {
                transitionSupport.SetFromAnchor(fromStateAnchorSide, fromStateAnchorOffset);
                transitionSupport.SetToAnchor(toStateAnchorSide, toStateAnchorOffset);

                var transition = transitionSupport.CreateTransition();
                transitionSupport.AddTransition(transition);
            }
            return transitionSupport;
        }

        /// <summary>
        /// Adds a self transition rule on a state, reusing the state's self transition support of the same type if
        /// there is one.
        /// </summary>
        /// <param name="stateModel">The state on which to add the self transition.</param>
        /// <param name="transitionSupportType">The type of transition to create. Must derive from <see cref="SelfTransitionModel"/>.</param>
        /// <returns>The transition support the rule was added to, either an existing one or a newly created one.</returns>
        /// <remarks>
        /// A state holds at most one self transition support per transition type: when the state already has a self
        /// transition of <paramref name="transitionSupportType"/>, a new rule is added to it instead of creating a
        /// second support. Otherwise a new support is created, seeded with a single rule by
        /// <see cref="CreateTransitionSupport"/>.
        /// </remarks>
        public TransitionSupportModel CreateOrExtendSelfTransitionSupport(StateModel stateModel, Type transitionSupportType)
        {
            var existingSupport = FindSelfTransitionSupport(stateModel, transitionSupportType);
            if (existingSupport == null)
                return CreateSelfTransitionSupport(stateModel, transitionSupportType);

            existingSupport.AddTransition(existingSupport.CreateTransition());
            return existingSupport;
        }

        /// <summary>
        /// Returns the self transition support of the given type on a state, if there is one.
        /// </summary>
        /// <param name="stateModel">The state to look on.</param>
        /// <param name="transitionSupportType">The <see cref="TransitionSupportModel.TransitionSupportType"/> to look for.</param>
        /// <returns>The matching self transition support, or null if the state has none.</returns>
        public static TransitionSupportModel FindSelfTransitionSupport(StateModel stateModel, Type transitionSupportType)
        {
            foreach (var wire in stateModel.GetInPort().GetConnectedWires())
            {
                if (wire is TransitionSupportModel { IsSelfTransition: true } support
                    && support is not IGhostWireModel
                    && support.TransitionSupportType == transitionSupportType)
                {
                    return support;
                }
            }

            return null;
        }

        /// <summary>
        /// Reconciles the value of every <see cref="VariableConditionModel"/> that references the given variable,
        /// recreating it when the variable's type changed. Called when a variable's type changes.
        /// </summary>
        /// <param name="declaration">The variable whose type changed.</param>
        internal void ReconcileVariableConditions(VariableDeclarationModelBase declaration)
        {
            foreach (var wire in WireModels)
            {
                if (wire is not TransitionSupportModel transitionSupport)
                    continue;

                foreach (var transition in transitionSupport.Transitions)
                    ReconcileConditions(transition.ConditionModel, declaration);
            }
        }

        static void ReconcileConditions(ConditionModel condition, VariableDeclarationModelBase declaration)
        {
            switch (condition)
            {
                case null:
                    break;
                case GroupConditionModel group:
                    foreach (var subCondition in group.SubConditions)
                        ReconcileConditions(subCondition, declaration);
                    break;
                case VariableConditionModel variableCondition when variableCondition.Variable == declaration:
                    variableCondition.ReconcileValueType();
                    break;
            }
        }

        /// <summary>
        /// Registers a condition that has been added in this <see cref="GraphModel"/>.
        /// </summary>
        /// <param name="conditionModel">The condition to register.</param>
        public void RegisterCondition(ConditionModel conditionModel)
        {
            RegisterElement(conditionModel);
        }

        /// <summary>
        /// Unregisters a condition that is removed from this <see cref="GraphModel"/>.
        /// </summary>
        /// <param name="conditionModel">The condition to unregister.</param>
        public void UnregisterCondition(ConditionModel conditionModel)
        {
            UnregisterElement(conditionModel);
        }

        /// <summary>
        /// Registers a transition that has been added in this <see cref="GraphModel"/>.
        /// </summary>
        /// <param name="transitionModel">The transition to register</param>
        public void RegisterTransition(TransitionModel transitionModel)
        {
            RegisterElement(transitionModel);
        }

        /// <summary>
        /// Unregisters a transition that is removed from this <see cref="GraphModel"/>.
        /// </summary>
        /// <param name="transitionModel">The transition to unregister.</param>
        public void UnregisterTransition(TransitionModel transitionModel)
        {
            UnregisterElement(transitionModel);
        }
    }
}
