// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.Implementation;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for a state in a <see cref="StateMachine"/>.
    /// </summary>
    /// <remarks>
    /// A state is the state machine equivalent of an <see cref="INode"/>. Unlike a node, a state does not expose
    /// configurable input and output ports: its incoming and outgoing connections are transitions managed by the
    /// state machine itself.
    ///
    /// See also:
    ///
    ///- <see cref="State"/> for the base class used to define custom states
    ///- <see cref="ISubgraphState"/> for how to work with subgraph-based states
    ///
    /// Unity implements this interface. Do not implement it in your own types.
    /// </remarks>
    public interface IState
    {
        /// <summary>
        /// The text displayed when hovering over the state's header.
        /// </summary>
        public string Tooltip { get; set; }

        /// <summary>
        /// The main text displayed in the state's header.
        /// </summary>
        /// <remarks>
        /// Use this property to specify the state title displayed in the state machine view.
        /// To modify the state title displayed in the graph item library, use <see cref="NodeAttribute.Title"/>.
        /// </remarks>
        /// <seealso cref="NodeAttribute.Title"/>
        public string Title { get; set; }

        /// <summary>
        /// The secondary text displayed in the state's header.
        /// </summary>
        public string Subtitle { get; set; }

        /// <summary>
        /// The highlight color of the state. The highlight is located on the upper border of the state.
        /// </summary>
        public Color DefaultColor { get; set; }

        /// <summary>
        /// The progress fill amount displayed on the state's accent bar, expressed as a percentage.
        /// </summary>
        /// <remarks>
        /// Accepted values range from -100f to 100f. A positive value fills the bar from left to right.
        /// A negative value fills it from right to left. A value of <c>0</c> hides the bar.
        /// </remarks>
        public float FillAmount
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// The globally unique identifier for this state.
        /// </summary>
        public Hash128 ID { get; }

        /// <summary>
        /// The <see cref="StateMachine"/> that contains this state.
        /// </summary>
        public StateMachine StateMachine => (StateModel.GraphModel as GraphModelImp)?.Graph as StateMachine;

        /// <summary>
        /// Whether the state is connected to at least one transition.
        /// </summary>
        public bool IsConnected => StateModel.GetInPort().IsConnected() || StateModel.GetOutPort().IsConnected();

        /// <summary>
        /// The position of the state in the state machine.
        /// </summary>
        public Vector2 Position
        {
            get => StateModel.Position;
            set => StateModel.SetNodeModelPosition(value);
        }

        /// <summary>
        /// Removes the state from its state machine.
        /// </summary>
        public void RemoveFromStateMachine()
        {
            var stateModel = StateModel;
            if (stateModel.GraphModel is GraphModelImp graphModel)
            {
                graphModel.CheckModificationLock();
                graphModel.DeleteNode(stateModel, deleteConnections: true);
            }
        }

        /// <summary>
        /// Retrieves the transitions that go to this state.
        /// </summary>
        /// <returns>An <c>IEnumerable</c> of the incoming <see cref="ITransition"/>s.</returns>
        /// <remarks>
        /// A self transition is anchored on both the incoming and outgoing side of its state, so it appears in
        /// the results of both <see cref="GetIncomingTransitions"/> and <see cref="GetOutgoingTransitions"/>.
        /// </remarks>
        public IEnumerable<ITransition> GetIncomingTransitions() => StateMachineImp.GetTransitionsOnPort(StateModel.GetInPort());

        /// <summary>
        /// Retrieves the transitions that originate from this state.
        /// </summary>
        /// <returns>An <c>IEnumerable</c> of the outgoing <see cref="ITransition"/>s.</returns>
        /// <remarks>
        /// A self transition is anchored on both the incoming and outgoing side of its state, so it appears in
        /// the results of both <see cref="GetIncomingTransitions"/> and <see cref="GetOutgoingTransitions"/>.
        /// </remarks>
        public IEnumerable<ITransition> GetOutgoingTransitions() => StateMachineImp.GetTransitionsOnPort(StateModel.GetOutPort());

        /// <summary>
        /// The number of options defined in the state.
        /// </summary>
        public int OptionCount => StateModel.NodeOptions.Count;

        /// <summary>
        /// Retrieves a state option using its zero-based index.
        /// </summary>
        /// <param name="index">Index of the option, based on the order in which the options were declared.</param>
        /// <returns>The option at the specified index.</returns>
        /// <remarks>
        /// The index is zero-based.
        ///
        /// Throws <see cref="ArgumentOutOfRangeException"/> when the index is out of bounds.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if the index is out of bounds.
        /// </exception>
        public IStateOption GetOption(int index) => StateModel.NodeOptions[index];

        /// <summary>
        /// The options defined on this state.
        /// </summary>
        public IEnumerable<IStateOption> Options => StateModel.NodeOptions;

        /// <summary>
        /// Retrieves a state option using its name.
        /// </summary>
        /// <param name="name">The unique name of the option.</param>
        /// <returns>The option with the specified name, or null if none is found.</returns>
        /// <remarks>The option's name is unique within the state's options.</remarks>
        public IStateOption GetOptionByName(string name) => StateModel.NodeOptionsByName.GetValueOrDefault(name);

        internal StateModel StateModel => (StateModel)this;
    }
}
