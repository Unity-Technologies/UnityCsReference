// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    abstract partial class GraphModel
    {
        static readonly (string, ConditionModelFactory)[] k_DefaultConditionTypes = { ("Add Group Condition", _ => new GroupConditionModel()) };

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
        /// <param name="transitionSupportKind">The kind of transition to create.</param>
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <returns>The newly created wire</returns>
        public virtual TransitionSupportModel CreateTransitionSupport(
            PortModel toPort, AnchorSide toStateAnchorSide, float toStateAnchorOffset,
            PortModel fromPort, AnchorSide fromStateAnchorSide, float fromStateAnchorOffset,
            TransitionSupportKind transitionSupportKind, Hash128 guid = default)
        {
            var transitionType = GetTransitionType(toPort, fromPort, transitionSupportKind);
            if (transitionType == null)
                return null;

            var transitionSupport = CreateWire(transitionType, toPort, fromPort, false, guid) as TransitionSupportModel;
            if (transitionSupport != null)
            {
                transitionSupport.SetFromAnchor(fromStateAnchorSide, fromStateAnchorOffset);
                transitionSupport.SetToAnchor(toStateAnchorSide, toStateAnchorOffset);
                transitionSupport.TransitionSupportKind = transitionSupportKind;

                var transition = transitionSupport.CreateTransition();
                transitionSupport.AddTransition(transition);
            }
            return transitionSupport;
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
