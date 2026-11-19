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
    /// The base class for all user-accessible states in a <see cref="StateMachine"/>.
    /// </summary>
    /// <remarks>
    /// Inherit from this class to define custom state types that appear in a state machine graph. A state represents
    /// a node in a <see cref="StateMachine"/>; transitions connect states to one another. Unlike <see cref="Node"/>,
    /// a state does not expose configurable input and output ports: its incoming and outgoing transition connections
    /// are managed by the state machine itself.
    ///
    /// See also:
    ///
    ///- <see cref="StateMachine"/> for the graph type that contains states
    ///- <see cref="Node"/> for the equivalent base class used in regular graphs
    ///- <see cref="IState"/> for the interface this class implements
    ///
    /// </remarks>
    [Serializable]
    public abstract partial class State : IState
    {
        /// <summary>
        /// Interface that provides methods to declare state options inside a state.
        /// </summary>
        /// <remarks>
        /// Use this interface to add options on states. Unlike the node options of a <see cref="Node"/>, the options of
        /// a state are never drawn on the state in the state machine canvas: they only appear in the graph inspector when the
        /// state is selected.
        /// </remarks>
        public interface IOptionDefinitionContext
        {
            /// <summary>
            /// Adds a new state option.
            /// </summary>
            /// <param name="name">The unique identifier of the option.</param>
            /// <param name="dataType">The data type of the option.</param>
            /// <returns>An <see cref="IStateOptionBuilder"/> to further configure the option.</returns>
            /// <remarks>
            /// <c>name</c> is used to identify the option. It must be unique among the options on the state. This name is used as the ID when calling <see cref="GetOptionByName(string)"/>.
            /// If <see cref="IStateOptionBuilder.WithDisplayName(string)"/> is not used, this name is also used as the option's display label.
            /// </remarks>
            /// <example>
            /// <code lang="cs">
            /// <![CDATA[
            /// protected override void OnDefineOptions(IOptionDefinitionContext context)
            /// {
            ///     context.AddOption("MyOption", typeof(int))
            ///         .WithDefaultValue(2)
            ///         .Delayed();
            /// }
            /// ]]>
            /// </code>
            /// </example>
            IStateOptionBuilder AddOption(string name, Type dataType);

            /// <summary>
            /// Adds a new state option.
            /// </summary>
            /// <typeparam name="TData">The data type of the option.</typeparam>
            /// <param name="name">The unique identifier of the option.</param>
            /// <returns>An <see cref="IStateOptionBuilder"/> to further configure the option.</returns>
            /// <remarks>
            /// <c>name</c> is used to identify the option. It must be unique among the options on the state. This name is used as the ID when calling <see cref="GetOptionByName(string)"/>.
            /// If <see cref="IStateOptionBuilder{TData}.WithDisplayName(string)"/> is not used, this name is also used as the option's display label.
            /// </remarks>
            /// <example>
            /// <code lang="cs">
            /// <![CDATA[
            /// protected override void OnDefineOptions(IOptionDefinitionContext context)
            /// {
            ///     context.AddOption<int>("MyOption")
            ///         .WithDefaultValue(2)
            ///         .Delayed();
            /// }
            /// ]]>
            /// </code>
            /// </example>
            IStateOptionBuilder<TData> AddOption<TData>(string name);
        }

        /// <summary>
        /// The <see cref="StateMachine"/> that contains this state.
        /// </summary>
        public StateMachine StateMachine => ((IState)GetImplementation()).StateMachine;

        /// <summary>
        /// Whether the state is connected to at least one transition.
        /// </summary>
        public bool IsConnected => ((IState)GetImplementation()).IsConnected;

        /// <summary>
        /// The globally unique identifier for this state.
        /// </summary>
        public Hash128 ID => GetImplementation().Guid;

        /// <summary>
        /// The text displayed when hovering over the state's header.
        /// </summary>
        public string Tooltip
        {
            get => GetImplementation().Tooltip;
            set => GetImplementation().Tooltip = value;
        }

        /// <summary>
        /// The main text displayed in the state's header.
        /// </summary>
        /// <remarks>
        /// Use this property to specify the state title displayed in the state machine view.
        /// To modify the state title displayed in the graph item library, use <see cref="NodeAttribute.Title"/>.
        /// </remarks>
        /// <seealso cref="NodeAttribute.Title"/>
        public string Title
        {
            get => GetImplementation().Title;
            set => ((IState)GetImplementation()).Title = value;
        }

        /// <summary>
        /// The secondary text displayed in the state's header.
        /// </summary>
        public string Subtitle
        {
            get => GetImplementation().Subtitle;
            set => GetImplementation().Subtitle = value;
        }

        /// <summary>
        /// The highlight color of the state. The highlight is located on the upper border of the state.
        /// </summary>
        public Color DefaultColor
        {
            get => GetImplementation().DefaultColor;
            set => GetImplementation().DefaultColor = value;
        }

        /// <summary>
        /// The progress fill amount displayed on the state's accent bar, expressed as a percentage.
        /// </summary>
        /// <remarks>
        /// Accepted values range from -100f to 100f. A positive value fills the bar from left to right.
        /// A negative value fills it from right to left. A value of <c>0</c> hides the bar.
        /// </remarks>
        public float FillAmount
        {
            get => GetImplementation().FillAmount;
            set => GetImplementation().FillAmount = value;
        }

        /// <summary>
        /// Called when the state is created or when the state machine is enabled.
        /// </summary>
        /// <remarks>
        /// Use this method to perform initialization logic.
        /// </remarks>
        public virtual void OnEnable() { }

        /// <summary>
        /// Called when the state is removed or when the state machine is disabled.
        /// </summary>
        /// <remarks>
        /// Use this method to perform any cleanup logic.
        /// </remarks>
        public virtual void OnDisable() { }

        /// <summary>
        /// The position of the state in the state machine.
        /// </summary>
        public Vector2 Position
        {
            get => GetImplementation().Position;
            set => GetImplementation().SetNodeModelPosition(value);
        }

        /// <summary>
        /// Removes the state from its state machine.
        /// </summary>
        public void RemoveFromStateMachine() => ((IState)GetImplementation()).RemoveFromStateMachine();

        /// <summary>
        /// Retrieves the transitions that go to this state.
        /// </summary>
        /// <returns>An <c>IEnumerable</c> of the incoming <see cref="ITransition"/>s.</returns>
        /// <remarks>
        /// A self transition is anchored on both the incoming and outgoing side of its state, so it appears in
        /// the results of both <see cref="GetIncomingTransitions"/> and <see cref="GetOutgoingTransitions"/>.
        /// </remarks>
        public IEnumerable<ITransition> GetIncomingTransitions() => ((IState)GetImplementation()).GetIncomingTransitions();

        /// <summary>
        /// Retrieves the transitions that originate from this state.
        /// </summary>
        /// <returns>An <c>IEnumerable</c> of the outgoing <see cref="ITransition"/>s.</returns>
        /// <remarks>
        /// A self transition is anchored on both the incoming and outgoing side of its state, so it appears in
        /// the results of both <see cref="GetIncomingTransitions"/> and <see cref="GetOutgoingTransitions"/>.
        /// </remarks>
        public IEnumerable<ITransition> GetOutgoingTransitions() => ((IState)GetImplementation()).GetOutgoingTransitions();

        /// <summary>
        /// Defines the structure of the state by building its options.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="OnDefineOptions"/> to allow custom definition of the state. It does nothing
        /// when the state is not part of a <see cref="StateMachine"/>.
        /// </remarks>
        /// <example>
        /// <code lang="cs">
        /// <![CDATA[
        /// public class MyState : State
        /// {
        ///     public override void OnEnable()
        ///     {
        ///         DefineState();
        ///     }
        /// }
        /// ]]>
        /// </code>
        /// </example>
        public void DefineState()
        {
            if (StateMachine == null)
                return;

            GetImplementation().DefineNode();
        }

        /// <summary>
        /// Called during <see cref="DefineState"/> to define the options available on the state.
        /// </summary>
        /// <param name="context">Provides methods for defining state options.</param>
        /// <remarks>
        /// Override this method to add options using the provided <see cref="IOptionDefinitionContext"/>. The options
        /// of a state only appear in the graph inspector when the state is selected: they are never drawn on the state in
        /// the state machine view.
        /// </remarks>
        /// <example>
        /// <code lang="cs">
        /// <![CDATA[
        /// protected override void OnDefineOptions(IOptionDefinitionContext context)
        /// {
        ///     context.AddOption<float>("speed")
        ///         .WithDisplayName("Speed")
        ///         .WithTooltip("The speed at which the state runs.")
        ///         .WithDefaultValue(1.0f);
        /// }
        /// ]]>
        /// </code>
        /// </example>
        protected virtual void OnDefineOptions(IOptionDefinitionContext context) { }

        /// <summary>
        /// The number of options defined in the state.
        /// </summary>
        /// <example>
        /// <code lang="cs">
        /// <![CDATA[
        /// var optionCount = myState.OptionCount;
        /// ]]>
        /// </code>
        /// </example>
        public int OptionCount => ((IState)GetImplementation()).OptionCount;

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
        /// <example>
        /// <code lang="cs">
        /// <![CDATA[
        /// var firstOption = myState.GetOption(0);
        /// ]]>
        /// </code>
        /// </example>
        public IStateOption GetOption(int index) => ((IState)GetImplementation()).GetOption(index);

        /// <summary>
        /// The options defined on this state.
        /// </summary>
        /// <example>
        /// <code lang="cs">
        /// <![CDATA[
        /// foreach (var option in myState.Options)
        ///     Debug.Log(option.Name);
        /// ]]>
        /// </code>
        /// </example>
        public IEnumerable<IStateOption> Options => ((IState)GetImplementation()).Options;

        /// <summary>
        /// Retrieves a state option using its name.
        /// </summary>
        /// <param name="name">The unique name of the option.</param>
        /// <returns>The option with the specified name, or null if none is found.</returns>
        /// <remarks>The option's name is unique within the state's options.</remarks>
        /// <example>
        /// <code lang="cs">
        /// <![CDATA[
        /// if (myState.GetOptionByName("speed") is { } speed && speed.TryGetValue<float>(out var value))
        ///     Debug.Log(value);
        /// ]]>
        /// </code>
        /// </example>
        public IStateOption GetOptionByName(string name) => ((IState)GetImplementation()).GetOptionByName(name);
    }
}
